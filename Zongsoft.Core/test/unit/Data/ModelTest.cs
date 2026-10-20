using System;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Data.Tests;

public class ModelTest
{
	#region 常量定义
	private static readonly string NOTEXISTS = "NotExists!";
	private static readonly DateTime BIRTHDATE = DateTime.Now;
	#endregion

	#region 公共方法
	[Fact]
	public void TestPersonInterface()
	{
		// Dynamically build an instance of the IPerson interface.
		var person = Model.Build<Zongsoft.Tests.IPerson>();
		Assert.NotNull(person);
		Assert.Equal(typeof(IPerson), Model.GetModelType(person));
		this.TestPersonInterfaceCore(person);
	}

	[Fact]
	public void TestEmployeeInterface()
	{
		// Dynamically build an instance of the IEmployee interface.
		var employee = Model.Build<Zongsoft.Tests.IEmployee>();
		Assert.NotNull(employee);
		Assert.Equal(typeof(IEmployee), Model.GetModelType(employee));
		this.TestEmployeeInterfaceCore(employee);
	}

	[Fact]
	public void TestCustomerInterface()
	{
		// Dynamically build an instance of the ICustomer interface.
		var customer = Model.Build<Zongsoft.Tests.ICustomer>();
		Assert.NotNull(customer);
		Assert.Equal(typeof(ICustomer), Model.GetModelType(customer));
		this.TestCustomerInterfaceCore(customer);
	}

	[Fact]
	public void TestSpecialEmployeeInterface()
	{
		// Dynamically build an instance of the ISpecialEmployee interface.
		var special = Model.Build<Zongsoft.Tests.ISpecialEmployee>();
		Assert.NotNull(special);
		Assert.Equal(typeof(ISpecialEmployee), Model.GetModelType(special));
		this.TestSpecialEmployeeInterfaceCore(special);
	}

	[Fact]
	public void TestPersonClass()
	{
		// Dynamically build an instance of the PersonBase abstract class.
		var person = Model.Build<Zongsoft.Tests.PersonBase>();
		Assert.NotNull(person);
		Assert.Equal(typeof(PersonBase), Model.GetModelType(person));
		this.TestPersonClassCore(person);
	}

	[Fact]
	public void TestEmployeeClass()
	{
		// Dynamically build an instance of the EmployeeBase abstract class.
		var employee = Model.Build<Zongsoft.Tests.EmployeeBase>();
		Assert.NotNull(employee);
		Assert.Equal(typeof(EmployeeBase), Model.GetModelType(employee));
		this.TestEmployeeClassCore(employee);
	}

	[Fact]
	public void TestCustomerClass()
	{
		// Dynamically build an instance of the CustomerBase abstract class.
		var customer = Model.Build<Zongsoft.Tests.CustomerBase>();
		Assert.NotNull(customer);
		Assert.Equal(typeof(CustomerBase), Model.GetModelType(customer));
		this.TestCustomerClassCore(customer);
	}

	[Fact]
	public void TestSpecialEmployeeClass()
	{
		// Dynamically build an instance of the SpecialEmployeeBase abstract class.
		var special = Model.Build<Zongsoft.Tests.SpecialEmployeeBase>();
		Assert.NotNull(special);
		Assert.Equal(typeof(SpecialEmployeeBase), Model.GetModelType(special));
		this.TestSpecialEmployeeClassCore(special);
	}
	#endregion

