using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.Tests;

public abstract class BatchMutationTestBase(IDataAccess accessor, bool enabled, bool returning, Type constraintException = null)
{
	#region 公共方法
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportPreservesValuesAcrossBatchesAsync(bool streaming)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var marker = Guid.NewGuid().ToString("N");
		var rows = Model.Build<UserModel>(2051, (row, index) =>
		{
			row.UserId = (uint)(1800000 + index);
			row.Name = $"Import-{index}";
			row.Namespace = marker;
			row.Description = index % 2 == 0 ? null : $"Value-{index}";
		}).ToArray();
		var criteria = Condition.Equal(nameof(UserModel.Namespace), marker);

		try
		{
			var count = streaming ? await accessor.ImportAsync(Stream(rows)) : await accessor.ImportAsync(rows);
			Assert.Equal(rows.Length, count);
			var stored = (await ReadAsync(accessor.SelectAsync<UserModel>(criteria))).ToDictionary(row => row.UserId);
			Assert.Equal(rows.Length, stored.Count);

			foreach(var row in rows)
			{
				Assert.True(stored.TryGetValue(row.UserId, out var actual));
				Assert.Equal(row.Name, actual.Name);
				Assert.Equal(row.Description, actual.Description);
			}
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportSourceFailureRollsBackEarlierBatchesAsync(bool canceled)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var marker = Guid.NewGuid().ToString("N");
		var criteria = Condition.Equal(nameof(UserModel.Namespace), marker);
		var failure = new InvalidOperationException();
		var disposed = false;
		using var cancellation = new CancellationTokenSource();

		try
		{
			var exception = await Record.ExceptionAsync(async () => await accessor.ImportAsync(Source(), cancellation: cancellation.Token));
			Assert.NotNull(exception);

			if(canceled)
				Assert.IsAssignableFrom<OperationCanceledException>(exception.GetBaseException());
			else
				Assert.Same(failure, exception.GetBaseException());

			Assert.True(disposed);
			Assert.Equal(0, await accessor.CountAsync<UserModel>(criteria));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }

		async IAsyncEnumerable<UserModel> Source()
		{
			try
			{
				for(int i = 0; i < 1300; i++)
				{
					var index = i;
					yield return Model.Build<UserModel>(row =>
					{
						row.UserId = (uint)(1810000 + index);
						row.Name = $"ImportFailure-{index}";
						row.Namespace = marker;
					});
				}

				await Task.Yield();
				if(canceled)
				{
					cancellation.Cancel();
					cancellation.Token.ThrowIfCancellationRequested();
				}

				throw failure;
			}
			finally { disposed = true; }
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InsertManyPreservesValuesAndGeneratedIdsAcrossBatchesAsync(bool streaming)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var marker = Guid.NewGuid().ToString("N");
		var rows = Model.Build<Log>(513, (row, index) =>
		{
			row.UserId = 1;
			row.TenantId = 1;
			row.BranchId = 0;
			row.Target = marker;
			row.Action = $"Batch-{index}";
			row.Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index);
		}).ToArray();
		var criteria = Condition.Equal(nameof(Log.Target), marker);
		var options = returning ? DataInsertOptions.Return(nameof(Log.LogId), nameof(Log.Action)).Build() : new DataInsertOptions();

		try
		{
			var count = streaming ?
				await accessor.InsertManyAsync(Stream(rows), options) :
				await accessor.InsertManyAsync(rows, options);
			Assert.Equal(rows.Length, count);
			Assert.All(rows, row => Assert.True(row.LogId > 0));
			Assert.Equal(rows.Length, rows.Select(row => row.LogId).Distinct().Count());
			var stored = await ReadAsync(accessor.SelectAsync<Log>(criteria));
			Assert.Equal(rows.Length, stored.Count);

			foreach(var row in rows)
			{
				var actual = Assert.Single(stored, item => item.Action == row.Action);
				Assert.Equal(row.LogId, actual.LogId);
				Assert.Equal(row.Timestamp, actual.Timestamp);
			}

			if(returning)
			{
				Assert.True(options.HasReturning(out var result));
				Assert.Equal(rows.Length, result.Rows.Count);

				for(int index = 0; index < rows.Length; index++)
				{
					Assert.True(result.Rows[index].TryGetValue(nameof(Log.LogId), out var id));
					Assert.Equal(rows[index].LogId, Convert.ToUInt64(id));
					Assert.True(result.Rows[index].TryGetValue(nameof(Log.Action), out var action));
					Assert.Equal(rows[index].Action, action);
				}
			}
		}
		finally { await DeleteLogsAsync(accessor, marker); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpsertManyUpdatesEveryBatchAsync(bool streaming)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var rows = CreateUsers();
		var criteria = Condition.Between(nameof(UserModel.UserId), 1500000, 1500512);
		var options = DataUpsertOptions.Sequence(DataSequenceBehavior.Never);

		try
		{
			Assert.Equal(rows.Length, await accessor.InsertManyAsync(rows, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));

			foreach(var row in rows)
				row.Description = $"Updated-{row.UserId}";

			var count = streaming ?
				await accessor.UpsertManyAsync(Stream(rows), options) :
				await accessor.UpsertManyAsync(rows, options);
			Assert.True(count >= rows.Length);
			var stored = await ReadAsync(accessor.SelectAsync<UserModel>(criteria));
			Assert.Equal(rows.Length, stored.Count);
			Assert.All(stored, row => Assert.Equal($"Updated-{row.UserId}", row.Description));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InsertManyRollsBackEarlierBatchesOnDuplicateAsync(bool streaming)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var rows = CreateUsers();
		rows[256].UserId = rows[0].UserId;
		var criteria = Condition.Between(nameof(UserModel.UserId), 1500000, 1500512);
		var options = DataInsertOptions.Sequence(DataSequenceBehavior.Never);

		try
		{
			await Assert.ThrowsAsync(constraintException ?? typeof(DataConstraintException), () => streaming ?
				accessor.InsertManyAsync(Stream(rows), options).AsTask() :
				accessor.InsertManyAsync(rows, options).AsTask());
			Assert.Empty(await ReadAsync(accessor.SelectAsync<UserModel>(criteria)));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Fact]
	public async Task InsertManyRollsBackEarlierBatchesOnCancellationAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		using var cancellation = new CancellationTokenSource();
		var rows = CreateUsers();
		var criteria = Condition.Between(nameof(UserModel.UserId), 1500000, 1500512);

		try
		{
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => accessor.InsertManyAsync(
				Stream(rows, cancellation), DataInsertOptions.Sequence(DataSequenceBehavior.Never), cancellation.Token).AsTask());
			Assert.Empty(await ReadAsync(accessor.SelectAsync<UserModel>(criteria)));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Fact]
	public async Task InsertManySplitsChildrenAndPreservesParentLinksAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var rows = Model.Build<RoleModel>(2, (row, index) =>
		{
			row.RoleId = (uint)(1600000 + index);
			row.Name = $"BatchRole-{index}";
			row.Children = Model.Build<MemberModel>(800, (child, offset) =>
			{
				child.MemberId = (uint)(1700000 + offset);
				child.MemberType = MemberType.User;
			}).ToArray();
		}).ToArray();
		var criteria = Condition.Between(nameof(RoleModel.RoleId), 1600000, 1600001);

		try
		{
			Assert.Equal(1602, await accessor.InsertManyAsync(rows, "*,Children{*}", DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			var stored = await ReadAsync(accessor.SelectAsync<MemberModel>(criteria));
			Assert.Equal(1600, stored.Count);

			foreach(var row in rows)
			{
				Assert.All(row.Children, child => Assert.Equal(row.RoleId, child.RoleId));
				Assert.Equal(Enumerable.Range(1700000, 800).Select(id => (uint)id),
					stored.Where(child => child.RoleId == row.RoleId).Select(child => child.MemberId).Order());
			}
		}
		finally
		{
			await accessor.DeleteAsync<MemberModel>(criteria);
			await accessor.DeleteAsync<RoleModel>(criteria);
		}
	}

	[Fact]
	public async Task UpdateSplitsChildUpsertsAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var role = Model.Build<RoleModel>();
		role.RoleId = 1600000;
		role.Name = "BatchRole";
		var criteria = Condition.Equal(nameof(RoleModel.RoleId), role.RoleId);

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(role, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			role.Description = "Updated";
			role.Children = Model.Build<MemberModel>(800, (child, index) =>
			{
				child.MemberId = (uint)(1700000 + index);
				child.MemberType = MemberType.User;
			}).ToArray();

			Assert.Equal(801, await accessor.UpdateAsync(role, criteria, "*,Children{*}"));
			var stored = await ReadAsync(accessor.SelectAsync<MemberModel>(criteria));
			Assert.Equal(Enumerable.Range(1700000, 800).Select(id => (uint)id), stored.Select(child => child.MemberId).Order());
			Assert.All(role.Children, child => Assert.Equal(role.RoleId, child.RoleId));
		}
		finally
		{
			await accessor.DeleteAsync<MemberModel>(criteria);
			await accessor.DeleteAsync<RoleModel>(criteria);
		}
	}

	[Fact]
	public async Task InsertManyIgnoresExistingRowsAcrossBatchesAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var rows = CreateUsers();
		var criteria = Condition.Between(nameof(UserModel.UserId), 1500000, 1500512);

		try
		{
			Assert.Equal(256, await accessor.InsertManyAsync(rows.Take(256), DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(257, await accessor.InsertManyAsync(rows, DataInsertOptions.Sequence(DataSequenceBehavior.Never).IgnoreConstraint()));
			var stored = await ReadAsync(accessor.SelectAsync<UserModel>(criteria));
			Assert.Equal(rows.Select(row => row.UserId).Order(), stored.Select(row => row.UserId).Order());
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}
	#endregion

	#region 私有方法
	private static async ValueTask DeleteLogsAsync(IDataAccess accessor, string marker)
	{
		var command = Mapping.Commands.Add($"DeleteBatchLogs_{marker}", DataCommandMutability.Delete)
			.Script("MsSql", "DELETE FROM [Log] WHERE [Target]=@Target")
			.Script("MySql", "DELETE FROM `Log` WHERE `Target`=@Target")
			.Script("PostgreSql", "DELETE FROM \"Log\" WHERE \"Target\"=@Target")
			.Script("SQLite", "DELETE FROM \"Log\" WHERE \"Target\"=@Target")
			.Script("DuckDB", "DELETE FROM \"Log\" WHERE \"Target\"=$Target")
			.Parameter("Target", DataType.String);

		try
		{
			await accessor.ExecuteAsync(command.QualifiedName, [new Parameter("Target", marker)]);
		}
		finally
		{
			Mapping.Commands.Remove(command.QualifiedName);
		}
	}

	private static UserModel[] CreateUsers() => Model.Build<UserModel>(513, (row, index) =>
	{
		row.UserId = (uint)(1500000 + index);
		row.Name = $"BatchUser-{index}";
	}).ToArray();

	private static async IAsyncEnumerable<T> Stream<T>(IEnumerable<T> source, CancellationTokenSource stop = null, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		var index = 0;

		foreach(var item in source)
		{
			if(index++ == 256 && stop != null)
				stop.Cancel();

			cancellation.ThrowIfCancellationRequested();
			yield return item;
		}

		await Task.CompletedTask;
	}

	private static async Task<List<T>> ReadAsync<T>(IAsyncEnumerable<T> source)
	{
		var result = new List<T>();

		await foreach(var item in source)
			result.Add(item);

		return result;
	}
	#endregion
}
