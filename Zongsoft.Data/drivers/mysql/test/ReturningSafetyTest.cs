using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MySqlConnector;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class ReturningSafetyTest(DatabaseFixture database)
{
	[Fact]
	public async Task SnapshotPreservesBinaryAndStringKeysAndDeduplicatesJoinsAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var mapping = new DataEntity(null, $"Returning{Guid.NewGuid():N}");
		mapping.Properties.Simplex("Id", DataType.Binary, 4, false);
		mapping.Properties.Simplex("Code", DataType.String, 32, false);
		mapping.Properties.Simplex("Value", DataType.String, 50, true);
		mapping.SetKey("Id", "Code");
		var duplicates = new DataEntity(null, $"Returning{Guid.NewGuid():N}");
		duplicates.Properties.Simplex("Id", DataType.Binary, 4, false);
		Mapping.Entities.Add(mapping);
		var accessor = database.Accessor;
		var executor = accessor.Provider.Executor;
		await using var connection = MySqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();

		try
		{
			command.CommandText = $"CREATE TABLE `{mapping.Name}` (`Id` BINARY(4), `Code` VARCHAR(32) COLLATE utf8mb4_0900_bin, `Value` VARCHAR(50), PRIMARY KEY (`Id`,`Code`)) ENGINE=InnoDB; " +
				$"CREATE TABLE `{duplicates.Name}` (`Id` BINARY(4)) ENGINE=InnoDB; " +
				$"INSERT INTO `{mapping.Name}` VALUES (X'000102FF','Key','Original'),(X'000102FF','KEY','Original'),(X'000102FF','Key ','Original'); " +
				$"INSERT INTO `{duplicates.Name}` VALUES (X'000102FF'),(X'000102FF');";
			await command.ExecuteNonQueryAsync();
			accessor.Provider.Executor = new JoiningExecutor(executor, duplicates);
			var options = DataUpdateOptions.Return(ReturningKind.Newer, "Id", "Code", "Value")
				.Return(ReturningKind.Older, "Code", "Value").Build();
			Assert.Equal(3, await accessor.UpdateAsync(mapping.Name, new { Value = "Changed" }, Condition.Equal("Value", "Original"), "Value", options));
			Assert.Equal(3, options.Returning.Rows.Count);
			var codes = new HashSet<string>(StringComparer.Ordinal);

			foreach(var row in options.Returning.Rows)
			{
				Assert.True(row.TryGetValue("Id", ReturningKind.Newer, out var id));
				Assert.Equal(new byte[] { 0, 1, 2, 255 }, Assert.IsType<byte[]>(id));
				Assert.True(row.TryGetValue("Code", ReturningKind.Newer, out var code));
				Assert.True(codes.Add(Assert.IsType<string>(code)));
				Assert.True(row.TryGetValue("Code", ReturningKind.Older, out var previousCode));
				Assert.Equal(code, previousCode);
				Assert.True(row.TryGetValue("Value", ReturningKind.Older, out var previous));
				Assert.Equal("Original", previous);
				Assert.True(row.TryGetValue("Value", ReturningKind.Newer, out var current));
				Assert.Equal("Changed", current);
			}

			Assert.True(codes.SetEquals(["Key", "KEY", "Key "]));
			var deletion = DataDeleteOptions.Return("Code", "Value").Build();
			Assert.Equal(3, await accessor.DeleteAsync(mapping.Name, Condition.Equal("Value", "Changed"), deletion));
			Assert.Equal(3, deletion.Returning.Rows.Count);
		}
		finally
		{
			accessor.Provider.Executor = executor;
			Mapping.Entities.Remove(mapping.QualifiedName);
			command.CommandText = $"DROP TABLE IF EXISTS `{mapping.Name}`, `{duplicates.Name}`;";
			await command.ExecuteNonQueryAsync();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SnapshotReadsTriggerValuesAndRejectsChangedKeysAsync(bool changesKey)
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var mapping = new DataEntity(null, $"Returning{Guid.NewGuid():N}");
		mapping.Properties.Simplex("Id", DataType.Int32, false);
		mapping.Properties.Simplex("Value", DataType.String, 50, true);
		mapping.SetKey("Id");
		Mapping.Entities.Add(mapping);
		await using var connection = MySqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		var options = DataUpdateOptions.Return(ReturningKind.Newer, "Value").Return(ReturningKind.Older, "Value").Build();

		try
		{
			var assignment = changesKey ? "NEW.Id=OLD.Id+10" : "NEW.Value=CONCAT(NEW.Value,':trigger')";
			command.CommandText = $"CREATE TABLE `{mapping.Name}` (`Id` INT PRIMARY KEY, `Value` VARCHAR(50)) ENGINE=InnoDB; " +
				$"INSERT INTO `{mapping.Name}` VALUES (1,'Original'); " +
				$"CREATE TRIGGER `T_{mapping.Name}` BEFORE UPDATE ON `{mapping.Name}` FOR EACH ROW SET {assignment};";
			await command.ExecuteNonQueryAsync();

			if(changesKey)
			{
				await Assert.ThrowsAsync<NotSupportedException>(async () => await database.Accessor.UpdateAsync(mapping.Name,
					new { Value = "Changed" }, Condition.Equal("Id", 1), "Value", options));
				Assert.Empty(options.Returning.Rows);
				command.CommandText = $"SELECT `Value` FROM `{mapping.Name}` WHERE `Id`=1;";
				Assert.Equal("Original", await command.ExecuteScalarAsync());
			}
			else
			{
				Assert.Equal(1, await database.Accessor.UpdateAsync(mapping.Name, new { Value = "Changed" }, Condition.Equal("Id", 1), "Value", options));
				var row = Assert.Single(options.Returning.Rows);
				Assert.True(row.TryGetValue("Value", ReturningKind.Older, out var previous));
				Assert.Equal("Original", previous);
				Assert.True(row.TryGetValue("Value", ReturningKind.Newer, out var current));
				Assert.Equal("Changed:trigger", current);
			}
		}
		finally
		{
			Mapping.Entities.Remove(mapping.QualifiedName);
			command.CommandText = $"DROP TABLE IF EXISTS `{mapping.Name}`;";
			await command.ExecuteNonQueryAsync();
		}
	}

	[Fact]
	public async Task LockPrecedesMutationAndWriteBinderRunsOnceAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var accessor = database.Accessor;
		var user = Model.Build<UserModel>(user => { user.UserId = 1609000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name))
			.Return(ReturningKind.Older, nameof(UserModel.Name)).Build();
		var binder = new PausingBinder();
		var executor = accessor.Provider.Executor;
		Task<int> operation = null;

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			await using var connection = MySqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
			await connection.OpenAsync();
			await using var command = connection.CreateCommand();
			command.CommandText = "SET SESSION innodb_lock_wait_timeout=1; UPDATE Security_User SET Name='Concurrent' WHERE UserId=1609000;";
			accessor.Provider.Executor = new BindingExecutor(executor, binder);
			operation = accessor.UpdateAsync<UserModel>(new Dictionary<string, object> { [nameof(UserModel.Name)] = "Updated" }, criteria, options).AsTask();
			await binder.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.Empty(options.Returning.Rows);
			var error = await Assert.ThrowsAsync<MySqlException>(async () => await command.ExecuteNonQueryAsync());
			Assert.Equal(1205, error.Number);
			binder.Release.TrySetResult();
			Assert.Equal(1, await operation.WaitAsync(TimeSpan.FromSeconds(10)));
			Assert.Equal(1, binder.Count);
			var row = Assert.Single(options.Returning.Rows);
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out var previous));
			Assert.Equal("Original", previous);
			Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var current));
			Assert.Equal("Updated", current);
		}
		finally
		{
			binder.Release.TrySetResult();

			try
			{
				if(operation != null)
					await operation.WaitAsync(TimeSpan.FromSeconds(10));
			}
			finally
			{
				accessor.Provider.Executor = executor;
				await accessor.DeleteAsync<UserModel>(criteria);
			}
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RejectUnsupportedKeyBeforeWritingAsync(bool primaryKey)
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var accessor = database.Accessor;
		var user = Model.Build<UserModel>(user => { user.UserId = 1607000; user.Name = "Original"; });
		var mapping = new DataEntity(null, $"Returning{Guid.NewGuid():N}")
		{
			Alias = Mapping.Entities[Model.Naming.Get<UserModel>()].GetTableName(),
		};
		mapping.Properties.Simplex("Id", DataType.UInt32, false).Alias = "UserId";
		mapping.Properties.Simplex("Value", DataType.String, 50, true).Alias = "Name";

		if(primaryKey)
			mapping.SetKey("Id");

		Mapping.Entities.Add(mapping);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, "Value").Build();
		options.Behaviors = UpdateBehaviors.PrimaryKey;

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			await Assert.ThrowsAsync<NotSupportedException>(async () => await accessor.UpdateAsync(mapping.Name,
				new { Id = user.UserId + 1, Value = "Changed" }, Condition.Equal("Id", user.UserId), "Id,Value", options));
			Assert.Empty(options.Returning.Rows);

			await foreach(var actual in accessor.SelectAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId)))
				Assert.Equal("Original", actual.Name);
		}
		finally
		{
			Mapping.Entities.Remove(mapping.Name);
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
		}
	}

	[Fact]
	public async Task RejectSuppressedTransactionBeforeWritingAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var accessor = database.Accessor;
		var options = DataDeleteOptions.Return(nameof(UserModel.Name)).Build();
		IDataSource source = null;
		accessor.Provider.Executing += Suppress;

		try
		{
			await Assert.ThrowsAsync<NotSupportedException>(async () => await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), int.MaxValue), options));
			Assert.Empty(options.Returning.Rows);
		}
		finally
		{
			accessor.Provider.Executing -= Suppress;
			source?.Features.Remove(Feature.TransactionSuppressed);
		}

		ValueTask Suppress(object sender, DataAccessEventArgs<IDataAccessContext> args, System.Threading.CancellationToken cancellation)
		{
			source = args.Context.Source;
			source.Features.Add(Feature.TransactionSuppressed);
			return ValueTask.CompletedTask;
		}
	}

	[Fact]
	public async Task RejectNontransactionalTableBeforeWritingAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var mapping = new DataEntity(null, $"Returning{Guid.NewGuid():N}");
		mapping.Properties.Simplex("Id", DataType.Int32, false);
		mapping.Properties.Simplex("Value", DataType.String, 50, true);
		mapping.SetKey("Id");
		Mapping.Entities.Add(mapping);
		await using var connection = MySqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		var options = DataUpdateOptions.Return(ReturningKind.Newer, "Value").Build();

		try
		{
			command.CommandText = $"CREATE TABLE `{mapping.Name}` (`Id` INT PRIMARY KEY, `Value` VARCHAR(50)) ENGINE=MyISAM; INSERT INTO `{mapping.Name}` VALUES (1,'Original');";
			await command.ExecuteNonQueryAsync();
			await Assert.ThrowsAsync<NotSupportedException>(async () => await database.Accessor.UpdateAsync(mapping.Name,
				new { Value = "Changed" }, Condition.Equal("Id", 1), "Value", options));
			Assert.Empty(options.Returning.Rows);
			command.CommandText = $"SELECT `Value` FROM `{mapping.Name}` WHERE `Id`=1;";
			Assert.Equal("Original", await command.ExecuteScalarAsync());
		}
		finally
		{
			Mapping.Entities.Remove(mapping.Name);
			command.CommandText = $"DROP TABLE IF EXISTS `{mapping.Name}`;";
			await command.ExecuteNonQueryAsync();
		}
	}

	private sealed class JoiningExecutor(IDataExecutor executor, IDataEntity entity) : IDataExecutor
	{
		public ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation)
		{
			if(statement is UpdateStatement update)
			{
				var join = new JoinClause("Duplicates", new TableIdentifier(entity, "D"), JoinType.Inner);
				join.Conditions.Add(Expression.Equal(update.Table.CreateField("Id"), join.CreateField("Id")));
				update.From.Add(join);
			}

			return executor.ExecuteAsync(context, statement, cancellation);
		}
	}

	private sealed class BindingExecutor(IDataExecutor executor, PausingBinder binder) : IDataExecutor
	{
		public ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation)
		{
			var parameter = statement.Parameters.Single(parameter => parameter.Schema?.Name == nameof(UserModel.Name));
			parameter.Value = binder;
			parameter.Type = DataType.String;
			return executor.ExecuteAsync(context, statement, cancellation);
		}
	}

	private sealed class PausingBinder : IDataValueBinder
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Count { get; private set; }

		public async ValueTask<object> BindAsync(IDataMutateContextBase context, object data, object value, CancellationToken cancellation)
		{
			this.Count++;
			this.Entered.TrySetResult();
			await this.Release.Task.WaitAsync(cancellation);
			return "Updated";
		}
	}
}