	#region 私有方法
	private void TestPersonInterfaceCore(IPerson person)
	{
		if(person == null)
			throw new ArgumentNullException(nameof(person));

		object value;
		IDictionary<string, object> changes;

		Assert.NotNull(person);
		Assert.Null(person.GetChanges());
		Assert.False(person.HasChanges());

		// Test the TryGetValue(...) method for properties of the IPerson interface on uninitialized(unchanged).
		Assert.False(person.TryGetValue(nameof(person.Name), out value));
		Assert.False(person.TryGetValue(nameof(person.Gender), out value));
		Assert.False(person.TryGetValue(nameof(person.Birthdate), out value));
		Assert.False(person.TryGetValue(nameof(person.BloodType), out value));
		Assert.False(person.TryGetValue(nameof(person.HomeAddress), out value));

		Assert.False(person.TryGetValue(NOTEXISTS, out value));
		Assert.False(person.TrySetValue(NOTEXISTS, null));

		// Change the 'Name' property of the IPerson interface.
		person.Name = "Popeye";
		Assert.True(person.HasChanges());
		Assert.True(person.HasChanges(nameof(person.Name)));
		Assert.True(person.HasChanges(nameof(person.Name), nameof(person.Gender), nameof(person.Birthdate), nameof(person.BloodType), nameof(person.HomeAddress)));
		Assert.False(person.HasChanges(nameof(person.Gender)));
		Assert.False(person.HasChanges(nameof(person.Birthdate)));
		Assert.False(person.HasChanges(nameof(person.BloodType)));
		Assert.False(person.HasChanges(nameof(person.HomeAddress)));

		changes = person.GetChanges();
		Assert.Single(changes);
		Assert.True(changes.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);

		// Change the 'Gender' property of the IPerson interface.
		person.Gender = Zongsoft.Tests.Gender.Male;
		Assert.True(person.HasChanges());
		Assert.True(person.HasChanges(nameof(person.Gender)));
		Assert.True(person.HasChanges(nameof(person.Name), nameof(person.Gender), nameof(person.Birthdate), nameof(person.BloodType), nameof(person.HomeAddress)));
		Assert.True(person.HasChanges(nameof(person.Gender)));
		Assert.False(person.HasChanges(nameof(person.Birthdate)));
		Assert.False(person.HasChanges(nameof(person.BloodType)));
		Assert.False(person.HasChanges(nameof(person.HomeAddress)));

		changes = person.GetChanges();
		Assert.Equal(2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(person.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);

		// Change the 'Birthdate' property of the IPerson interface.
		person.Birthdate = new DateTime(BIRTHDATE.Ticks);
		Assert.True(person.HasChanges());
		Assert.True(person.HasChanges(nameof(person.Birthdate)));
		Assert.True(person.HasChanges(nameof(person.Name), nameof(person.Gender), nameof(person.Birthdate), nameof(person.BloodType), nameof(person.HomeAddress)));
		Assert.True(person.HasChanges(nameof(person.Gender)));
		Assert.True(person.HasChanges(nameof(person.Birthdate)));
		Assert.False(person.HasChanges(nameof(person.BloodType)));
		Assert.False(person.HasChanges(nameof(person.HomeAddress)));

		changes = person.GetChanges();
		Assert.Equal(3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(person.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(person.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);

		// Change the 'BloodType' property of the IPerson interface via TrySetValue(...) method.
		Assert.True(person.TrySetValue(nameof(person.BloodType), "X"));
		Assert.True(person.HasChanges());
		Assert.True(person.HasChanges(nameof(person.BloodType)));
		Assert.True(person.HasChanges(nameof(person.Name), nameof(person.Gender), nameof(person.Birthdate), nameof(person.BloodType), nameof(person.HomeAddress)));
		Assert.True(person.HasChanges(nameof(person.Gender)));
		Assert.True(person.HasChanges(nameof(person.Birthdate)));
		Assert.True(person.HasChanges(nameof(person.BloodType)));
		Assert.False(person.HasChanges(nameof(person.HomeAddress)));

		changes = person.GetChanges();
		Assert.Equal(4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(person.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(person.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(changes.TryGetValue(nameof(person.BloodType), out value));
		Assert.Equal("X", value);

		// Change the 'HomeAddress' property of the IPerson interface via TrySetValue(...) method.
		Assert.True(person.TrySetValue(nameof(person.HomeAddress), new Zongsoft.Tests.Address()
		{
			CountryId = 86,
			PostalCode = "430223",
			City = "Wuhan",
			Detail = "No.1 The university park road",
		}));
		Assert.True(person.HasChanges());
		Assert.True(person.HasChanges(nameof(person.HomeAddress)));
		Assert.True(person.HasChanges(nameof(person.Name), nameof(person.Gender), nameof(person.Birthdate), nameof(person.BloodType), nameof(person.HomeAddress)));
		Assert.True(person.HasChanges(nameof(person.Gender)));
		Assert.True(person.HasChanges(nameof(person.Birthdate)));
		Assert.True(person.HasChanges(nameof(person.BloodType)));
		Assert.True(person.HasChanges(nameof(person.HomeAddress)));

		changes = person.GetChanges();
		Assert.Equal(5, changes.Count);
		Assert.True(changes.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(person.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(person.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(changes.TryGetValue(nameof(person.BloodType), out value));
		Assert.Equal("X", value);
		Assert.True(changes.TryGetValue(nameof(person.HomeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("430223", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Wuhan", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.1", ((Zongsoft.Tests.Address)value).Detail);

		// Retest the TryGetValue(...) method for properties of the IPerson interface on initialized(changed).
		Assert.True(person.TryGetValue(nameof(person.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(person.TryGetValue(nameof(person.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(person.TryGetValue(nameof(person.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(person.TryGetValue(nameof(person.BloodType), out value));
		Assert.Equal("X", value);
		Assert.True(person.TryGetValue(nameof(person.HomeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("430223", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Wuhan", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.1", ((Zongsoft.Tests.Address)value).Detail);

		Assert.False(person.TryGetValue(NOTEXISTS, out value));
		Assert.False(person.TrySetValue(NOTEXISTS, null));
	}

	private void TestEmployeeInterfaceCore(IEmployee employee)
	{
		object value;
		IDictionary<string, object> changes;

		// Test the IPerson interface.
		this.TestPersonInterfaceCore(employee);

		// Test the TryGetValue(...) method for properties of the IEmployee interface on uninitialized(unchanged).
		Assert.False(employee.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Department), out value));
		Assert.False(employee.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Salary), out value));

		Assert.False(employee.TryGetValue(NOTEXISTS, out value));
		Assert.False(employee.TrySetValue(NOTEXISTS, null));

		// Change the 'EmployeeId' property of the IEmployee interface.
		employee.EmployeeId = 100;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.False(employee.HasChanges(nameof(employee.Department)));
		Assert.False(employee.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(employee.HasChanges(nameof(employee.Salary)));

		changes = employee.GetChanges();
		Assert.Equal(5 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);

		// Change the 'Department' property of the IEmployee interface.
		employee.Department = new Zongsoft.Tests.Department("Development");
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(employee.HasChanges(nameof(employee.Department)));
		Assert.False(employee.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(employee.HasChanges(nameof(employee.Salary)));

		changes = employee.GetChanges();
		Assert.Equal(5 + 2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);

		// Change the 'OfficeAddress' property of the IEmployee interface.
		Assert.True(employee.TrySetValue(nameof(employee.OfficeAddress), new Zongsoft.Tests.Address()
		{
			CountryId = 86,
			PostalCode = "518049",
			City = "Shenzhen",
			Detail = "No.19 Meilin road, Futian district",
		}));
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(employee.HasChanges(nameof(employee.Department)));
		Assert.True(employee.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(employee.HasChanges(nameof(employee.Salary)));

		changes = employee.GetChanges();
		Assert.Equal(5 + 3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(changes.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);

		// Change the 'Salary' property of the IEmployee interface.
		Assert.True(employee.TrySetValue(nameof(employee.Salary), 35000m));
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(employee.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(employee.HasChanges(nameof(employee.Department)));
		Assert.True(employee.HasChanges(nameof(employee.OfficeAddress)));
		Assert.True(employee.HasChanges(nameof(employee.Salary)));

		changes = employee.GetChanges();
		Assert.Equal(5 + 4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(changes.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);
		Assert.True(changes.TryGetValue(nameof(employee.Salary), out value));
		Assert.Equal(35000m, value);

		// Retest the TryGetValue(...) method for properties of the IEmployee interface on initialized(changed).
		Assert.True(employee.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(employee.TryGetValue(nameof(employee.Salary), out value));
		Assert.Equal(35000m, value);
		Assert.True(employee.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(employee.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);

		Assert.False(employee.TryGetValue(NOTEXISTS, out value));
		Assert.False(employee.TrySetValue(NOTEXISTS, null));
	}

	private void TestCustomerInterfaceCore(ICustomer customer)
	{
		object value;
		IDictionary<string, object> changes;

		// Test the IPerson interface.
		this.TestPersonInterfaceCore(customer);

		// Test the TryGetValue(...) method for properties of the ICustomer interface on uninitialized(unchanged).
		Assert.False(customer.TryGetValue(nameof(customer.Level), out value));

		Assert.False(customer.TryGetValue(NOTEXISTS, out value));
		Assert.False(customer.TrySetValue(NOTEXISTS, null));

		// Change the 'Level' property of the ICustomer interface.
		customer.Level = 10;
		Assert.True(customer.HasChanges());
		Assert.True(customer.HasChanges(nameof(customer.Level)));

		changes = customer.GetChanges();
		Assert.Equal(5 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(customer.Level), out value));
		Assert.Equal((byte)10, value);

		// Retest the TryGetValue(...) method for properties of the ICustomer interface on initialized(changed).
		Assert.True(customer.TryGetValue(nameof(customer.Level), out value));
		Assert.Equal((byte)10, value);

		Assert.False(customer.TryGetValue(NOTEXISTS, out value));
		Assert.False(customer.TrySetValue(NOTEXISTS, null));
	}

	private void TestSpecialEmployeeInterfaceCore(ISpecialEmployee employee)
	{
		object value;
		IDictionary<string, object> changes;

		// Test the IEmployee interface.
		this.TestEmployeeInterfaceCore(employee);

		// Test the TryGetValue(...) method for properties of the ISpecialEmployee interface on uninitialized(unchanged).
		Assert.False(employee.TryGetValue(nameof(employee.Property01), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property02), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property11), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property12), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property13), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property14), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property15), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property16), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property17), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property18), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property19), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property20), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property21), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property22), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property23), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property24), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property25), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property26), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property27), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property28), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property29), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property30), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property31), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property32), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property33), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property34), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property35), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property36), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property37), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property38), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property39), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property40), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property41), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property42), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property43), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property44), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property45), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property46), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property47), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property48), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property49), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property50), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property51), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property52), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property53), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property54), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(employee.TryGetValue(nameof(employee.Property56), out value));

		Assert.False(employee.TryGetValue(NOTEXISTS, out value));
		Assert.False(employee.TrySetValue(NOTEXISTS, null));

		// Change the 'Property01' property of the ISpecialEmployee interface.
		employee.Property01 = 1;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.False(employee.HasChanges(nameof(employee.Property02)));
		Assert.False(employee.HasChanges(nameof(employee.Property03)));
		Assert.False(employee.HasChanges(nameof(employee.Property04)));
		Assert.False(employee.HasChanges(nameof(employee.Property05)));
		Assert.False(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property02' property of the ISpecialEmployee interface.
		employee.Property02 = 2;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.False(employee.HasChanges(nameof(employee.Property03)));
		Assert.False(employee.HasChanges(nameof(employee.Property04)));
		Assert.False(employee.HasChanges(nameof(employee.Property05)));
		Assert.False(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property03' property of the ISpecialEmployee interface.
		employee.Property03 = 3;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.False(employee.HasChanges(nameof(employee.Property04)));
		Assert.False(employee.HasChanges(nameof(employee.Property05)));
		Assert.False(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property04' property of the ISpecialEmployee interface.
		employee.Property04 = 4;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.False(employee.HasChanges(nameof(employee.Property05)));
		Assert.False(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property05' property of the ISpecialEmployee interface.
		employee.Property05 = 5;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.False(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 5, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property06' property of the ISpecialEmployee interface.
		employee.Property06 = 6;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.False(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 6, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property07' property of the ISpecialEmployee interface.
		employee.Property07 = 7;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.False(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 7, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property08' property of the ISpecialEmployee interface.
		employee.Property08 = 8;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.True(employee.HasChanges(nameof(employee.Property08)));
		Assert.False(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 8, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property09' property of the ISpecialEmployee interface.
		employee.Property09 = 9;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.True(employee.HasChanges(nameof(employee.Property08)));
		Assert.True(employee.HasChanges(nameof(employee.Property09)));
		Assert.False(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 9, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property10' property of the ISpecialEmployee interface.
		employee.Property10 = 10;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.True(employee.HasChanges(nameof(employee.Property08)));
		Assert.True(employee.HasChanges(nameof(employee.Property09)));
		Assert.True(employee.HasChanges(nameof(employee.Property10)));
		Assert.False(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 10, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property55' property of the ISpecialEmployee interface.
		employee.Property55 = 55;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.True(employee.HasChanges(nameof(employee.Property08)));
		Assert.True(employee.HasChanges(nameof(employee.Property09)));
		Assert.True(employee.HasChanges(nameof(employee.Property10)));
		Assert.True(employee.HasChanges(nameof(employee.Property55)));
		Assert.False(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 11, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.Equal(55, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property56' property of the ISpecialEmployee interface.
		employee.Property56 = 56;
		Assert.True(employee.HasChanges());
		Assert.True(employee.HasChanges(nameof(employee.Property01)));
		Assert.True(employee.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(employee.HasChanges(nameof(employee.Property02)));
		Assert.True(employee.HasChanges(nameof(employee.Property03)));
		Assert.True(employee.HasChanges(nameof(employee.Property04)));
		Assert.True(employee.HasChanges(nameof(employee.Property05)));
		Assert.True(employee.HasChanges(nameof(employee.Property06)));
		Assert.True(employee.HasChanges(nameof(employee.Property07)));
		Assert.True(employee.HasChanges(nameof(employee.Property08)));
		Assert.True(employee.HasChanges(nameof(employee.Property09)));
		Assert.True(employee.HasChanges(nameof(employee.Property10)));
		Assert.True(employee.HasChanges(nameof(employee.Property55)));
		Assert.True(employee.HasChanges(nameof(employee.Property56)));

		changes = employee.GetChanges();
		Assert.Equal(9 + 12, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.Equal(55, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property56), out value));
		Assert.Equal(56, value);

		Assert.False(employee.TryGetValue(NOTEXISTS, out value));
		Assert.False(employee.TrySetValue(NOTEXISTS, null));
	}

	private void TestPersonClassCore(PersonBase person)
	{
		if(person == null)
			throw new ArgumentNullException(nameof(person));

		object value;
		IModel model = (IModel)person;
		IDictionary<string, object> changes;

		Assert.NotNull(person);
		Assert.Null(model.GetChanges());
		Assert.False(model.HasChanges());

		// Test the TryGetValue(...) method for properties of the IPerson interface on uninitialized(unchanged).
		Assert.False(model.TryGetValue(nameof(person.Name), out value));
		Assert.False(model.TryGetValue(nameof(person.Gender), out value));
		Assert.False(model.TryGetValue(nameof(person.Birthdate), out value));
		Assert.False(model.TryGetValue(nameof(person.BloodType), out value));
		Assert.False(model.TryGetValue(nameof(person.HomeAddress), out value));

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));

		// Change the 'Name' property of the IPerson interface.
		person.Name = "Popeye";
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(PersonBase.Name)));
		Assert.True(model.HasChanges(nameof(PersonBase.Name), nameof(PersonBase.Gender), nameof(PersonBase.Birthdate), nameof(PersonBase.BloodType), nameof(PersonBase.HomeAddress)));
		Assert.False(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.False(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.False(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.False(model.HasChanges(nameof(PersonBase.HomeAddress)));

		changes = model.GetChanges();
		Assert.Single(changes);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);

		// Change the 'Gender' property of the IPerson interface.
		person.Gender = Zongsoft.Tests.Gender.Male;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.True(model.HasChanges(nameof(PersonBase.Name), nameof(PersonBase.Gender), nameof(PersonBase.Birthdate), nameof(PersonBase.BloodType), nameof(PersonBase.HomeAddress)));
		Assert.True(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.False(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.False(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.False(model.HasChanges(nameof(PersonBase.HomeAddress)));

		changes = model.GetChanges();
		Assert.Equal(2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);

		// Change the 'Birthdate' property of the IPerson interface.
		person.Birthdate = new DateTime(BIRTHDATE.Ticks);
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.True(model.HasChanges(nameof(PersonBase.Name), nameof(PersonBase.Gender), nameof(PersonBase.Birthdate), nameof(PersonBase.BloodType), nameof(PersonBase.HomeAddress)));
		Assert.True(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.True(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.False(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.False(model.HasChanges(nameof(PersonBase.HomeAddress)));

		changes = model.GetChanges();
		Assert.Equal(3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);

		// Change the 'BloodType' property of the IPerson interface via TrySetValue(...) method.
		Assert.True(model.TrySetValue(nameof(PersonBase.BloodType), "X"));
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.True(model.HasChanges(nameof(PersonBase.Name), nameof(PersonBase.Gender), nameof(PersonBase.Birthdate), nameof(PersonBase.BloodType), nameof(PersonBase.HomeAddress)));
		Assert.True(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.True(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.True(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.False(model.HasChanges(nameof(PersonBase.HomeAddress)));

		changes = model.GetChanges();
		Assert.Equal(4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.BloodType), out value));
		Assert.Equal("X", value);

		// Change the 'HomeAddress' property of the IPerson interface via TrySetValue(...) method.
		Assert.True(model.TrySetValue(nameof(PersonBase.HomeAddress), new Zongsoft.Tests.Address()
		{
			CountryId = 86,
			PostalCode = "430223",
			City = "Wuhan",
			Detail = "No.1 The university park road",
		}));
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(PersonBase.HomeAddress)));
		Assert.True(model.HasChanges(nameof(PersonBase.Name), nameof(PersonBase.Gender), nameof(PersonBase.Birthdate), nameof(PersonBase.BloodType), nameof(PersonBase.HomeAddress)));
		Assert.True(model.HasChanges(nameof(PersonBase.Gender)));
		Assert.True(model.HasChanges(nameof(PersonBase.Birthdate)));
		Assert.True(model.HasChanges(nameof(PersonBase.BloodType)));
		Assert.True(model.HasChanges(nameof(PersonBase.HomeAddress)));

		changes = model.GetChanges();
		Assert.Equal(5, changes.Count);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.BloodType), out value));
		Assert.Equal("X", value);
		Assert.True(changes.TryGetValue(nameof(PersonBase.HomeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("430223", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Wuhan", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.1", ((Zongsoft.Tests.Address)value).Detail);

		// Retest the TryGetValue(...) method for properties of the IPerson interface on initialized(changed).
		Assert.True(model.TryGetValue(nameof(PersonBase.Name), out value));
		Assert.Equal("Popeye", value);
		Assert.True(model.TryGetValue(nameof(PersonBase.Gender), out value));
		Assert.Equal(Zongsoft.Tests.Gender.Male, value);
		Assert.True(model.TryGetValue(nameof(PersonBase.Birthdate), out value));
		Assert.Equal(BIRTHDATE, value);
		Assert.True(model.TryGetValue(nameof(PersonBase.BloodType), out value));
		Assert.Equal("X", value);
		Assert.True(model.TryGetValue(nameof(PersonBase.HomeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("430223", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Wuhan", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.1", ((Zongsoft.Tests.Address)value).Detail);

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));
	}

	private void TestEmployeeClassCore(EmployeeBase employee)
	{
		object value;
		var model = (IModel)employee;
		IDictionary<string, object> changes;

		// Test the PersonBase abstract class.
		this.TestPersonClassCore(employee);

		// Test the TryGetValue(...) method for properties of the IEmployee interface on uninitialized(unchanged).
		Assert.False(model.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.False(model.TryGetValue(nameof(employee.Department), out value));
		Assert.False(model.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.False(model.TryGetValue(nameof(employee.Salary), out value));

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));

		// Change the 'EmployeeId' property of the IEmployee interface.
		employee.EmployeeId = 100;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(model.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.False(model.HasChanges(nameof(employee.Department)));
		Assert.False(model.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(model.HasChanges(nameof(employee.Salary)));

		changes = model.GetChanges();
		Assert.Equal(5 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);

		// Change the 'Department' property of the IEmployee interface.
		employee.Department = new Zongsoft.Tests.Department("Development");
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(model.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(model.HasChanges(nameof(employee.Department)));
		Assert.False(model.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(model.HasChanges(nameof(employee.Salary)));

		changes = model.GetChanges();
		Assert.Equal(5 + 2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);

		// Change the 'OfficeAddress' property of the IEmployee interface.
		Assert.True(model.TrySetValue(nameof(employee.OfficeAddress), new Zongsoft.Tests.Address()
		{
			CountryId = 86,
			PostalCode = "518049",
			City = "Shenzhen",
			Detail = "No.19 Meilin road, Futian district",
		}));
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(model.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(model.HasChanges(nameof(employee.Department)));
		Assert.True(model.HasChanges(nameof(employee.OfficeAddress)));
		Assert.False(model.HasChanges(nameof(employee.Salary)));

		changes = model.GetChanges();
		Assert.Equal(5 + 3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(changes.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);

		// Change the 'Salary' property of the IEmployee interface.
		Assert.True(model.TrySetValue(nameof(employee.Salary), 35000m));
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.EmployeeId)));
		Assert.True(model.HasChanges(nameof(employee.EmployeeId), nameof(employee.Department), nameof(employee.OfficeAddress), nameof(employee.Salary)));
		Assert.True(model.HasChanges(nameof(employee.Department)));
		Assert.True(model.HasChanges(nameof(employee.OfficeAddress)));
		Assert.True(model.HasChanges(nameof(employee.Salary)));

		changes = model.GetChanges();
		Assert.Equal(5 + 4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(changes.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(changes.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);
		Assert.True(changes.TryGetValue(nameof(employee.Salary), out value));
		Assert.Equal(35000m, value);

		// Retest the TryGetValue(...) method for properties of the IEmployee interface on initialized(changed).
		Assert.True(model.TryGetValue(nameof(employee.EmployeeId), out value));
		Assert.Equal(100, value);
		Assert.True(model.TryGetValue(nameof(employee.Salary), out value));
		Assert.Equal(35000m, value);
		Assert.True(model.TryGetValue(nameof(employee.Department), out value));
		Assert.Equal("Development", ((Zongsoft.Tests.Department)value).Name);
		Assert.True(model.TryGetValue(nameof(employee.OfficeAddress), out value));
		Assert.Equal(86, ((Zongsoft.Tests.Address)value).CountryId);
		Assert.Equal("518049", ((Zongsoft.Tests.Address)value).PostalCode);
		Assert.Equal("Shenzhen", ((Zongsoft.Tests.Address)value).City);
		Assert.Contains("No.19", ((Zongsoft.Tests.Address)value).Detail);

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));
	}

	private void TestCustomerClassCore(CustomerBase customer)
	{
		object value;
		var model = (IModel)customer;
		IDictionary<string, object> changes;

		// Test the PersonBase abstract class.
		this.TestPersonClassCore(customer);

		// Test the TryGetValue(...) method for properties of the ICustomer interface on uninitialized(unchanged).
		Assert.False(model.TryGetValue(nameof(customer.Level), out value));

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));

		// Change the 'Level' property of the ICustomer interface.
		customer.Level = 10;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(customer.Level)));

		changes = model.GetChanges();
		Assert.Equal(5 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(customer.Level), out value));
		Assert.Equal((byte)10, value);

		// Retest the TryGetValue(...) method for properties of the ICustomer interface on initialized(changed).
		Assert.True(model.TryGetValue(nameof(customer.Level), out value));
		Assert.Equal((byte)10, value);

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));
	}

	private void TestSpecialEmployeeClassCore(SpecialEmployeeBase employee)
	{
		object value;
		var model = (IModel)employee;
		IDictionary<string, object> changes;

		// Test the EmployeeBase abstract class.
		this.TestEmployeeClassCore(employee);

		// Test the TryGetValue(...) method for properties of the ISpecialEmployee interface on uninitialized(unchanged).
		Assert.False(model.TryGetValue(nameof(employee.Property01), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property02), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property11), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property12), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property13), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property14), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property15), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property16), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property17), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property18), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property19), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property20), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property21), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property22), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property23), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property24), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property25), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property26), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property27), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property28), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property29), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property30), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property31), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property32), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property33), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property34), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property35), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property36), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property37), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property38), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property39), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property40), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property41), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property42), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property43), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property44), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property45), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property46), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property47), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property48), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property49), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property50), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property51), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property52), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property53), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property54), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(model.TryGetValue(nameof(employee.Property56), out value));

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));

		// Change the 'Property01' property of the ISpecialEmployee interface.
		employee.Property01 = 1;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.False(model.HasChanges(nameof(employee.Property02)));
		Assert.False(model.HasChanges(nameof(employee.Property03)));
		Assert.False(model.HasChanges(nameof(employee.Property04)));
		Assert.False(model.HasChanges(nameof(employee.Property05)));
		Assert.False(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 1, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property02' property of the ISpecialEmployee interface.
		employee.Property02 = 2;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.False(model.HasChanges(nameof(employee.Property03)));
		Assert.False(model.HasChanges(nameof(employee.Property04)));
		Assert.False(model.HasChanges(nameof(employee.Property05)));
		Assert.False(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 2, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property03' property of the ISpecialEmployee interface.
		employee.Property03 = 3;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.False(model.HasChanges(nameof(employee.Property04)));
		Assert.False(model.HasChanges(nameof(employee.Property05)));
		Assert.False(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 3, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property04' property of the ISpecialEmployee interface.
		employee.Property04 = 4;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.False(model.HasChanges(nameof(employee.Property05)));
		Assert.False(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 4, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property05' property of the ISpecialEmployee interface.
		employee.Property05 = 5;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.False(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 5, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property06' property of the ISpecialEmployee interface.
		employee.Property06 = 6;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.False(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 6, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property07' property of the ISpecialEmployee interface.
		employee.Property07 = 7;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.False(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 7, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property08' property of the ISpecialEmployee interface.
		employee.Property08 = 8;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.True(model.HasChanges(nameof(employee.Property08)));
		Assert.False(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 8, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property09' property of the ISpecialEmployee interface.
		employee.Property09 = 9;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.True(model.HasChanges(nameof(employee.Property08)));
		Assert.True(model.HasChanges(nameof(employee.Property09)));
		Assert.False(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 9, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property10' property of the ISpecialEmployee interface.
		employee.Property10 = 10;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.True(model.HasChanges(nameof(employee.Property08)));
		Assert.True(model.HasChanges(nameof(employee.Property09)));
		Assert.True(model.HasChanges(nameof(employee.Property10)));
		Assert.False(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 10, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property55' property of the ISpecialEmployee interface.
		employee.Property55 = 55;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.True(model.HasChanges(nameof(employee.Property08)));
		Assert.True(model.HasChanges(nameof(employee.Property09)));
		Assert.True(model.HasChanges(nameof(employee.Property10)));
		Assert.True(model.HasChanges(nameof(employee.Property55)));
		Assert.False(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 11, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.Equal(55, value);
		Assert.False(changes.TryGetValue(nameof(employee.Property56), out value));

		// Change the 'Property56' property of the ISpecialEmployee interface.
		employee.Property56 = 56;
		Assert.True(model.HasChanges());
		Assert.True(model.HasChanges(nameof(employee.Property01)));
		Assert.True(model.HasChanges(nameof(employee.Property01), nameof(employee.Property02), nameof(employee.Property03), nameof(employee.Property04), nameof(employee.Property05), nameof(employee.Property06), nameof(employee.Property07), nameof(employee.Property08), nameof(employee.Property09), nameof(employee.Property10), nameof(employee.Property55), nameof(employee.Property56)));
		Assert.True(model.HasChanges(nameof(employee.Property02)));
		Assert.True(model.HasChanges(nameof(employee.Property03)));
		Assert.True(model.HasChanges(nameof(employee.Property04)));
		Assert.True(model.HasChanges(nameof(employee.Property05)));
		Assert.True(model.HasChanges(nameof(employee.Property06)));
		Assert.True(model.HasChanges(nameof(employee.Property07)));
		Assert.True(model.HasChanges(nameof(employee.Property08)));
		Assert.True(model.HasChanges(nameof(employee.Property09)));
		Assert.True(model.HasChanges(nameof(employee.Property10)));
		Assert.True(model.HasChanges(nameof(employee.Property55)));
		Assert.True(model.HasChanges(nameof(employee.Property56)));

		changes = model.GetChanges();
		Assert.Equal(9 + 12, changes.Count);
		Assert.True(changes.TryGetValue(nameof(employee.Property01), out value));
		Assert.Equal(1, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property02), out value));
		Assert.Equal(2, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property03), out value));
		Assert.Equal(3, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property04), out value));
		Assert.Equal(4, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property05), out value));
		Assert.Equal(5, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property06), out value));
		Assert.Equal(6, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property07), out value));
		Assert.Equal(7, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property08), out value));
		Assert.Equal(8, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property09), out value));
		Assert.Equal(9, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property10), out value));
		Assert.Equal(10, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property55), out value));
		Assert.Equal(55, value);
		Assert.True(changes.TryGetValue(nameof(employee.Property56), out value));
		Assert.Equal(56, value);

		Assert.False(model.TryGetValue(NOTEXISTS, out value));
		Assert.False(model.TrySetValue(NOTEXISTS, null));
	}
	#endregion
}
