using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.SQLite.Tests;

[Collection("Database")]
public class InsertTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task InsertLogAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var log = Model.Build<Log>(log =>
		{
			log.UserId = 1;
			log.Target = "MyTarget";
			log.Action = "MyAction";
			log.TenantId = 1;
			log.BranchId = 0;
			log.Timestamp = DateTime.Now;
		});

		var count = await accessor.InsertAsync(log);
		Assert.Equal(1, count);
		Assert.True(log.LogId > 0);

		await accessor.ExecuteAsync("TruncateLog");
	}

	[Fact]
	public async Task InsertLogsAsync()
	{
		const int COUNT = 10;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var logs = Model.Build<Log>(COUNT, (log, index) =>
		{
			log.UserId = 1;
			log.Target = $"MyTarget#{index}";
			log.Action = $"MyAction#{index}";
			log.TenantId = 1;
			log.BranchId = 0;
		}).ToArray();

		var count = await accessor.InsertManyAsync(logs);
		Assert.Equal(COUNT, count);

		for(int i = 0; i < COUNT; i++)
			Assert.True(logs[i].LogId > 0);

		await accessor.ExecuteAsync("TruncateLog");
	}

	[Fact]
	public async Task InsertLogModelAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		//获取数据模型描述器，然后将其转换成数据实体
		var entity = Model.GetDescriptor<LogModel>().ToEntity();
		//尝试将数据实体加入到映射集中
		Mapping.Entities.TryAdd(entity);

		var accessor = _database.Accessor;
		var log = Model.Build<LogModel>(log =>
		{
			log.UserId = 1;
			log.Target = "MyTarget";
			log.Action = "MyAction";
			log.TenantId = 1;
			log.BranchId = 0;
		});

		var count = await accessor.InsertAsync(log);
		Assert.Equal(1, count);
		Assert.True(log.LogId > 0);

		var model = accessor.Select<LogModel>(
			Condition.Equal(nameof(LogModel.LogId), log.LogId),
			$"*,{nameof(LogModel.User)}{{*}}",
			Paging.Limit(1)).FirstOrDefault();

		Assert.NotNull(model);
		Assert.Equal(log.LogId, model.LogId);
		Assert.Equal(log.UserId, model.UserId);
		Assert.Equal(log.Target, model.Target);
		Assert.Equal(log.Action, model.Action);
		Assert.True(model.Timestamp >= DateTime.Today);
		Assert.NotNull(model.User);
		Assert.NotNull(model.User.Name);
		Assert.NotEmpty(model.User.Name);
		Assert.Equal(model.UserId, model.User.UserId);

		await accessor.ExecuteAsync("TruncateLog");
	}

	[Fact]
	public async Task InsertLogModelsAsync()
	{
		const int COUNT = 10;

		if(!Global.IsTestingEnabled)
			return;

		//获取数据模型描述器，然后将其转换成数据实体
		var entity = Model.GetDescriptor<LogModel>().ToEntity();
		//尝试将数据实体加入到映射集中
		Mapping.Entities.TryAdd(entity);

		var accessor = _database.Accessor;
		var logs = Model.Build<LogModel>(COUNT, (log, index) =>
		{
			log.UserId = 1;
			log.Target = $"MyTarget#{index}";
			log.Action = $"MyAction#{index}";
			log.TenantId = 1;
			log.BranchId = 0;
		}).ToArray();

		var count = await accessor.InsertManyAsync(logs);
		Assert.Equal(COUNT, count);

		for(int i = 0; i < COUNT; i++)
			Assert.True(logs[i].LogId > 0);

		var models = accessor.Select<LogModel>(
			Condition.In(nameof(LogModel.LogId), logs.Select(log => log.LogId)));

		foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.True(model.LogId > 0);
			Assert.True(model.Timestamp >= DateTime.Today);
			Assert.StartsWith("MyTarget", model.Target);
			Assert.StartsWith("MyAction", model.Action);
		}

		await accessor.ExecuteAsync("TruncateLog");
	}

	[Fact]
	public async Task InsertDepartmentsAsync()
	{
		const int COUNT = 10;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var departments = Model.Build<Department>(COUNT, (department, index) =>
		{
			department.TenantId = 1;
			department.BranchId = 0;
			department.DepartmentNo = $"No.{index}";
			department.Name = $"MyDepartment#{index}";
		}).ToArray();

		var count = await accessor.InsertManyAsync(departments);
		Assert.Equal(COUNT, count);

		for(int i = 0; i < COUNT; i++)
			Assert.True(departments[i].DepartmentId > 0);

		var models = accessor.SelectAsync<Department>(
			Condition.Equal(nameof(Department.TenantId), 1) &
			Condition.Equal(nameof(Department.BranchId), 0));

		await foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.Equal(1U, model.TenantId);
			Assert.Equal(0U, model.BranchId);
			Assert.True(model.DepartmentId > 0);
			Assert.StartsWith("No.", model.DepartmentNo);
			Assert.StartsWith("MyDepartment", model.Name);
		}

		await accessor.DeleteAsync<Department>(
			Condition.Equal(nameof(Department.TenantId), 1) &
			Condition.Equal(nameof(Department.BranchId), 0) &
			Condition.In(nameof(Department.DepartmentId), departments.Select(department => department.DepartmentId).ToArray()));
	}

	[Fact]
	public async Task InsertAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		Assert.Equal(0, await accessor.InsertAsync<UserModel>(null));
		Assert.Equal(0, await accessor.InsertAsync<RoleModel>(null, $"*, {nameof(RoleModel.Children)}{{*}}"));
		Assert.Equal(0, await accessor.InsertAsync<Employee>(null, $"*, {nameof(Employee.User)}{{*}}"));

		await accessor.DeleteAsync<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));

		var count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye";
		}), DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);

		count = await accessor.InsertAsync(Model.Build<UserModel>(model => {
			model.UserId = 100;
			model.Name = "Popeye Zhong";
		}), DataInsertOptions.SuppressSequence().IgnoreConstraint());
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task InsertWithOneAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<Employee>(model => {
			model.TenantId = 1;
			model.BranchId = 0;
			model.UserId = 100;
			model.FullName = "Boss Zhong";
			model.User = Model.Build<UserModel>(user => {
				user.UserId = 100;
				user.Name = "Popeye";
				user.Nickname = "Popeye Zhong";
			});
		});

		var count = await accessor.InsertAsync(model, $"*,{nameof(Employee.User)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(2, count);

		var employees = accessor.SelectAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.Equal(nameof(Employee.UserId), 100),
			$"*,{nameof(Employee.User)}{{*}}");

		await using var enumerator = employees.GetAsyncEnumerator(CancellationToken.None);
		Assert.True(await enumerator.MoveNextAsync());

		var employee = enumerator.Current;
		Assert.NotNull(employee);
		Assert.Equal(100U, employee.UserId);
		Assert.Equal("Boss Zhong", employee.FullName);
		Assert.NotNull(employee.User);
		Assert.Equal(100U, employee.User.UserId);
		Assert.Equal("Popeye", employee.User.Name);
		Assert.Equal("Popeye Zhong", employee.User.Nickname);

		model = Model.Build<Employee>(model => {
			model.TenantId = 1;
			model.BranchId = 0;
			model.UserId = 404;
			model.FullName = "Unnamed";
		});

		//必须先释放掉枚举器，否则会因为占用连接而导致后续的插入操作失败
		await enumerator.DisposeAsync();

		count = await accessor.InsertAsync(model, $"*,{nameof(Employee.User)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);
		Assert.True(await accessor.ExistsAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), model.TenantId) &
			Condition.Equal(nameof(Employee.UserId), model.UserId)));
	}

	[Fact]
	public async Task InsertWithChildrenAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<RoleModel>(model => {
			model.RoleId = 10;
			model.Name = "Managers";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = 100;
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = 404;
					member.MemberType = MemberType.Role;
				}),
			];
		});

		var count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(3, count);
		Assert.NotNull(model.Children);
		Assert.NotEmpty(model.Children);
		foreach(var child in model.Children)
			Assert.Equal(10U, child.RoleId);

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Equal(nameof(RoleModel.RoleId), 10),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		await using var enumerator = roles.GetAsyncEnumerator(CancellationToken.None);
		Assert.True(await enumerator.MoveNextAsync());

		var role = enumerator.Current;
		Assert.NotNull(role);
		Assert.Equal(10U, role.RoleId);
		Assert.Equal("Managers", role.Name);
		Assert.NotNull(role.Children);

		var members = role.Children.OrderBy(member => member.MemberId).ToArray();
		Assert.NotEmpty(members);
		Assert.Equal(2, members.Length);
		Assert.Equal(10U, members[0].RoleId);
		Assert.Equal(100U, members[0].MemberId);
		Assert.Equal(MemberType.User, members[0].MemberType);
		Assert.Equal(10U, members[1].RoleId);
		Assert.Equal(404U, members[1].MemberId);
		Assert.Equal(MemberType.Role, members[1].MemberType);

		//必须先释放掉枚举器，否则会因为占用连接而导致后续的插入操作失败
		await enumerator.DisposeAsync();

		model = Model.Build<RoleModel>(model => {
			model.RoleId = 11;
			model.Name = $"Role#{Random.Shared.Next():X}";
		});
		count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);
		Assert.True(await accessor.ExistsAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), model.RoleId)));

		model = Model.Build<RoleModel>(model => {
			model.RoleId = 12;
			model.Name = $"Role#{Random.Shared.Next():X}";
			model.Children = [];
		});
		count = await accessor.InsertAsync(model, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(1, count);
		Assert.True(await accessor.ExistsAsync<RoleModel>(Condition.Equal(nameof(RoleModel.RoleId), model.RoleId)));
	}

	[Fact]
	public async Task InsertWithDepartmentsAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var model = Model.Build<Branch>(model => {
			model.TenantId = 1;
			model.BranchId = 10;
			model.BranchNo = "B01";
			model.Name = "My Branch";
			model.Departments =
			[
				Model.Build<Department>(department => {
					department.TenantId = model.TenantId;
					department.BranchId = model.BranchId;
					department.DepartmentNo = "A1";
					department.Name = "MyDepartment#1";
				}),
				Model.Build<Department>(department => {
					department.TenantId = model.TenantId;
					department.BranchId = model.BranchId;
					department.DepartmentNo = "A2";
					department.Name = "MyDepartment#2";
				}),
			];
		});

		var count = await accessor.InsertAsync(model, $"*,{nameof(model.Departments)}{{*}}");
		Assert.Equal(3, count);
		Assert.NotNull(model.Departments);
		Assert.NotEmpty(model.Departments);
		foreach(var department in model.Departments)
			Assert.True(department.DepartmentId > 0);

		var departments = accessor.SelectAsync<Department>(
			Condition.Equal(nameof(Department.TenantId), model.TenantId) &
			Condition.Equal(nameof(Department.BranchId), model.BranchId));

		await foreach(var department in departments)
		{
			Assert.NotNull(department);
			Assert.True(department.DepartmentId > 0);
			Assert.Equal(model.TenantId, department.TenantId);
			Assert.Equal(model.BranchId, department.BranchId);
			Assert.NotNull(department.DepartmentNo);
			Assert.NotEmpty(department.DepartmentNo);
			Assert.NotNull(department.Name);
			Assert.NotEmpty(department.Name);
		}

		await accessor.DeleteAsync<Branch>(
			Condition.Equal(nameof(Department.TenantId), model.TenantId) &
			Condition.Equal(nameof(Department.BranchId), model.BranchId), nameof(Branch.Departments));
	}

	[Fact]
	public async Task InsertManyAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		Assert.Equal(0, await accessor.InsertManyAsync<UserModel>(null));
		Assert.Equal(0, await accessor.InsertManyAsync(Array.Empty<UserModel>()));
		Assert.Equal(0, await accessor.InsertManyAsync<RoleModel>(null, $"*, {nameof(RoleModel.Children)}{{*}}"));
		Assert.Equal(0, await accessor.InsertManyAsync(Array.Empty<RoleModel>(), $"*, {nameof(RoleModel.Children)}{{*}}"));
		Assert.Equal(0, await accessor.InsertManyAsync<Employee>(null, $"*, {nameof(Employee.User)}{{*}}"));
		Assert.Equal(0, await accessor.InsertManyAsync(Array.Empty<Employee>(), $"*, {nameof(Employee.User)}{{*}}"));

		var count = await accessor.InsertManyAsync(Model.Build<UserModel>(COUNT, (model, index) => {
			model.UserId = (uint)(200 + index);
			model.Name = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
		}), DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task InsertManyWithOneAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var count = await accessor.InsertManyAsync(Model.Build<Employee>(COUNT, (model, index) => {
			model.TenantId = 1;
			model.BranchId = 0;
			model.UserId = (uint)(200 + index);
			model.FullName = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
			model.EmployeeNo = $"A{model.UserId}";
			model.EmployeeCode = $"X{model.UserId}";
			model.User = Model.Build<UserModel>(user =>
			{
				user.UserId = model.UserId;
				user.Name = model.FullName;
			});
		}), $"*,{nameof(Employee.User)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT * 2, count);

		var employees = accessor.SelectAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.In(nameof(Employee.UserId), Enumerable.Range(200, COUNT)), $"*,{nameof(Employee.User)}{{*}}");

		count = 0;

		await foreach(var employee in employees)
		{
			Assert.NotNull(employee);
			Assert.NotNull(employee.User);
			++count;
		}

		Assert.Equal(COUNT, count);

		await accessor.DeleteAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.In(nameof(Employee.UserId), Enumerable.Range(200, COUNT)), nameof(Employee.User));

		count = await accessor.InsertManyAsync(Model.Build<Employee>(COUNT, (model, index) => {
			model.TenantId = 1;
			model.BranchId = 0;
			model.UserId = (uint)(200 + index);
			model.FullName = $"${Zongsoft.Common.Randomizer.GenerateString()}_{index}";
			model.EmployeeNo = $"A{model.UserId}";
			model.EmployeeCode = $"X{model.UserId}";
		}), $"*,{nameof(Employee.User)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		await accessor.DeleteAsync<Employee>(
			Condition.Equal(nameof(Employee.TenantId), 1) &
			Condition.In(nameof(Employee.UserId), Enumerable.Range(200, COUNT)), nameof(Employee.User));
	}

	[Fact]
	public async Task InsertManyWithChildrenAsync1()
	{
		const int COUNT = 10;
		const int OFFSET = 1000;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<RoleModel>(COUNT, (model, index) => {
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"$Role#{(OFFSET + index)}";
			model.Children =
			[
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(501 + index);
					member.MemberType = MemberType.User;
				}),
				Model.Build<MemberModel>(member => {
					member.MemberId = (uint)(601 + index);
					member.MemberType = MemberType.Role;
				}),
			];
		}).ToArray();

		var count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(3 * COUNT, count);

		for(int i = 0; i < models.Length; i++)
		{
			var model = models[i];
			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);

			foreach(var child in model.Children)
				Assert.Equal((uint)(OFFSET + i), child.RoleId);
		}

		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT),
			$"*,{nameof(RoleModel.Children)}{{*}}");

		int index = 0;

		await foreach(var role in roles)
		{
			var id = (uint)(OFFSET + index);

			Assert.NotNull(role);
			Assert.Equal(id, role.RoleId);
			Assert.Equal($"$Role#{id}", role.Name);
			Assert.NotNull(role.Children);

			var members = role.Children.OrderBy(member => member.MemberId).ToArray();
			Assert.NotEmpty(members);
			Assert.Equal(2, members.Length);
			Assert.Equal(id, members[0].RoleId);
			Assert.Equal((uint)(501 + index), members[0].MemberId);
			Assert.Equal(MemberType.User, members[0].MemberType);
			Assert.Equal(id, members[1].RoleId);
			Assert.Equal((uint)(601 + index), members[1].MemberId);
			Assert.Equal(MemberType.Role, members[1].MemberType);

			++index;
		}

		await accessor.DeleteAsync<RoleModel>(Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT));
		await accessor.DeleteAsync<MemberModel>(Condition.Between(nameof(MemberModel.RoleId), OFFSET, OFFSET + COUNT));

		models = Model.Build<RoleModel>(COUNT, (model, index) => {
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"$Role#{(OFFSET + index)}";
		}).ToArray();
		count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		await accessor.DeleteAsync<RoleModel>(Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT));

		models = Model.Build<RoleModel>(COUNT, (model, index) => {
			model.RoleId = (uint)(OFFSET + index);
			model.Name = $"$Role#{(OFFSET + index)}";
			model.Children = [];
		}).ToArray();
		count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(COUNT, count);

		await accessor.DeleteAsync<RoleModel>(Condition.Between(nameof(RoleModel.RoleId), OFFSET, OFFSET + COUNT));
	}

	[Fact]
	public async Task InsertManyWithChildrenAsync2()
	{
		const int ROLE_OFFSET = 1000;
		const int MEMBER_OFFSET = 100;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = new RoleModel[]
		{
			Model.Build<RoleModel>(model =>
			{
				model.RoleId = ROLE_OFFSET;
				model.Name = $"$Role#{model.RoleId}";
			}),
			Model.Build<RoleModel>(model =>
			{
				model.RoleId = ROLE_OFFSET + 1;
				model.Name = $"$Role#{model.RoleId}";
				model.Children =
				[
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET + 1;
						member.MemberType = MemberType.Role;
					}),
				];
			}),
			Model.Build<RoleModel>(model =>
			{
				model.RoleId = ROLE_OFFSET + 2;
				model.Name = $"$Role#{model.RoleId}";
				model.Children =
				[
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET * 2 + 1;
						member.MemberType = MemberType.Role;
					}),
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET * 2 + 2;
						member.MemberType = MemberType.User;
					}),
				];
			}),
			Model.Build<RoleModel>(model =>
			{
				model.RoleId = ROLE_OFFSET + 3;
				model.Name = $"$Role#{model.RoleId}";
				model.Children =
				[
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET * 3 + 1;
						member.MemberType = MemberType.Role;
					}),
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET * 3 + 2;
						member.MemberType = MemberType.User;
					}),
					Model.Build<MemberModel>(member =>
					{
						member.MemberId = MEMBER_OFFSET * 3 + 3;
						member.MemberType = MemberType.User;
					}),
				];
			}),
		};

		var count = await accessor.InsertManyAsync(models, $"*,{nameof(RoleModel.Children)}{{*}}", DataInsertOptions.SuppressSequence());
		Assert.Equal(10, count);

		for(int i = 1; i < models.Length; i++)
		{
			var model = models[i];
			Assert.NotNull(model.Children);
			Assert.NotEmpty(model.Children);

			foreach(var child in model.Children)
				Assert.Equal((uint)(ROLE_OFFSET + i), child.RoleId);
		}

		#if NET10_0_OR_GREATER
		var roles = await accessor.SelectAsync<RoleModel>(
			Condition.Between(nameof(RoleModel.RoleId), ROLE_OFFSET, ROLE_OFFSET + models.Length - 1),
			$"*,{nameof(RoleModel.Children)}{{*}}",
			[Sorting.Ascending(nameof(RoleModel.RoleId))]).ToArrayAsync();
		#else
		var roles = accessor.SelectAsync<RoleModel>(
			Condition.Between(nameof(RoleModel.RoleId), ROLE_OFFSET, ROLE_OFFSET + models.Length - 1),
			$"*,{nameof(RoleModel.Children)}{{*}}",
			[Sorting.Ascending(nameof(RoleModel.RoleId))]).ToBlockingEnumerable().ToArray();
		#endif

		for(int i = 0; i < roles.Length; i++)
		{
			Assert.NotNull(roles[i]);
			Assert.Equal((uint)(ROLE_OFFSET + i), roles[i].RoleId);
			Assert.Equal($"$Role#{ROLE_OFFSET + i}", roles[i].Name);

			if(i == 0)
				Assert.Empty(roles[i].Children);
			else
			{
				Assert.NotNull(roles[i].Children);

				int index = 1;
				foreach(var child in roles[i].Children)
				{
					var memberId = MEMBER_OFFSET * i + index++;
					Assert.Equal(roles[i].RoleId, child.RoleId);
					Assert.Equal((uint)memberId, child.MemberId);
				}
			}
		}

		await accessor.DeleteAsync<RoleModel>(Condition.Between(nameof(RoleModel.RoleId), ROLE_OFFSET, ROLE_OFFSET + models.Length - 1));
		await accessor.DeleteAsync<MemberModel>(Condition.Between(nameof(MemberModel.RoleId), ROLE_OFFSET, ROLE_OFFSET + models.Length - 1));
	}

	[Fact]
	public async Task InsertManyWithDepartmentsAsync()
	{
		const int COUNT = 10;

		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var models = Model.Build<Branch>(COUNT, (model, index) => {
			model.TenantId = 1;
			model.BranchId = (uint)(index + 1);
			model.BranchNo = $"B{index + 1}";
			model.Name = $"My Branch #{index + 1}";
			model.Departments =
			[
				Model.Build<Department>(department => {
					department.TenantId = model.TenantId;
					department.BranchId = model.BranchId;
					department.DepartmentNo = "A1";
					department.Name = $"MyDepartment#1@{model.BranchNo}";
				}),
				Model.Build<Department>(department => {
					department.TenantId = model.TenantId;
					department.BranchId = model.BranchId;
					department.DepartmentNo = "A2";
					department.Name = $"MyDepartment#2@{model.BranchNo}";
				}),
			];
		}).ToArray();

		var count = await accessor.InsertManyAsync(models, $"*,{nameof(Branch.Departments)}{{*}}");
		Assert.Equal(3 * COUNT, count);

		foreach(var model in models)
		{
			Assert.NotNull(model);
			Assert.NotNull(model.Departments);
			Assert.NotEmpty(model.Departments);
			foreach(var department in model.Departments)
				Assert.True(department.DepartmentId > 0);
		}

		var branchIds = models.Select(model => model.BranchId).ToArray();

		var departments = accessor.SelectAsync<Department>(
			Condition.Equal(nameof(Department.TenantId), 1) &
			Condition.In(nameof(Department.BranchId), branchIds));

		await foreach(var department in departments)
		{
			Assert.NotNull(department);
			Assert.True(department.DepartmentId > 0);
			Assert.NotNull(department.DepartmentNo);
			Assert.NotEmpty(department.DepartmentNo);
			Assert.NotNull(department.Name);
			Assert.NotEmpty(department.Name);
		}

		await accessor.DeleteAsync<Branch>(
			Condition.Equal(nameof(Department.TenantId), 1) &
			Condition.In(nameof(Department.BranchId), branchIds), nameof(Branch.Departments));
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Delete<Employee>(Condition.In(nameof(Employee.UserId), [100, 404]));
		accessor.Delete<UserModel>(Condition.Equal(nameof(UserModel.UserId), 100));
		accessor.Delete<UserModel>(Condition.Like(nameof(UserModel.Name), "$%"));
		accessor.Delete<RoleModel>(Condition.In(nameof(RoleModel.RoleId), [10, 11, 12]));
		accessor.Delete<MemberModel>(Condition.Equal(nameof(MemberModel.RoleId), 10));
	}
}
