using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Npgsql;
using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class ErrorMetadataTest(DatabaseFixture database)
{
	[Theory]
	[InlineData(DataConstraintKind.PrimaryKey, false)]
	[InlineData(DataConstraintKind.Unique, false)]
	[InlineData(DataConstraintKind.Unique, true)]
	public async Task CatalogResolvesQuotedSchemaAndOrderedKeysWithoutIncludeColumnsAsync(DataConstraintKind kind, bool expression)
	{
		if(!Global.IsTestingEnabled)
			return;

		var cancellation = TestContext.Current.CancellationToken;
		var prefix = "Error'" + Guid.NewGuid().ToString("N");
		var firstSchema = prefix + "A";
		var secondSchema = prefix + "B";
		const string TABLE = "Order'\"Table";
		const string INDEX = "Key'\"Index";
		const string FIRST = "First Key";
		const string SECOND = "Second'Key";
		const string INCLUDED = "Included Value";
		var firstTable = Quote(firstSchema) + "." + Quote(TABLE);
		var secondTable = Quote(secondSchema) + "." + Quote(TABLE);
		await using var connection = PostgreSqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync(cancellation);

		try
		{
			await ExecuteAsync(connection, $"CREATE SCHEMA {Quote(firstSchema)}; CREATE SCHEMA {Quote(secondSchema)}", cancellation);
			var columns = $"({Quote(FIRST)} integer NOT NULL, {Quote(SECOND)} integer NOT NULL, {Quote(INCLUDED)} text)";
			await ExecuteAsync(connection, $"CREATE TABLE {firstTable} {columns}; CREATE TABLE {secondTable} {columns}", cancellation);

			if(kind == DataConstraintKind.PrimaryKey)
				await ExecuteAsync(connection, $"ALTER TABLE {firstTable} ADD CONSTRAINT {Quote(INDEX)} PRIMARY KEY ({Quote(SECOND)}, {Quote(FIRST)}) INCLUDE ({Quote(INCLUDED)})", cancellation);
			else
				await ExecuteAsync(connection, $"CREATE UNIQUE INDEX {Quote(INDEX)} ON {firstTable} ({(expression ? $"abs({Quote(SECOND)})" : Quote(SECOND))}, {Quote(FIRST)}) INCLUDE ({Quote(INCLUDED)})", cancellation);

			// The second schema deliberately uses the same table/index names and a different key.
			await ExecuteAsync(connection, $"CREATE UNIQUE INDEX {Quote(INDEX)} ON {secondTable} ({Quote(FIRST)})", cancellation);
			await ExecuteAsync(connection, $"INSERT INTO {firstTable} VALUES (1, 2, 'first')", cancellation);
			var original = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO {firstTable} VALUES (1, 2, 'different included value')", cancellation));
			Assert.Equal(PostgresErrorCodes.UniqueViolation, original.SqlState);
			Assert.Equal(firstSchema, original.SchemaName);
			Assert.Equal(TABLE, original.TableName);
			Assert.Equal(INDEX, original.ConstraintName);
			using var context = new DataExistContext(database.Accessor, nameof(Log), null);
			await using var session = context.Session;
			var result = Assert.IsType<DataConstraintException>(await PostgreSqlDriver.Instance.OnErrorAsync(context, original, cancellation));
			Assert.Equal(kind, result.Kind);
			Assert.Equal(23505, result.Code);
			Assert.Equal(PostgreSqlDriver.NAME, result.DriverName);
			Assert.Equal(INDEX, result.Name);
			Assert.Equal(TABLE, result.Principal.Name);
			Assert.Equal(expression ? Array.Empty<string>() : [SECOND, FIRST], result.Principal.Fields);
			Assert.DoesNotContain(INCLUDED, result.Principal.Fields);
			Assert.Null(result.Value);
			Assert.Same(original, result.InnerException);

			await ExecuteAsync(connection, $"INSERT INTO {secondTable} VALUES (1, 2, 'first')", cancellation);
			var otherError = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO {secondTable} VALUES (1, 3, 'different key')", cancellation));
			Assert.Equal(PostgresErrorCodes.UniqueViolation, otherError.SqlState);
			Assert.Equal(secondSchema, otherError.SchemaName);
			var otherResult = Assert.IsType<DataConstraintException>(await PostgreSqlDriver.Instance.OnErrorAsync(context, otherError, cancellation));
			Assert.Equal(DataConstraintKind.Unique, otherResult.Kind);
			Assert.Equal(INDEX, otherResult.Name);
			Assert.Equal(TABLE, otherResult.Principal.Name);
			Assert.Equal(new[] { FIRST }, otherResult.Principal.Fields);
			Assert.Same(otherError, otherResult.InnerException);
		}
		finally
		{
			await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(firstSchema)} CASCADE; DROP SCHEMA IF EXISTS {Quote(secondSchema)} CASCADE", CancellationToken.None);
		}
	}

	[Fact]
	public async Task MetadataConnectionFailurePreservesOriginalConstraintAsync()
	{
		await using var accessor = CreateUnavailableAccessor();
		using var context = new DataExistContext(accessor, nameof(Log), null);
		await using var session = context.Session;
		var connector = DataConnectorManager.GetConnector(context.Source);
		var failures = 0;
		connector.Failed += (_, args, _) =>
		{
			failures++;
			args.ExceptionHandled = true;
			return ValueTask.CompletedTask;
		};
		var original = CreateUniqueViolation();
		var result = Assert.IsType<DataConstraintException>(await PostgreSqlDriver.Instance.OnErrorAsync(context, original, TestContext.Current.CancellationToken));
		Assert.Equal(1, failures);
		Assert.Equal(DataConstraintKind.Unique, result.Kind);
		Assert.Equal(23505, result.Code);
		Assert.Equal(original.ConstraintName, result.Name);
		Assert.Equal(original.TableName, result.Principal.Name);
		Assert.Empty(result.Principal.Fields);
		Assert.Null(result.Value);
		Assert.Same(original, result.InnerException);
	}

	[Fact]
	public async Task CancelledMetadataLookupPropagatesCancellationAsync()
	{
		await using var accessor = CreateUnavailableAccessor();
		using var context = new DataExistContext(accessor, nameof(Log), null);
		await using var session = context.Session;
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.Cancel();
		var failures = 0;
		DataConnectorManager.GetConnector(context.Source).Failed += (_, _, _) =>
		{
			failures++;
			return ValueTask.CompletedTask;
		};
		var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await PostgreSqlDriver.Instance.OnErrorAsync(context, CreateUniqueViolation(), cancellation.Token));
		Assert.Equal(cancellation.Token, error.CancellationToken);
		Assert.Equal(0, failures);
	}

	[Fact]
	public async Task DataAccessDuplicateKeyReturnsPrimaryConstraintAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var cancellation = TestContext.Current.CancellationToken;
		var accessor = database.Accessor;
		var id = (uint)Random.Shared.Next(1_000_000, int.MaxValue);
		var name = "Error-" + Guid.NewGuid().ToString("N");
		var criteria = Condition.Equal(nameof(UserModel.UserId), id) & Condition.Equal(nameof(UserModel.Name), name);
		Assert.False(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), id), cancellation));
		var options = DataInsertOptions.Sequence(DataSequenceBehavior.Never).Build();
		var user = Model.Build<UserModel>(model =>
		{
			model.UserId = id;
			model.Name = name;
			model.Namespace = name;
			model.Enabled = true;
		});

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, "UserId,Name,Namespace,Enabled", options, cancellation));
			var originalCount = await accessor.CountAsync<UserModel>(criteria, cancellation);
			var result = await Assert.ThrowsAsync<DataConstraintException>(async () =>
				await accessor.InsertAsync(user, "UserId,Name,Namespace,Enabled", options, cancellation));
			var original = Assert.IsType<PostgresException>(result.InnerException);
			Assert.Equal(PostgresErrorCodes.UniqueViolation, original.SqlState);
			Assert.Equal(23505, result.Code);
			Assert.Equal(DataConstraintKind.PrimaryKey, result.Kind);
			Assert.Equal(original.ConstraintName, result.Name);
			Assert.Equal("Security_User", result.Principal.Name);
			Assert.Equal(new[] { "UserId" }, result.Principal.Fields);
			Assert.Null(result.Value);
			Assert.Equal(1, originalCount);
			Assert.Equal(originalCount, await accessor.CountAsync<UserModel>(criteria, cancellation));
		}
		finally
		{
			await accessor.DeleteAsync<UserModel>(criteria, CancellationToken.None);
		}
	}

	[Fact]
	public async Task ServerForeignKeyViolationPreservesConstraintAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var cancellation = TestContext.Current.CancellationToken;
		var schema = "ErrorForeign" + Guid.NewGuid().ToString("N");
		var parent = Quote(schema) + ".\"Parent\"";
		var child = Quote(schema) + ".\"Child\"";
		const string CONSTRAINT = "FK'Child_Parent";
		await using var connection = PostgreSqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync(cancellation);

		try
		{
			await ExecuteAsync(connection, $"CREATE SCHEMA {Quote(schema)}; CREATE TABLE {parent} (id integer PRIMARY KEY); CREATE TABLE {child} (parent integer CONSTRAINT {Quote(CONSTRAINT)} REFERENCES {parent}(id))", cancellation);
			var original = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO {child} VALUES (1)", cancellation));
			Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, original.SqlState);
			Assert.Equal(CONSTRAINT, original.ConstraintName);
			using var context = new DataExistContext(database.Accessor, nameof(Log), null);
			await using var session = context.Session;
			var result = Assert.IsType<DataConflictException>(await PostgreSqlDriver.Instance.OnErrorAsync(context, original, cancellation));
			Assert.Equal(PostgreSqlDriver.NAME, result.DriverName);
			Assert.Equal(23503, result.Code);
			Assert.Equal(CONSTRAINT, result.Name);
			Assert.Null(result.Value);
			Assert.Same(original, result.InnerException);
		}
		finally
		{
			await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(schema)} CASCADE", CancellationToken.None);
		}
	}

	[Fact]
	public async Task ServerStringTruncationPreservesNullableColumnAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var cancellation = TestContext.Current.CancellationToken;
		var schema = "ErrorLength" + Guid.NewGuid().ToString("N");
		var table = Quote(schema) + ".\"Length\"";
		await using var connection = PostgreSqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync(cancellation);

		try
		{
			await ExecuteAsync(connection, $"CREATE SCHEMA {Quote(schema)}; CREATE TABLE {table} (value varchar(3))", cancellation);
			var original = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO {table} VALUES ('long-value')", cancellation));
			Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, original.SqlState);
			using var context = new DataExistContext(database.Accessor, nameof(Log), null);
			await using var session = context.Session;
			var result = Assert.IsType<DataArgumentException>(await PostgreSqlDriver.Instance.OnErrorAsync(context, original, cancellation));
			Assert.Equal(original.ColumnName, result.Name);
			Assert.Null(result.Value);
			Assert.Same(original, result.InnerException);
		}
		finally
		{
			await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(schema)} CASCADE", CancellationToken.None);
		}
	}

	private static DataAccess CreateUnavailableAccessor()
	{
		var name = "ErrorMetadata-" + Guid.NewGuid().ToString("N");
		var settings = Configuration.PostgreSqlConnectionSettingsDriver.Instance.GetSettings(name,
			"Server=127.0.0.1;Port=1;Database=unavailable;UserName=invalid;Password=invalid;Timeout=00:00:01;Pooling=false");
		return new DataAccess(name, new DataAccessOptions([settings]));
	}
	private static PostgresException CreateUniqueViolation() => new("test", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
		schemaName: "public", tableName: "ErrorTable", constraintName: "ErrorIndex");
	private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
	private static async Task ExecuteAsync(DbConnection connection, string sql, CancellationToken cancellation)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		await command.ExecuteNonQueryAsync(cancellation);
	}
}
