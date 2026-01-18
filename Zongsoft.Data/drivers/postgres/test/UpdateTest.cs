using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class UpdateTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task UpdateAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());

		var count = await accessor.UpdateAsync<UserModel>(new
		{
			Name = "Popeye Zhong",
		}, Condition.Equal(nameof(UserModel.UserId), 100));
		Assert.Equal(1, count);

		var result = accessor.SelectAsync<string>(
			Model.Naming.Get<UserModel>(),
			Condition.Equal(nameof(UserModel.UserId), 100),
			nameof(UserModel.Name));

		var enumerator = result.GetAsyncEnumerator();
		Assert.True(await enumerator.MoveNextAsync());
		var name = enumerator.Current;
		await enumerator.DisposeAsync();
		Assert.Equal("Popeye Zhong", name);

		var options = DataUpdateOptions
			.Return(ReturningKind.Newer, nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Return(ReturningKind.Older, nameof(UserModel.Name), nameof(UserModel.Enabled))
			.Build();

		count = await accessor.UpdateAsync<UserModel>(new
		{
			Name = "Popeye",
			Enabled = true,
		}, Condition.Equal(nameof(UserModel.UserId), 100), options);

		Assert.Equal(1, count);
		Assert.True(options.HasReturning(out var returning));
		Assert.Single(returning.Rows);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Newer, out var value));
		Assert.Equal("Popeye", value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Name), ReturningKind.Older, out value));
		Assert.Equal("Popeye Zhong", value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Newer, out value));
		Assert.Equal(true, value);
		Assert.True(returning.Rows[0].TryGetValue(nameof(UserModel.Enabled), ReturningKind.Older, out value));
		Assert.Equal(true, value);
	}

	[Fact]
	public async Task UpdateEmployeeWithUserAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
			model.Nickname = "Popeye Zhong";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());
		Assert.Equal(1, count);

		count = await accessor.InsertAsync(Model.Build<Employee>(model => {
			model.TenantId = 1;
			model.BranchId = 0;
			model.UserId = 100;
			model.FullName = "Popeye Zhong";
			model.EmployeeNo = "A101";
			model.EmployeeCode = "X101";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());
		Assert.Equal(1, count);

		count = await accessor.UpdateAsync<Employee>(new
		{
			FullName = "Popeye.Zhong",
			User = new
			{
				Name = "Popeye.New",
			},
		}, Condition.Equal(nameof(Employee.TenantId), 1) & Condition.Equal(nameof(Employee.UserId), 100), $"*,{nameof(Employee.User)}{{*}}");
		Assert.Equal(2, count);

		#if NET10_0_OR_GREATER
		var employee = await accessor.SelectAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.Equal(nameof(Employee.UserId), 100), $"*,{nameof(Employee.User)}{{*}}").FirstOrDefaultAsync();
		#else
		var employee = accessor.SelectAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.Equal(nameof(Employee.UserId), 100), $"*,{nameof(Employee.User)}{{*}}").ToBlockingEnumerable().FirstOrDefault();
		#endif

		Assert.NotNull(employee);
		Assert.Equal(1U, employee.TenantId);
		Assert.Equal(0U, employee.BranchId);
		Assert.Equal(100U, employee.UserId);
		Assert.Equal("A101", employee.EmployeeNo);
		Assert.Equal("X101", employee.EmployeeCode);
		Assert.Equal("Popeye.Zhong", employee.FullName);
		Assert.NotNull(employee.User);
		Assert.Equal(100U, employee.User.UserId);
		Assert.Equal("Popeye.New", employee.User.Name);
		Assert.Equal("Popeye Zhong", employee.User.Nickname);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<Employee>(Condition.Equal(nameof(UserModel.UserId), 100));
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
	}
}
