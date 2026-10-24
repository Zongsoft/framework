using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public abstract class ReturningMutationTestBase(IDataAccess accessor, bool enabled, bool older, bool upsert)
{
	[Theory]
	[InlineData(1)]
	[InlineData(257)]
	public async Task UpdatePreservesTargetsWhenPredicateChangesAsync(int count)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var marker = Guid.NewGuid().ToString("N");
		var criteria = Condition.Equal(nameof(UserModel.Namespace), marker);
		var users = Model.Build<UserModel>(count, (user, index) =>
		{
			user.UserId = (uint)(1600000 + index);
			user.Name = $"Returning-{index}";
			user.Namespace = marker;
			user.Enabled = false;
		}).ToArray();
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Enabled), nameof(UserModel.Description))
			.Return(ReturningKind.Older, nameof(UserModel.Enabled)).Build();

		try
		{
			Assert.Equal(count, await accessor.InsertManyAsync(users, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(count, await accessor.UpdateAsync<UserModel>(new { Enabled = true, Description = (string)null },
				criteria & Condition.Equal(nameof(UserModel.Enabled), false), options));
			Assert.Equal(count, options.Returning.Rows.Count);

			Assert.True(options.Returning.Columns.TryGetValue("enabled", ReturningKind.Newer, out var column));
			Assert.Equal(nameof(UserModel.Enabled), column.Name);
			Assert.Equal(ReturningKind.Newer, column.Kind);
			Assert.True(options.Returning.Columns.TryGetValue("enabled", ReturningKind.Older, out column));
			Assert.Equal(ReturningKind.Older, column.Kind);

			var identifiers = new HashSet<uint>();

			foreach(var row in options.Returning.Rows)
			{
				Assert.True(row.TryGetValue(nameof(UserModel.UserId), ReturningKind.Newer, out var identity));
				Assert.True(identifiers.Add(Convert.ToUInt32(identity)));
				Assert.True(row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out var current));
				Assert.True(Convert.ToBoolean(current));
				Assert.Equal(older, row.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out var previous));

				if(older)
					Assert.False(Convert.ToBoolean(previous));

				Assert.True(row.TryGetValue(nameof(UserModel.Description), ReturningKind.Newer, out var description));
				Assert.Null(description);
			}

			Assert.Equal(users.Select(user => user.UserId).Order(), identifiers.Order());
			await foreach(var user in accessor.SelectAsync<UserModel>(criteria))
				Assert.True(user.Enabled);
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Fact]
	public async Task DeleteReturnsEveryCompositeKeyAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var criteria = Condition.Equal(nameof(Employee.UserId), 1601000);
		var options = DataDeleteOptions.Return(nameof(Employee.TenantId), nameof(Employee.UserId), nameof(Employee.FullName)).Build();

		try
		{
			for(uint tenant = 1; tenant <= 3; tenant++)
				Assert.Equal(1, await accessor.InsertAsync(CreateEmployee(tenant, 1601000), DataInsertOptions.Sequence(DataSequenceBehavior.Never)));

			Assert.Equal(2, await accessor.DeleteAsync<Employee>(criteria & Condition.LessThan(nameof(Employee.TenantId), 3), options));
			Assert.Equal(2, options.Returning.Rows.Count);

			Assert.True(options.Returning.Columns.TryGetValue("fullname", out var column));
			Assert.Equal(nameof(Employee.FullName), column.Name);
			Assert.Equal(ReturningKind.Older, column.Kind);
			Assert.False(options.Returning.Columns.TryGetValue("fullname", ReturningKind.Newer, out column));
			Assert.Equal(default, column);

			var tenants = new HashSet<uint>();

			foreach(var row in options.Returning.Rows)
			{
				Assert.True(row.TryGetValue(nameof(Employee.TenantId), out var tenant));
				Assert.True(tenants.Add(Convert.ToUInt32(tenant)));
				Assert.True(row.TryGetValue(nameof(Employee.FullName), out var name));
				Assert.Equal("Original", name);
			}

			Assert.Equal(new uint[] { 1, 2 }, tenants.Order());
			Assert.True(await accessor.ExistsAsync<Employee>(criteria & Condition.Equal(nameof(Employee.TenantId), 3)));
		}
		finally { await accessor.DeleteAsync<Employee>(criteria); }
	}

	[Fact]
	public async Task AssociatedUpdateReturnsOnlyRootRowsAndUpdatesSharedTargetOnceAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var criteria = Condition.Equal(nameof(Employee.UserId), 1602000);
		var user = Model.Build<UserModel>(user => { user.UserId = 1602000; user.Name = "Original"; });
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Employee.TenantId), nameof(Employee.FullName))
			.Return(ReturningKind.Older, nameof(Employee.FullName)).Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.InsertAsync(CreateEmployee(1, user.UserId), DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.InsertAsync(CreateEmployee(2, user.UserId), DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			var count = await accessor.UpdateAsync<Employee>(new { FullName = "Updated", User = new { Name = "Linked" } },
				criteria & Condition.Equal(nameof(Employee.FullName), "Original"), "FullName,User{Name}", options);
			Assert.Equal(3, count);
			Assert.Equal(2, options.Returning.Rows.Count);

			foreach(var row in options.Returning.Rows)
			{
				Assert.True(row.TryGetValue(nameof(Employee.FullName), ReturningKind.Newer, out var current));
				Assert.Equal("Updated", current);
				Assert.Equal(older, row.TryGetValue(nameof(Employee.FullName), ReturningKind.Older, out var previous));

				if(older)
					Assert.Equal("Original", previous);
			}

			await foreach(var actual in accessor.SelectAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId)))
				Assert.Equal("Linked", actual.Name);
		}
		finally
		{
			await accessor.DeleteAsync<Employee>(criteria);
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
		}
	}

	[Fact]
	public async Task EmptyAndCanceledMutationsDoNotPublishRowsAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var criteria = Condition.Equal(nameof(UserModel.UserId), int.MaxValue);
		var update = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name)).Build();
		var delete = DataDeleteOptions.Return(nameof(UserModel.Name)).Build();
		Assert.Equal(0, await accessor.UpdateAsync<UserModel>(new { Name = "Missing" }, criteria, update));
		Assert.Equal(0, await accessor.DeleteAsync<UserModel>(criteria, delete));
		Assert.Empty(update.Returning.Rows);
		Assert.Empty(delete.Returning.Rows);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await accessor.UpdateAsync<UserModel>(new { Name = "Canceled" }, criteria, update, cancellation.Token));
		Assert.Empty(update.Returning.Rows);
	}

	[Fact]
	public async Task InsertAndUpsertRespectReturningCapabilitiesAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1609101; user.Name = "Inserted"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var insertion = DataInsertOptions.Return(nameof(UserModel.UserId), nameof(UserModel.Name))
			.Sequence(DataSequenceBehavior.Never).Build();
		var upsertion = DataUpsertOptions.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name))
			.Return(ReturningKind.Older, nameof(UserModel.Name)).Sequence(DataSequenceBehavior.Never).Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, insertion));

			if(upsert)
			{
				var row = Assert.Single(insertion.Returning.Rows);
				Assert.True(row.TryGetValue(nameof(UserModel.UserId), out var identity));
				Assert.Equal(user.UserId, Convert.ToUInt32(identity));

				Assert.True(row.TryGetValue(nameof(UserModel.Name), out var name));
				Assert.Equal("Inserted", name);
			}
			else
				Assert.Empty(insertion.Returning.Rows);

			user.Name = "Updated";
			Assert.True(await accessor.UpsertAsync(user, upsertion) > 0);

			if(upsert)
			{
				var row = Assert.Single(upsertion.Returning.Rows);
				Assert.True(row.TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var current));
				Assert.Equal("Updated", current);

				Assert.Equal(older, row.TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out var previous));
				if(older)
					Assert.Equal("Inserted", previous);
			}
			else
				Assert.Empty(upsertion.Returning.Rows);

			Assert.True(await accessor.ExistsAsync<UserModel>(criteria & Condition.Equal(nameof(UserModel.Name), "Updated")));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Fact]
	public async Task IgnoredUpsertConflictProducesNoReturningRowAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		Assert.SkipUnless(upsert, "The driver does not provide native upsert returning.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1603000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpsertOptions.Return(ReturningKind.Newer, nameof(UserModel.UserId), nameof(UserModel.Name))
			.Sequence(DataSequenceBehavior.Never).IgnoreConstraint().Build();

		try
		{
			Assert.Equal(1, await accessor.UpsertAsync(user, options));
			Assert.Single(options.Returning.Rows);
			options = DataUpsertOptions.Return(ReturningKind.Newer, nameof(UserModel.UserId))
				.Sequence(DataSequenceBehavior.Never).IgnoreConstraint().Build();
			user.Name = "Ignored";
			Assert.Equal(0, await accessor.UpsertAsync(user, options));
			Assert.Empty(options.Returning.Rows);
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailedExecutionRollsBackAndDoesNotPublishReturningAsync(bool canceled)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1604000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name)).Build();
		var provider = Assert.IsType<DataAccess>(accessor).Provider;
		var executor = provider.Executor;
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			provider.Executor = new ObservingExecutor(executor, () =>
			{
				Assert.Empty(options.Returning.Rows);
				if(canceled)
				{
					cancellation.Cancel();
					cancellation.Token.ThrowIfCancellationRequested();
				}

				throw failure;
			});

			if(canceled)
				await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await accessor.UpdateAsync<UserModel>(new { Name = "Updated" }, criteria, options, cancellation.Token));
			else
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(async () => await accessor.UpdateAsync<UserModel>(new { Name = "Updated" }, criteria, options)));

			Assert.Empty(options.Returning.Rows);
			provider.Executor = executor;
			await foreach(var actual in accessor.SelectAsync<UserModel>(criteria))
				Assert.Equal("Original", actual.Name);
		}
		finally
		{
			provider.Executor = executor;
			await accessor.DeleteAsync<UserModel>(criteria);
		}
	}

	[Fact]
	public async Task ReturningDoesNotCommitCallerTransactionAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1605000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name)).Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));

			await using(var transaction = new Transaction())
			{
				Assert.Equal(1, await accessor.UpdateAsync<UserModel>(new { Name = "Uncommitted" }, criteria, options));
				Assert.Single(options.Returning.Rows);
				await transaction.RollbackAsync();
			}

			await foreach(var actual in accessor.SelectAsync<UserModel>(criteria))
				Assert.Equal("Original", actual.Name);
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	[Fact]
	public async Task MultipleNavigationsPreserveCompleteCompositeLinksAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var employee = CreateEmployee(1606000, 1606000);
		employee.BranchId = 1;
		var user = Model.Build<UserModel>(user => { user.UserId = employee.UserId; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(Employee.UserId), employee.UserId);
		var branches = Model.Build<Branch>(2, (branch, index) =>
		{
			branch.TenantId = (uint)(employee.TenantId + index);
			branch.BranchId = 1;
			branch.BranchNo = $"R{index}";
			branch.Name = "Original";
		}).ToArray();
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Employee.FullName)).Build();

		try
		{
			Assert.Equal(2, await accessor.InsertManyAsync(branches, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.InsertAsync(employee, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(3, await accessor.UpdateAsync<Employee>(new
			{
				FullName = "Updated",
				User = new { Name = "LinkedUser" },
				Branch = new { Name = "LinkedBranch" },
			}, criteria, "FullName,User{Name},Branch{Name}", options));
			Assert.Single(options.Returning.Rows);

			await foreach(var branch in accessor.SelectAsync<Branch>(Condition.Between(nameof(Branch.TenantId), employee.TenantId, employee.TenantId + 1)))
				Assert.Equal(branch.TenantId == employee.TenantId ? "LinkedBranch" : "Original", branch.Name);
		}
		finally
		{
			await accessor.DeleteAsync<Employee>(criteria);
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
			await accessor.DeleteAsync<Branch>(Condition.Between(nameof(Branch.TenantId), employee.TenantId, employee.TenantId + 1));
		}
	}

	[Fact]
	public async Task NestedAssociationsDeduplicateTargetsAcrossBatchesAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		const uint TENANT = 1609100;
		const int COUNT = 129;
		var user = Model.Build<UserModel>(user => { user.UserId = TENANT - 1; user.Name = "Original"; });
		var branches = Model.Build<Branch>(COUNT, (branch, index) =>
		{
			branch.TenantId = TENANT;
			branch.BranchId = (uint)(index + 1);
			branch.BranchNo = $"R{index}";
			branch.Name = "Original";
			branch.PrincipalId = user.UserId;
		}).ToArray();
		var employees = Enumerable.Range(0, COUNT).Select(index =>
		{
			var employee = CreateEmployee(TENANT, (uint)(TENANT + index));
			employee.BranchId = (uint)(index + 1);
			return employee;
		}).ToArray();
		var criteria = Condition.Equal(nameof(Employee.TenantId), TENANT);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(Employee.FullName)).Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(COUNT, await accessor.InsertManyAsync(branches, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(COUNT, await accessor.InsertManyAsync(employees, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(COUNT * 2 + 1, await accessor.UpdateAsync<Employee>(new
			{
				FullName = "Updated",
				Branch = new { Name = "LinkedBranch", Principal = new { Name = "LinkedUser" } },
			}, criteria & Condition.Equal(nameof(Employee.FullName), "Original"), "FullName,Branch{Name,Principal{Name}}", options));
			Assert.Equal(COUNT, options.Returning.Rows.Count);
			Assert.Equal(COUNT, await accessor.CountAsync<Branch>(criteria & Condition.Equal(nameof(Branch.Name), "LinkedBranch")));
			Assert.True(await accessor.ExistsAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId) & Condition.Equal(nameof(UserModel.Name), "LinkedUser")));
		}
		finally
		{
			await accessor.DeleteAsync<Employee>(criteria);
			await accessor.DeleteAsync<Branch>(criteria);
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
		}
	}

	[Fact]
	public async Task ReturningResolvesMappedFieldNamesAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1608000; user.Name = "Original"; });
		var mapping = new DataEntity(null, $"Returning{Guid.NewGuid():N}")
		{
			Alias = Mapping.Entities[Model.Naming.Get<UserModel>()].GetTableName(),
		};
		mapping.Properties.Simplex("Id", DataType.UInt32, false).Alias = "UserId";
		mapping.Properties.Simplex("Label", DataType.String, 50, true).Alias = "Name";
		mapping.SetKey("Id");
		Mapping.Entities.Add(mapping);
		var criteria = Condition.Equal("Id", user.UserId);
		var update = DataUpdateOptions.Return(ReturningKind.Newer, "Label").Build();
		var delete = DataDeleteOptions.Return("Id", "Label").Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.UpdateAsync(mapping.Name, new { Label = "Mapped" }, criteria, "Label", update));
			Assert.True(Assert.Single(update.Returning.Rows).TryGetValue("Label", ReturningKind.Newer, out var label));
			Assert.Equal("Mapped", label);
			Assert.Equal(1, await accessor.DeleteAsync(mapping.Name, criteria, delete));
			Assert.True(Assert.Single(delete.Returning.Rows).TryGetValue("Id", out var identity));
			Assert.Equal(user.UserId, Convert.ToUInt32(identity));
		}
		finally
		{
			Mapping.Entities.Remove(mapping.Name);
			await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), user.UserId));
		}
	}

	[Fact]
	public async Task ProviderExecutorDecorationPreservesReturningAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user =>
		{
			user.UserId = 1610000;
			user.Name = "Original";
			user.Enabled = false;
		});
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var update = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Enabled)).Build();
		var delete = DataDeleteOptions.Return(nameof(UserModel.UserId), nameof(UserModel.Enabled)).Build();
		var provider = Assert.IsType<DataAccess>(accessor).Provider;
		var executor = provider.Executor;
		var executions = 0;

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			provider.Executor = new ObservingExecutor(executor, () => executions++);
			Assert.Equal(1, await accessor.UpdateAsync<UserModel>(new { Enabled = true }, criteria, update));
			Assert.True(Assert.Single(update.Returning.Rows).TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out var updated));
			Assert.True(Convert.ToBoolean(updated));
			Assert.Equal(1, executions);
			Assert.Equal(1, await accessor.DeleteAsync<UserModel>(criteria, delete));
			var row = Assert.Single(delete.Returning.Rows);
			Assert.True(row.TryGetValue(nameof(UserModel.UserId), out var identity));
			Assert.Equal(user.UserId, Convert.ToUInt32(identity));
			Assert.True(row.TryGetValue(nameof(UserModel.Enabled), out var deleted));
			Assert.True(Convert.ToBoolean(deleted));
			Assert.Equal(2, executions);
		}
		finally
		{
			provider.Executor = executor;
			await accessor.DeleteAsync<UserModel>(criteria);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FailedReturningPreservesPreviouslyPublishedRowsAsync(bool canceled)
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1611000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name)).Build();
		var provider = Assert.IsType<DataAccess>(accessor).Provider;
		var executor = provider.Executor;
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.UpdateAsync<UserModel>(new { Name = "Published" }, criteria, options));
			Assert.Single(options.Returning.Rows);
			provider.Executor = new ObservingExecutor(executor, () =>
			{
				Assert.Single(options.Returning.Rows);
				if(canceled)
				{
					cancellation.Cancel();
					cancellation.Token.ThrowIfCancellationRequested();
				}

				throw failure;
			});

			if(canceled)
				await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await accessor.UpdateAsync<UserModel>(new { Name = "Discarded" }, criteria, options, cancellation.Token));
			else
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(async () => await accessor.UpdateAsync<UserModel>(new { Name = "Discarded" }, criteria, options)));

			Assert.True(Assert.Single(options.Returning.Rows).TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var value));
			Assert.Equal("Published", value);
			provider.Executor = executor;
			Assert.True(await accessor.ExistsAsync<UserModel>(criteria & Condition.Equal(nameof(UserModel.Name), "Published")));
		}
		finally
		{
			provider.Executor = executor;
			await accessor.DeleteAsync<UserModel>(criteria);
		}
	}

	[Fact]
	public async Task ReturningRowsKeepNamesWhenColumnsChangeAsync()
	{
		Assert.SkipUnless(enabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var user = Model.Build<UserModel>(user => { user.UserId = 1612000; user.Name = "Original"; });
		var criteria = Condition.Equal(nameof(UserModel.UserId), user.UserId);
		var options = DataUpdateOptions.Return(ReturningKind.Newer, nameof(UserModel.Name)).Build();

		try
		{
			Assert.Equal(1, await accessor.InsertAsync(user, DataInsertOptions.Sequence(DataSequenceBehavior.Never)));
			Assert.Equal(1, await accessor.UpdateAsync<UserModel>(new { Name = "First" }, criteria, options));
			var first = Assert.Single(options.Returning.Rows);
			Assert.True(options.Returning.Columns.Prepend(nameof(UserModel.Enabled), ReturningKind.Newer));
			Assert.Equal(1, await accessor.UpdateAsync<UserModel>(new { Name = "Second", Enabled = true }, criteria, options));
			Assert.Equal(2, options.Returning.Rows.Count);
			Assert.True(first.TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var name));
			Assert.Equal("First", name);
			Assert.False(first.TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out _));
			Assert.True(options.Returning.Rows[1].TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out name));
			Assert.Equal("Second", name);
			Assert.True(options.Returning.Rows[1].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out var enabledValue));
			Assert.True(Convert.ToBoolean(enabledValue));
		}
		finally { await accessor.DeleteAsync<UserModel>(criteria); }
	}

	private static Employee CreateEmployee(uint tenant, uint user) => Model.Build<Employee>(employee =>
	{
		employee.TenantId = tenant;
		employee.UserId = user;
		employee.BranchId = 0;
		employee.CreatorId = user;
		employee.FullName = "Original";
	});

	private sealed class ObservingExecutor(IDataExecutor executor, Action observe) : IDataExecutor
	{
		public async ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation)
		{
			await executor.ExecuteAsync(context, statement, cancellation);
			observe();
		}
	}
}
