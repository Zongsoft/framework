﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Data;

namespace Zongsoft.Tests;

public class PersonComparer : IEqualityComparer<IPerson>
{
	public bool Equals(IPerson x, IPerson y)
	{
		if(x is null)
			return y is null;
		if(y is null)
			return false;

		return x.Name == y.Name &&
			x.Gender == y.Gender &&
			x.Birthdate == y.Birthdate &&
			x.BloodType == y.BloodType &&
			x.HomeAddress == y.HomeAddress;
	}

	public int GetHashCode(IPerson person) => person is null ? 0 : HashCode.Combine(person.Name?.ToUpperInvariant(), person.Gender, person.Birthdate, person.BloodType);
}

public interface IPerson : Zongsoft.Data.IModel
{
	/// <summary>获取或设置人员姓名。</summary>
	string Name { get; set; }

	/// <summary>获取或设置人员性别。</summary>
	Gender? Gender { get; set; }

	/// <summary>获取或设置人员出生日期。</summary>
	DateTime Birthdate { get; set; }

	/// <summary>获取或设置人员血型。</summary>
	string BloodType { get; set; }

	/// <summary>获取或设置人员的家庭住址。</summary>
	Address HomeAddress { get; set; }
}

public interface IEmployee : IPerson
{
	/// <summary>获取或设置员工编号。</summary>
	int EmployeeId { get; set; }

	/// <summary>获取或设置员工所属的部门。</summary>
	Department Department { get; set; }

	/// <summary>获取或设置员工的办公地址。</summary>
	Address OfficeAddress { get; set; }

	/// <summary>获取或设置员工的月薪。</summary>
	decimal Salary { get; set; }
}

public interface ICustomer : IPerson
{
	/// <summary>获取或设置客户的评级。</summary>
	byte Level { get; set; }
}

public interface ISpecialEmployee : IEmployee
{
	int Property01 { get; set; }
	int Property02 { get; set; }
	int Property03 { get; set; }
	int Property04 { get; set; }
	int Property05 { get; set; }
	int Property06 { get; set; }
	int Property07 { get; set; }
	int Property08 { get; set; }
	int Property09 { get; set; }
	int Property10 { get; set; }
	int Property11 { get; set; }
	int Property12 { get; set; }
	int Property13 { get; set; }
	int Property14 { get; set; }
	int Property15 { get; set; }
	int Property16 { get; set; }
	int Property17 { get; set; }
	int Property18 { get; set; }
	int Property19 { get; set; }
	int Property20 { get; set; }
	int Property21 { get; set; }
	int Property22 { get; set; }
	int Property23 { get; set; }
	int Property24 { get; set; }
	int Property25 { get; set; }
	int Property26 { get; set; }
	int Property27 { get; set; }
	int Property28 { get; set; }
	int Property29 { get; set; }
	int Property30 { get; set; }
	int Property31 { get; set; }
	int Property32 { get; set; }
	int Property33 { get; set; }
	int Property34 { get; set; }
	int Property35 { get; set; }
	int Property36 { get; set; }
	int Property37 { get; set; }
	int Property38 { get; set; }
	int Property39 { get; set; }
	int Property40 { get; set; }
	int Property41 { get; set; }
	int Property42 { get; set; }
	int Property43 { get; set; }
	int Property44 { get; set; }
	int Property45 { get; set; }
	int Property46 { get; set; }
	int Property47 { get; set; }
	int Property48 { get; set; }
	int Property49 { get; set; }
	int Property50 { get; set; }
	int Property51 { get; set; }
	int Property52 { get; set; }
	int Property53 { get; set; }
	int Property54 { get; set; }
	int Property55 { get; set; }
	int Property56 { get; set; }
}

public abstract class PersonBase : Zongsoft.Data.IModel
{
	/// <summary>获取或设置人员姓名。</summary>
	public abstract string Name { get; set; }

	/// <summary>获取或设置人员性别。</summary>
	public abstract Gender? Gender { get; set; }

	/// <summary>获取或设置人员出生日期。</summary>
	public abstract DateTime Birthdate { get; set; }

	/// <summary>获取或设置人员血型。</summary>
	public abstract string BloodType { get; set; }

	/// <summary>获取或设置人员的家庭住址。</summary>
	public abstract Address HomeAddress { get; set; }

	#region 抽象方法
	protected abstract int GetCount();
	protected abstract IDictionary<string, object> GetChanges();
	protected abstract bool HasChanges(params string[] names);
	protected abstract bool Reset(string name, out object value);
	protected abstract void Reset(params string[] names);
	protected abstract bool TryGetValue(string name, out object value);
	protected abstract bool TrySetValue(string name, object value);
	#endregion

	#region 显式实现
	int IModel.GetCount() => this.GetCount();
	IDictionary<string, object> IModel.GetChanges() => this.GetChanges();
	bool IModel.HasChanges(params string[] names) => this.HasChanges(names);
	bool IModel.Reset(string name, out object value) => this.Reset(name, out value);
	void IModel.Reset(params string[] names) => this.Reset(names);
	bool IModel.TryGetValue(string name, out object value) => this.TryGetValue(name, out value);
	bool IModel.TrySetValue(string name, object value) => this.TrySetValue(name, value);
	#endregion
}

public abstract class EmployeeBase : PersonBase
{
	/// <summary>获取或设置员工编号。</summary>
	public abstract int EmployeeId { get; set; }

	/// <summary>获取或设置员工所属的部门。</summary>
	public abstract Department Department { get; set; }

	/// <summary>获取或设置员工的办公地址。</summary>
	public abstract Address OfficeAddress { get; set; }

	/// <summary>获取或设置员工的月薪。</summary>
	public abstract decimal Salary { get; set; }
}

public abstract class CustomerBase : PersonBase
{
	/// <summary>获取或设置客户的评级。</summary>
	public abstract byte Level { get; set; }
}

public abstract class SpecialEmployeeBase : EmployeeBase
{
	public abstract int Property01 { get; set; }
	public abstract int Property02 { get; set; }
	public abstract int Property03 { get; set; }
	public abstract int Property04 { get; set; }
	public abstract int Property05 { get; set; }
	public abstract int Property06 { get; set; }
	public abstract int Property07 { get; set; }
	public abstract int Property08 { get; set; }
	public abstract int Property09 { get; set; }
	public abstract int Property10 { get; set; }
	public abstract int Property11 { get; set; }
	public abstract int Property12 { get; set; }
	public abstract int Property13 { get; set; }
	public abstract int Property14 { get; set; }
	public abstract int Property15 { get; set; }
	public abstract int Property16 { get; set; }
	public abstract int Property17 { get; set; }
	public abstract int Property18 { get; set; }
	public abstract int Property19 { get; set; }
	public abstract int Property20 { get; set; }
	public abstract int Property21 { get; set; }
	public abstract int Property22 { get; set; }
	public abstract int Property23 { get; set; }
	public abstract int Property24 { get; set; }
	public abstract int Property25 { get; set; }
	public abstract int Property26 { get; set; }
	public abstract int Property27 { get; set; }
	public abstract int Property28 { get; set; }
	public abstract int Property29 { get; set; }
	public abstract int Property30 { get; set; }
	public abstract int Property31 { get; set; }
	public abstract int Property32 { get; set; }
	public abstract int Property33 { get; set; }
	public abstract int Property34 { get; set; }
	public abstract int Property35 { get; set; }
	public abstract int Property36 { get; set; }
	public abstract int Property37 { get; set; }
	public abstract int Property38 { get; set; }
	public abstract int Property39 { get; set; }
	public abstract int Property40 { get; set; }
	public abstract int Property41 { get; set; }
	public abstract int Property42 { get; set; }
	public abstract int Property43 { get; set; }
	public abstract int Property44 { get; set; }
	public abstract int Property45 { get; set; }
	public abstract int Property46 { get; set; }
	public abstract int Property47 { get; set; }
	public abstract int Property48 { get; set; }
	public abstract int Property49 { get; set; }
	public abstract int Property50 { get; set; }
	public abstract int Property51 { get; set; }
	public abstract int Property52 { get; set; }
	public abstract int Property53 { get; set; }
	public abstract int Property54 { get; set; }
	public abstract int Property55 { get; set; }
	public abstract int Property56 { get; set; }
}

[DefaultValue(Female)]
public enum Gender
{
	[Zongsoft.Components.Alias("F")]
	Female,

	[Zongsoft.Components.Alias("M")]
	[Description("Gender.Male")]
	Male,
}

public class Address : IEquatable<Address>
{
	public string City { get; set; }
	public string Detail { get; set; }
	public string PostalCode { get; set; }
	public int CountryId { get; set; }

	public bool Equals(Address other) => other is not null &&
		this.CountryId == other.CountryId &&
		this.City == other.City &&
		this.PostalCode == other.PostalCode;

	public override bool Equals(object obj) => obj is Address address && this.Equals(address);
	public override int GetHashCode() => HashCode.Combine(this.CountryId, this.City?.ToUpperInvariant(), this.PostalCode?.ToUpperInvariant());
	public override string ToString() => $"[{this.CountryId}] {this.City}({this.PostalCode})";
}

public class Department
{
	#region 成员字段
	private string _name;
	private EmployeeCollection _employees;
	#endregion

	#region 构造函数
	public Department()
	{
		_employees = new EmployeeCollection(this);
	}

	public Department(string name, IEnumerable<Employee> employees = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		_name = name;
		_employees = new EmployeeCollection(this);

		if(employees != null)
			_employees.AddRange(employees);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置企业编号。</summary>
	public int CorporationId { get; set; }

	/// <summary>获取或设置部门编号。</summary>
	public short DepartmentId { get; set; }

	/// <summary>获取或设置部门的名称。</summary>
	public string Name
	{
		get => _name;
		set => _name = string.IsNullOrEmpty(value) ? throw new ArgumentNullException() : value;
	}

	/// <summary>获取当前部门中指定名称的员工。</summary>
	/// <param name="name">指定要获取的本部门的员工的姓名。</param>
	/// <returns>返回的员工对象。</returns>
	public Employee this[string name]
	{
		get => _employees[name];
	}

	/// <summary>获取当前部门的员工集合。</summary>
	public EmployeeCollection Employees
	{
		get => _employees;
	}
	#endregion
}

public class Person : IPerson, INotifyPropertyChanged, ICloneable
{
	#region 静态字段
	protected static string[] __NAMES__ = new string[] { "Name", "Gender", "Birthdate", "BloodType", "HomeAddress" };
	protected static readonly Dictionary<string, PropertyToken<Person>> __TOKENS__ = new Dictionary<string, PropertyToken<Person>>()
	{
		{ "Name", new PropertyToken<Person>(0, target => target._name, (target, value) => target.Name = (string) value) },
		{ "Gender", new PropertyToken<Person>(1, target => target._gender, (target, value) => target.Gender = (Gender?) value) },
		{ "Birthdate", new PropertyToken<Person>(2, target => target._birthdate, (target, value) => target.Birthdate = (DateTime) value) },
		{ "BloodType", new PropertyToken<Person>(3, target => target._bloodType, (target, value) => target.BloodType = (string) value) },
		{ "HomeAddress", new PropertyToken<Person>(4, target => target._homeAddress, (target, value) => target.HomeAddress = (Address) value) },
	};
	#endregion

	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	#endregion

	#region 标记变量
	protected byte _MASK_;
	#endregion

	#region 成员字段
	private string _name;
	private Gender? _gender;
	private DateTime _birthdate;
	private string _bloodType;
	private Address _homeAddress;
	#endregion

	#region 构造函数
	public Person() { }
	public Person(string name) => this.Name = name;
	#endregion

	#region 公共属性
	public string Name
	{
		get => _name;
		set
		{
			if(_name == value)
				return;

			_name = value;
			_MASK_ |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
		}
	}

	public Gender? Gender
	{
		get => _gender;
		set
		{
			if(_gender == value)
				return;

			_gender = value;
			_MASK_ |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gender)));
		}
	}

	public DateTime Birthdate
	{
		get => _birthdate;
		set
		{
			if(_birthdate == value)
				return;

			_birthdate = value;
			_MASK_ |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Birthdate)));
		}
	}

	public string BloodType
	{
		get => _bloodType;
		set
		{
			if(_bloodType == value)
				return;

			_bloodType = value;
			_MASK_ |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BloodType)));
		}
	}

	public Address HomeAddress
	{
		get => _homeAddress;
		set
		{
			if(object.Equals(_homeAddress, value))
				return;

			_homeAddress = value;
			_MASK_ |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomeAddress)));
		}
	}
	#endregion

	#region 克隆方法
	object ICloneable.Clone() => this.Clone();
	public Person Clone()
	{
		var person = new Person();
		this.Clone(person);
		return person;
	}

	public void Clone(Person person)
	{
		if(person == null)
			return;

		person._MASK_ = _MASK_;
		person._name = _name;
		person._gender = _gender;
		person._birthdate = _birthdate;
		person._bloodType = _bloodType;
		person._homeAddress = _homeAddress;
	}
	#endregion

	#region 保护方法
	protected void RaisePropertyChanged(string name)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
	#endregion

	#region 虚拟方法
	protected virtual bool HasChanges(string[] names)
	{
		if(names == null || names.Length == 0)
			return _MASK_ != 0;

		for(var i = 0; i < names.Length; i++)
		{
			if(__TOKENS__.TryGetValue(names[i], out var property) && property.Setter != null && (_MASK_ >> property.Ordinal & 1) == 1)
				return true;
		}

		return false;
	}

	protected virtual IDictionary<string, object> GetChanges()
	{
		if(_MASK_ == 0)
			return null;

		var dictionary = new Dictionary<string, object>(__NAMES__.Length);

		for(int i = 0; i < __NAMES__.Length; i++)
		{
			if((_MASK_ >> i & 1) == 1)
				dictionary[__NAMES__[i]] = __TOKENS__[__NAMES__[i]].Getter(this);
		}

		return dictionary;
	}

	protected virtual bool TryGetValue(string name, out object value)
	{
		value = null;

		if(__TOKENS__.TryGetValue(name, out var property) && (property.Ordinal < 0 || (_MASK_ >> property.Ordinal & 1) == 1))
		{
			value = property.Getter(this);
			return true;
		}

		return false;
	}

	protected virtual bool TrySetValue(string name, object value)
	{
		if(__TOKENS__.TryGetValue(name, out var property) && property.Setter != null)
		{
			property.Setter(this, value);
			return true;
		}

		return false;
	}
	#endregion

	#region 接口实现
	public int GetCount() => 0;
	public void Reset(params string[] names) { }
	public bool Reset(string name, out object value)
	{
		value = null;
		return false;
	}
	bool Zongsoft.Data.IModel.HasChanges(params string[] names) => this.HasChanges(names);
	IDictionary<string, object> Zongsoft.Data.IModel.GetChanges() => this.GetChanges();
	bool Zongsoft.Data.IModel.TryGetValue(string name, out object value) => this.TryGetValue(name, out value);
	bool Zongsoft.Data.IModel.TrySetValue(string name, object value) => this.TrySetValue(name, value);
	#endregion
}

public class Employee : Person, IEmployee
{
	#region 静态构造
	static Employee()
	{
		var names = __NAMES__;
		__NAMES__ = new string[names.Length + 4];
		Array.Copy(names, 0, __NAMES__, 0, names.Length);

		__NAMES__[names.Length] = nameof(EmployeeId);
		__NAMES__[names.Length + 1] = nameof(Department);
		__NAMES__[names.Length + 2] = nameof(OfficeAddress);
		__NAMES__[names.Length + 3] = nameof(Salary);

		__TOKENS__.Add(nameof(EmployeeId), new PropertyToken<Person>(names.Length, target => ((Employee)target)._employeeId, (target, value) => ((Employee)target).EmployeeId = (int)value));
		__TOKENS__.Add(nameof(Department), new PropertyToken<Person>(names.Length + 1, target => ((Employee)target)._department, (target, value) => ((Employee)target).Department = (Department)value));
		__TOKENS__.Add(nameof(OfficeAddress), new PropertyToken<Person>(names.Length + 2, target => ((Employee)target)._officeAddress, (target, value) => ((Employee)target).OfficeAddress = (Address)value));
		__TOKENS__.Add(nameof(Salary), new PropertyToken<Person>(names.Length + 3, target => ((Employee)target)._salary, (target, value) => ((Employee)target).Salary = (decimal)value));
	}
	#endregion

	#region 标记变量
	protected ushort _MASK_INT16_;
	#endregion

	#region 成员字段
	private int _employeeId;
	private Department _department;
	private Address _officeAddress;
	private decimal _salary;
	#endregion

	#region 构造函数
	public Employee() { }
	public Employee(int id, string name) : base(name) => this.EmployeeId = id;
	#endregion

	#region 公共属性
	/// <summary>获取或设置员工编号。</summary>
	public int EmployeeId
	{
		get => _employeeId;
		set
		{
			if(_employeeId == value)
				return;

			_employeeId = value;
			_MASK_INT16_ |= 32;
			this.RaisePropertyChanged(nameof(EmployeeId));
		}
	}

	/// <summary>获取或设置员工所属的部门。</summary>
	public Department Department
	{
		get => _department;
		set
		{
			if(object.Equals(_department, value))
				return;

			_department = value;
			_MASK_INT16_ |= 64;
			this.RaisePropertyChanged(nameof(Department));
		}
	}

	/// <summary>获取或设置员工的办公地址。</summary>
	public Address OfficeAddress
	{
		get => _officeAddress;
		set
		{
			if(object.Equals(_officeAddress, value))
				return;

			_officeAddress = value;
			_MASK_INT16_ |= 128;
			this.RaisePropertyChanged(nameof(OfficeAddress));
		}
	}

	/// <summary>获取或设置员工的月薪。</summary>
	public decimal Salary
	{
		get => _salary;
		set
		{
			if(_salary == value)
				return;

			_salary = value;
			_MASK_INT16_ |= 256;
			this.RaisePropertyChanged(nameof(Salary));
		}
	}
	#endregion

	#region 重写方法
	protected override bool HasChanges(string[] names)
	{
		if(names == null || names.Length == 0)
			return _MASK_ != 0;

		for(var i = 0; i < names.Length; i++)
		{
			if(__TOKENS__.TryGetValue(names[i], out var property) && property.Setter != null)
			{
				if(property.Ordinal < 5)
				{
					if((_MASK_ >> property.Ordinal & 1) == 1)
						return true;
				}
				else
				{
					if((_MASK_INT16_ >> property.Ordinal & 1) == 1)
						return true;
				}
			}
		}

		return false;
	}

	protected override IDictionary<string, object> GetChanges()
	{
		if(_MASK_ == 0 && _MASK_INT16_ == 0)
			return null;

		var dictionary = new Dictionary<string, object>(__NAMES__.Length);

		for(int i = 0; i < __NAMES__.Length; i++)
		{
			if(i < 5)
			{
				if((_MASK_ >> i & 1) == 1)
					dictionary[__NAMES__[i]] = __TOKENS__[__NAMES__[i]].Getter(this);
			}
			else
			{
				if((_MASK_INT16_ >> i & 1) == 1)
					dictionary[__NAMES__[i]] = __TOKENS__[__NAMES__[i]].Getter(this);
			}
		}

		return dictionary;
	}

	protected override bool TryGetValue(string name, out object value)
	{
		value = null;

		if(__TOKENS__.TryGetValue(name, out var property))
		{
			if(property.Ordinal < 0 || (property.Ordinal < 5 && (_MASK_ >> property.Ordinal & 1) == 1))
			{
				value = property.Getter(this);
				return true;
			}
			else if(property.Ordinal >= 5 && (_MASK_INT16_ >> property.Ordinal & 1) == 1)
			{
				value = property.Getter(this);
				return true;
			}
		}

		return false;
	}
	#endregion
}

public class Customer : Person, ICustomer
{
	#region 静态构造
	static Customer()
	{
		var names = __NAMES__;
		__NAMES__ = new string[names.Length + 1];
		Array.Copy(names, 0, __NAMES__, 0, names.Length);

		__NAMES__[names.Length] = nameof(Level);
		__TOKENS__.Add(nameof(Level), new PropertyToken<Person>(names.Length, target => ((Customer)target)._level, (target, value) => ((Customer)target).Level = (byte)value));
	}
	#endregion

	#region 成员字段
	private byte _level;
	#endregion

	#region 构造函数
	public Customer() { }
	public Customer(string name) : base(name) { }
	#endregion

	#region 公共属性
	/// <summary>获取或设置客户的评级。</summary>
	public byte Level
	{
		get => _level;
		set
		{
			if(_level == value)
				return;

			_level = value;
			_MASK_ |= 32;
			this.RaisePropertyChanged(nameof(Level));
		}
	}
	#endregion
}

public class EmployeeCollection(Department department) : KeyedCollection<string, Employee>
{
	private readonly Department _department = department;

	public void AddRange(params Employee[] employees)
	{
		this.AddRange((IEnumerable<Employee>)employees);
	}

	public void AddRange(IEnumerable<Employee> employees)
	{
		if(employees == null)
			return;

		foreach(var employee in employees)
		{
			this.Add(employee);
		}
	}

	protected override string GetKeyForItem(Employee item) => item.Name;
	protected override void InsertItem(int index, Employee item)
	{
		if(item == null)
			return;

		item.Department = _department;
		base.InsertItem(index, item);
	}
}

public class SpecialEmployee : ISpecialEmployee, INotifyPropertyChanged
{
	#region 常量定义
	private const int PROPERTY_COUNT = 56 + 9;
	#endregion

	#region 静态字段
	private static readonly string[] __NAMES__ =
	[
		nameof(Property01),
		nameof(Property02),
		nameof(Property03),
		nameof(Property04),
		nameof(Property05),
		nameof(Property06),
		nameof(Property07),
		nameof(Property08),
		nameof(Property09),
		nameof(Property10),
		nameof(Property11),
		nameof(Property12),
		nameof(Property13),
		nameof(Property14),
		nameof(Property15),
		nameof(Property16),
		nameof(Property17),
		nameof(Property18),
		nameof(Property19),
		nameof(Property20),
		nameof(Property21),
		nameof(Property22),
		nameof(Property23),
		nameof(Property24),
		nameof(Property25),
		nameof(Property26),
		nameof(Property27),
		nameof(Property28),
		nameof(Property29),
		nameof(Property30),
		nameof(Property31),
		nameof(Property32),
		nameof(Property33),
		nameof(Property34),
		nameof(Property35),
		nameof(Property36),
		nameof(Property37),
		nameof(Property38),
		nameof(Property39),
		nameof(Property40),
		nameof(Property41),
		nameof(Property42),
		nameof(Property43),
		nameof(Property44),
		nameof(Property45),
		nameof(Property46),
		nameof(Property47),
		nameof(Property48),
		nameof(Property49),
		nameof(Property50),
		nameof(Property51),
		nameof(Property52),
		nameof(Property53),
		nameof(Property54),
		nameof(Property55),
		nameof(Property56),

		nameof(EmployeeId),
		nameof(Department),
		nameof(OfficeAddress),
		nameof(Salary),

		nameof(Name),
		nameof(Gender),
		nameof(Birthdate),
		nameof(BloodType),
		nameof(HomeAddress),
	];

	private static readonly Dictionary<string, PropertyToken<SpecialEmployee>> __TOKENS__ = new()
	{
		{ nameof(Property01), new PropertyToken<SpecialEmployee>(0, target => target._property01, (target, value) => target.Property01 = (int) value) },
		{ nameof(Property02), new PropertyToken<SpecialEmployee>(1, target => target._property02, (target, value) => target.Property02 = (int) value) },
		{ nameof(Property03), new PropertyToken<SpecialEmployee>(2, target => target._property03, (target, value) => target.Property03 = (int) value) },
		{ nameof(Property04), new PropertyToken<SpecialEmployee>(3, target => target._property04, (target, value) => target.Property04 = (int) value) },
		{ nameof(Property05), new PropertyToken<SpecialEmployee>(4, target => target._property05, (target, value) => target.Property05 = (int) value) },
		{ nameof(Property06), new PropertyToken<SpecialEmployee>(5, target => target._property06, (target, value) => target.Property06 = (int) value) },
		{ nameof(Property07), new PropertyToken<SpecialEmployee>(6, target => target._property07, (target, value) => target.Property07 = (int) value) },
		{ nameof(Property08), new PropertyToken<SpecialEmployee>(7, target => target._property08, (target, value) => target.Property08 = (int) value) },
		{ nameof(Property09), new PropertyToken<SpecialEmployee>(8, target => target._property09, (target, value) => target.Property09 = (int) value) },
		{ nameof(Property10), new PropertyToken<SpecialEmployee>(9, target => target._property10, (target, value) => target.Property10 = (int) value) },
		{ nameof(Property11), new PropertyToken<SpecialEmployee>(10, target => target._property11, (target, value) => target.Property11 = (int) value) },
		{ nameof(Property12), new PropertyToken<SpecialEmployee>(11, target => target._property12, (target, value) => target.Property12 = (int) value) },
		{ nameof(Property13), new PropertyToken<SpecialEmployee>(12, target => target._property13, (target, value) => target.Property13 = (int) value) },
		{ nameof(Property14), new PropertyToken<SpecialEmployee>(13, target => target._property14, (target, value) => target.Property14 = (int) value) },
		{ nameof(Property15), new PropertyToken<SpecialEmployee>(14, target => target._property15, (target, value) => target.Property15 = (int) value) },
		{ nameof(Property16), new PropertyToken<SpecialEmployee>(15, target => target._property16, (target, value) => target.Property16 = (int) value) },
		{ nameof(Property17), new PropertyToken<SpecialEmployee>(16, target => target._property17, (target, value) => target.Property17 = (int) value) },
		{ nameof(Property18), new PropertyToken<SpecialEmployee>(17, target => target._property18, (target, value) => target.Property18 = (int) value) },
		{ nameof(Property19), new PropertyToken<SpecialEmployee>(18, target => target._property19, (target, value) => target.Property19 = (int) value) },
		{ nameof(Property20), new PropertyToken<SpecialEmployee>(19, target => target._property20, (target, value) => target.Property20 = (int) value) },
		{ nameof(Property21), new PropertyToken<SpecialEmployee>(20, target => target._property21, (target, value) => target.Property21 = (int) value) },
		{ nameof(Property22), new PropertyToken<SpecialEmployee>(21, target => target._property22, (target, value) => target.Property22 = (int) value) },
		{ nameof(Property23), new PropertyToken<SpecialEmployee>(22, target => target._property23, (target, value) => target.Property23 = (int) value) },
		{ nameof(Property24), new PropertyToken<SpecialEmployee>(23, target => target._property24, (target, value) => target.Property24 = (int) value) },
		{ nameof(Property25), new PropertyToken<SpecialEmployee>(24, target => target._property25, (target, value) => target.Property25 = (int) value) },
		{ nameof(Property26), new PropertyToken<SpecialEmployee>(25, target => target._property26, (target, value) => target.Property26 = (int) value) },
		{ nameof(Property27), new PropertyToken<SpecialEmployee>(26, target => target._property27, (target, value) => target.Property27 = (int) value) },
		{ nameof(Property28), new PropertyToken<SpecialEmployee>(27, target => target._property28, (target, value) => target.Property28 = (int) value) },
		{ nameof(Property29), new PropertyToken<SpecialEmployee>(28, target => target._property29, (target, value) => target.Property29 = (int) value) },
		{ nameof(Property30), new PropertyToken<SpecialEmployee>(29, target => target._property30, (target, value) => target.Property30 = (int) value) },
		{ nameof(Property31), new PropertyToken<SpecialEmployee>(30, target => target._property31, (target, value) => target.Property31 = (int) value) },
		{ nameof(Property32), new PropertyToken<SpecialEmployee>(31, target => target._property32, (target, value) => target.Property32 = (int) value) },
		{ nameof(Property33), new PropertyToken<SpecialEmployee>(32, target => target._property33, (target, value) => target.Property33 = (int) value) },
		{ nameof(Property34), new PropertyToken<SpecialEmployee>(33, target => target._property34, (target, value) => target.Property34 = (int) value) },
		{ nameof(Property35), new PropertyToken<SpecialEmployee>(34, target => target._property35, (target, value) => target.Property35 = (int) value) },
		{ nameof(Property36), new PropertyToken<SpecialEmployee>(35, target => target._property36, (target, value) => target.Property36 = (int) value) },
		{ nameof(Property37), new PropertyToken<SpecialEmployee>(36, target => target._property37, (target, value) => target.Property37 = (int) value) },
		{ nameof(Property38), new PropertyToken<SpecialEmployee>(37, target => target._property38, (target, value) => target.Property38 = (int) value) },
		{ nameof(Property39), new PropertyToken<SpecialEmployee>(38, target => target._property39, (target, value) => target.Property39 = (int) value) },
		{ nameof(Property40), new PropertyToken<SpecialEmployee>(39, target => target._property40, (target, value) => target.Property40 = (int) value) },
		{ nameof(Property41), new PropertyToken<SpecialEmployee>(40, target => target._property41, (target, value) => target.Property41 = (int) value) },
		{ nameof(Property42), new PropertyToken<SpecialEmployee>(41, target => target._property42, (target, value) => target.Property42 = (int) value) },
		{ nameof(Property43), new PropertyToken<SpecialEmployee>(42, target => target._property43, (target, value) => target.Property43 = (int) value) },
		{ nameof(Property44), new PropertyToken<SpecialEmployee>(43, target => target._property44, (target, value) => target.Property44 = (int) value) },
		{ nameof(Property45), new PropertyToken<SpecialEmployee>(44, target => target._property45, (target, value) => target.Property45 = (int) value) },
		{ nameof(Property46), new PropertyToken<SpecialEmployee>(45, target => target._property46, (target, value) => target.Property46 = (int) value) },
		{ nameof(Property47), new PropertyToken<SpecialEmployee>(46, target => target._property47, (target, value) => target.Property47 = (int) value) },
		{ nameof(Property48), new PropertyToken<SpecialEmployee>(47, target => target._property48, (target, value) => target.Property48 = (int) value) },
		{ nameof(Property49), new PropertyToken<SpecialEmployee>(48, target => target._property49, (target, value) => target.Property49 = (int) value) },
		{ nameof(Property50), new PropertyToken<SpecialEmployee>(49, target => target._property50, (target, value) => target.Property50 = (int) value) },
		{ nameof(Property51), new PropertyToken<SpecialEmployee>(50, target => target._property51, (target, value) => target.Property51 = (int) value) },
		{ nameof(Property52), new PropertyToken<SpecialEmployee>(51, target => target._property52, (target, value) => target.Property52 = (int) value) },
		{ nameof(Property53), new PropertyToken<SpecialEmployee>(52, target => target._property53, (target, value) => target.Property53 = (int) value) },
		{ nameof(Property54), new PropertyToken<SpecialEmployee>(53, target => target._property54, (target, value) => target.Property54 = (int) value) },
		{ nameof(Property55), new PropertyToken<SpecialEmployee>(54, target => target._property55, (target, value) => target.Property55 = (int) value) },
		{ nameof(Property56), new PropertyToken<SpecialEmployee>(55, target => target._property56, (target, value) => target.Property56 = (int) value) },

		{ nameof(EmployeeId), new PropertyToken<SpecialEmployee>(56, target => target._employeeId, (target, value) => target.EmployeeId = (int) value) },
		{ nameof(Department), new PropertyToken<SpecialEmployee>(57, target => target._department, (target, value) => target.Department = (Department) value) },
		{ nameof(OfficeAddress), new PropertyToken<SpecialEmployee>(58, target => target._officeAddress, (target, value) => target.OfficeAddress = (Address) value) },
		{ nameof(Salary), new PropertyToken<SpecialEmployee>(59, target => target._salary, (target, value) => target.Salary = (decimal) value) },

		{ nameof(Name), new PropertyToken<SpecialEmployee>(60, target => target._name, (target, value) => target.Name = (string) value) },
		{ nameof(Gender), new PropertyToken<SpecialEmployee>(61, target => target._gender, (target, value) => target.Gender = (Gender?) value) },
		{ nameof(Birthdate), new PropertyToken<SpecialEmployee>(62, target => target._birthdate, (target, value) => target.Birthdate = (DateTime) value) },
		{ nameof(BloodType), new PropertyToken<SpecialEmployee>(63, target => target._bloodType, (target, value) => target.BloodType = (string) value) },
		{ nameof(HomeAddress), new PropertyToken<SpecialEmployee>(64, target => target._homeAddress, (target, value) => target.HomeAddress = (Address) value) },
	};
	#endregion

	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	#endregion

	#region 标记变量
	private readonly byte[] _MASKS_;
	#endregion

	#region 成员字段
	private int _property01;
	private int _property02;
	private int _property03;
	private int _property04;
	private int _property05;
	private int _property06;
	private int _property07;
	private int _property08;
	private int _property09;
	private int _property10;
	private int _property11;
	private int _property12;
	private int _property13;
	private int _property14;
	private int _property15;
	private int _property16;
	private int _property17;
	private int _property18;
	private int _property19;
	private int _property20;
	private int _property21;
	private int _property22;
	private int _property23;
	private int _property24;
	private int _property25;
	private int _property26;
	private int _property27;
	private int _property28;
	private int _property29;
	private int _property30;
	private int _property31;
	private int _property32;
	private int _property33;
	private int _property34;
	private int _property35;
	private int _property36;
	private int _property37;
	private int _property38;
	private int _property39;
	private int _property40;
	private int _property41;
	private int _property42;
	private int _property43;
	private int _property44;
	private int _property45;
	private int _property46;
	private int _property47;
	private int _property48;
	private int _property49;
	private int _property50;
	private int _property51;
	private int _property52;
	private int _property53;
	private int _property54;
	private int _property55;
	private int _property56;

	private int _employeeId;
	private Department _department;
	private Address _officeAddress;
	private decimal _salary;

	private string _name;
	private Gender? _gender;
	private DateTime _birthdate;
	private string _bloodType;
	private Address _homeAddress;
	#endregion

	#region 构造函数
	public SpecialEmployee()
	{
		_MASKS_ = new byte[(int)Math.Ceiling(PROPERTY_COUNT / 8.0)];
	}
	#endregion

	#region 公共属性
	public int Property01
	{
		get => _property01;
		set
		{
			if(_property01 == value)
				return;

			_property01 = value;
			//_MASK_[0 / 8] |= (byte)Math.Pow(2, 0 % 8);
			_MASKS_[0] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property01)));
		}
	}
	public int Property02
	{
		get => _property02;
		set
		{
			if(_property02 == value)
				return;

			_property02 = value;
			//_MASK_[1 / 8] |= (byte)Math.Pow(2, 1 % 8);
			_MASKS_[0] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property02)));
		}
	}
	public int Property03
	{
		get => _property03;
		set
		{
			if(_property03 == value)
				return;

			_property03 = value;
			//_MASK_[2 / 8] |= (byte)Math.Pow(2, 2 % 8);
			_MASKS_[0] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property03)));
		}
	}
	public int Property04
	{
		get => _property04;
		set
		{
			if(_property04 == value)
				return;

			_property04 = value;
			//_MASK_[3 / 8] |= (byte)Math.Pow(2, 3 % 8);
			_MASKS_[0] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property04)));
		}
	}
	public int Property05
	{
		get => _property05;
		set
		{
			if(_property05 == value)
				return;

			_property05 = value;
			//_MASK_[4 / 8] |= (byte)Math.Pow(2, 4 % 8);
			_MASKS_[0] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property05)));
		}
	}
	public int Property06
	{
		get => _property06;
		set
		{
			if(_property06 == value)
				return;

			_property06 = value;
			//_MASK_[5 / 8] |= (byte)Math.Pow(2, 5 % 8);
			_MASKS_[0] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property06)));
		}
	}
	public int Property07
	{
		get => _property07;
		set
		{
			if(_property07 == value)
				return;

			_property07 = value;
			//_MASK_[6 / 8] |= (byte)Math.Pow(2, 6 % 8);
			_MASKS_[0] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property07)));
		}
	}
	public int Property08
	{
		get => _property08;
		set
		{
			if(_property08 == value)
				return;

			_property08 = value;
			//_MASK_[7 / 8] |= (byte)Math.Pow(2, 7 % 8);
			_MASKS_[0] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property08)));
		}
	}
	public int Property09
	{
		get => _property09;
		set
		{
			if(_property09 == value)
				return;

			_property09 = value;
			//_MASK_[8 / 8] |= (byte)Math.Pow(2, 8 % 8);
			_MASKS_[1] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property09)));
		}
	}
	public int Property10
	{
		get => _property10;
		set
		{
			if(_property10 == value)
				return;

			_property10 = value;
			//_MASK_[9 / 8] |= (byte)Math.Pow(2, 9 % 8);
			_MASKS_[1] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property10)));
		}
	}
	public int Property11
	{
		get => _property11;
		set
		{
			if(_property11 == value)
				return;

			_property11 = value;
			//_MASK_[10 / 8] |= (byte)Math.Pow(2, 10 % 8);
			_MASKS_[1] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property11)));
		}
	}
	public int Property12
	{
		get => _property12;
		set
		{
			if(_property12 == value)
				return;

			_property12 = value;
			//_MASK_[11 / 8] |= (byte)Math.Pow(2, 11 % 8);
			_MASKS_[1] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property12)));
		}
	}
	public int Property13
	{
		get => _property13;
		set
		{
			if(_property13 == value)
				return;

			_property13 = value;
			//_MASK_[12 / 8] |= (byte)Math.Pow(2, 12 % 8);
			_MASKS_[1] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property13)));
		}
	}
	public int Property14
	{
		get => _property14;
		set
		{
			if(_property14 == value)
				return;

			_property14 = value;
			//_MASK_[13 / 8] |= (byte)Math.Pow(2, 13 % 8);
			_MASKS_[1] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property14)));
		}
	}
	public int Property15
	{
		get => _property15;
		set
		{
			if(_property15 == value)
				return;

			_property15 = value;
			//_MASK_[14 / 8] |= (byte)Math.Pow(2, 14 % 8);
			_MASKS_[1] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property15)));
		}
	}
	public int Property16
	{
		get => _property16;
		set
		{
			if(_property16 == value)
				return;

			_property16 = value;
			//_MASK_[15 / 8] |= (byte)Math.Pow(2, 15 % 8);
			_MASKS_[1] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property16)));
		}
	}
	public int Property17
	{
		get => _property17;
		set
		{
			if(_property17 == value)
				return;

			_property17 = value;
			//_MASK_[16 / 8] |= (byte)Math.Pow(2, 16 % 8);
			_MASKS_[2] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property17)));
		}
	}
	public int Property18
	{
		get => _property18;
		set
		{
			if(_property18 == value)
				return;

			_property18 = value;
			//_MASK_[17 / 8] |= (byte)Math.Pow(2, 17 % 8);
			_MASKS_[2] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property18)));
		}
	}
	public int Property19
	{
		get => _property19;
		set
		{
			if(_property19 == value)
				return;

			_property19 = value;
			//_MASK_[18 / 8] |= (byte)Math.Pow(2, 18 % 8);
			_MASKS_[2] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property19)));
		}
	}
	public int Property20
	{
		get => _property20;
		set
		{
			if(_property20 == value)
				return;

			_property20 = value;
			//_MASK_[19 / 8] |= (byte)Math.Pow(2, 19 % 8);
			_MASKS_[2] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property20)));
		}
	}
	public int Property21
	{
		get => _property21;
		set
		{
			if(_property21 == value)
				return;

			_property21 = value;
			//_MASK_[20 / 8] |= (byte)Math.Pow(2, 20 % 8);
			_MASKS_[2] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property21)));
		}
	}
	public int Property22
	{
		get => _property22;
		set
		{
			if(_property22 == value)
				return;

			_property22 = value;
			//_MASK_[21 / 8] |= (byte)Math.Pow(2, 21 % 8);
			_MASKS_[2] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property22)));
		}
	}
	public int Property23
	{
		get => _property23;
		set
		{
			if(_property23 == value)
				return;

			_property23 = value;
			//_MASK_[22 / 8] |= (byte)Math.Pow(2, 22 % 8);
			_MASKS_[2] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property23)));
		}
	}
	public int Property24
	{
		get => _property24;
		set
		{
			if(_property24 == value)
				return;

			_property24 = value;
			//_MASK_[23 / 8] |= (byte)Math.Pow(2, 23 % 8);
			_MASKS_[2] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property24)));
		}
	}
	public int Property25
	{
		get => _property25;
		set
		{
			if(_property25 == value)
				return;

			_property25 = value;
			//_MASK_[24 / 8] |= (byte)Math.Pow(2, 24 % 8);
			_MASKS_[3] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property25)));
		}
	}
	public int Property26
	{
		get => _property26;
		set
		{
			if(_property26 == value)
				return;

			_property26 = value;
			//_MASK_[25 / 8] |= (byte)Math.Pow(2, 25 % 8);
			_MASKS_[3] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property26)));
		}
	}
	public int Property27
	{
		get => _property27;
		set
		{
			if(_property27 == value)
				return;

			_property27 = value;
			//_MASK_[26 / 8] |= (byte)Math.Pow(2, 26 % 8);
			_MASKS_[3] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property27)));
		}
	}
	public int Property28
	{
		get => _property28;
		set
		{
			if(_property28 == value)
				return;

			_property28 = value;
			//_MASK_[27 / 8] |= (byte)Math.Pow(2, 27 % 8);
			_MASKS_[3] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property28)));
		}
	}
	public int Property29
	{
		get => _property29;
		set
		{
			if(_property29 == value)
				return;

			_property29 = value;
			//_MASK_[28 / 8] |= (byte)Math.Pow(2, 28 % 8);
			_MASKS_[3] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property29)));
		}
	}
	public int Property30
	{
		get => _property30;
		set
		{
			if(_property30 == value)
				return;

			_property30 = value;
			//_MASK_[29 / 8] |= (byte)Math.Pow(2, 29 % 8);
			_MASKS_[3] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property30)));
		}
	}
	public int Property31
	{
		get => _property31;
		set
		{
			if(_property31 == value)
				return;

			_property31 = value;
			//_MASK_[30 / 8] |= (byte)Math.Pow(2, 30 % 8);
			_MASKS_[3] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property31)));
		}
	}
	public int Property32
	{
		get => _property32;
		set
		{
			if(_property32 == value)
				return;

			_property32 = value;
			//_MASK_[31 / 8] |= (byte)Math.Pow(2, 31 % 8);
			_MASKS_[3] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property32)));
		}
	}
	public int Property33
	{
		get => _property33;
		set
		{
			if(_property33 == value)
				return;

			_property33 = value;
			//_MASK_[32 / 8] |= (byte)Math.Pow(2, 32 % 8);
			_MASKS_[4] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property33)));
		}
	}
	public int Property34
	{
		get => _property34;
		set
		{
			if(_property34 == value)
				return;

			_property34 = value;
			//_MASK_[33 / 8] |= (byte)Math.Pow(2, 33 % 8);
			_MASKS_[4] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property34)));
		}
	}
	public int Property35
	{
		get => _property35;
		set
		{
			if(_property35 == value)
				return;

			_property35 = value;
			//_MASK_[34 / 8] |= (byte)Math.Pow(2, 34 % 8);
			_MASKS_[4] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property35)));
		}
	}
	public int Property36
	{
		get => _property36;
		set
		{
			if(_property36 == value)
				return;

			_property36 = value;
			//_MASK_[35 / 8] |= (byte)Math.Pow(2, 35 % 8);
			_MASKS_[4] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property36)));
		}
	}
	public int Property37
	{
		get => _property37;
		set
		{
			if(_property37 == value)
				return;

			_property37 = value;
			//_MASK_[36 / 8] |= (byte)Math.Pow(2, 36 % 8);
			_MASKS_[4] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property37)));
		}
	}
	public int Property38
	{
		get => _property38;
		set
		{
			if(_property38 == value)
				return;

			_property38 = value;
			//_MASK_[37 / 8] |= (byte)Math.Pow(2, 37 % 8);
			_MASKS_[4] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property38)));
		}
	}
	public int Property39
	{
		get => _property39;
		set
		{
			if(_property39 == value)
				return;

			_property39 = value;
			//_MASK_[38 / 8] |= (byte)Math.Pow(2, 38 % 8);
			_MASKS_[4] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property39)));
		}
	}
	public int Property40
	{
		get => _property40;
		set
		{
			if(_property40 == value)
				return;

			_property40 = value;
			//_MASK_[39 / 8] |= (byte)Math.Pow(2, 39 % 8);
			_MASKS_[4] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property40)));
		}
	}
	public int Property41
	{
		get => _property41;
		set
		{
			if(_property41 == value)
				return;

			_property41 = value;
			//_MASK_[40 / 8] |= (byte)Math.Pow(2, 40 % 8);
			_MASKS_[5] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property41)));
		}
	}
	public int Property42
	{
		get => _property42;
		set
		{
			if(_property42 == value)
				return;

			_property42 = value;
			//_MASK_[41 / 8] |= (byte)Math.Pow(2, 41 % 8);
			_MASKS_[5] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property42)));
		}
	}
	public int Property43
	{
		get => _property43;
		set
		{
			if(_property43 == value)
				return;

			_property43 = value;
			//_MASK_[42 / 8] |= (byte)Math.Pow(2, 42 % 8);
			_MASKS_[5] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property43)));
		}
	}
	public int Property44
	{
		get => _property44;
		set
		{
			if(_property44 == value)
				return;

			_property44 = value;
			//_MASK_[43 / 8] |= (byte)Math.Pow(2, 43 % 8);
			_MASKS_[5] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property44)));
		}
	}
	public int Property45
	{
		get => _property45;
		set
		{
			if(_property45 == value)
				return;

			_property45 = value;
			//_MASK_[44 / 8] |= (byte)Math.Pow(2, 44 % 8);
			_MASKS_[5] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property45)));
		}
	}
	public int Property46
	{
		get => _property46;
		set
		{
			if(_property46 == value)
				return;

			_property46 = value;
			//_MASK_[45 / 8] |= (byte)Math.Pow(2, 45 % 8);
			_MASKS_[5] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property46)));
		}
	}
	public int Property47
	{
		get => _property47;
		set
		{
			if(_property47 == value)
				return;

			_property47 = value;
			//_MASK_[46 / 8] |= (byte)Math.Pow(2, 46 % 8);
			_MASKS_[5] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property47)));
		}
	}
	public int Property48
	{
		get => _property48;
		set
		{
			if(_property48 == value)
				return;

			_property48 = value;
			//_MASK_[47 / 8] |= (byte)Math.Pow(2, 47 % 8);
			_MASKS_[5] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property48)));
		}
	}
	public int Property49
	{
		get => _property49;
		set
		{
			if(_property49 == value)
				return;

			_property49 = value;
			//_MASK_[48 / 8] |= (byte)Math.Pow(2, 48 % 8);
			_MASKS_[6] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property49)));
		}
	}
	public int Property50
	{
		get => _property50;
		set
		{
			if(_property50 == value)
				return;

			_property50 = value;
			//_MASK_[49 / 8] |= (byte)Math.Pow(2, 49 % 8);
			_MASKS_[6] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property50)));
		}
	}
	public int Property51
	{
		get => _property51;
		set
		{
			if(_property51 == value)
				return;

			_property51 = value;
			//_MASK_[50 / 8] |= (byte)Math.Pow(2, 50 % 8);
			_MASKS_[6] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property51)));
		}
	}
	public int Property52
	{
		get => _property52;
		set
		{
			if(_property52 == value)
				return;

			_property52 = value;
			//_MASK_[51 / 8] |= (byte)Math.Pow(2, 51 % 8);
			_MASKS_[6] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property52)));
		}
	}
	public int Property53
	{
		get => _property53;
		set
		{
			if(_property53 == value)
				return;

			_property53 = value;
			//_MASK_[52 / 8] |= (byte)Math.Pow(2, 52 % 8);
			_MASKS_[6] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property53)));
		}
	}
	public int Property54
	{
		get => _property54;
		set
		{
			if(_property54 == value)
				return;

			_property54 = value;
			//_MASK_[53 / 8] |= (byte)Math.Pow(2, 53 % 8);
			_MASKS_[6] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property54)));
		}
	}
	public int Property55
	{
		get => _property55;
		set
		{
			if(_property55 == value)
				return;

			_property55 = value;
			//_MASK_[54 / 8] |= (byte)Math.Pow(2, 54 % 8);
			_MASKS_[6] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property55)));
		}
	}
	public int Property56
	{
		get => _property56;
		set
		{
			if(_property56 == value)
				return;

			_property56 = value;
			//_MASK_[55 / 8] |= (byte)Math.Pow(2, 55 % 8);
			_MASKS_[6] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property56)));
		}
	}

	public int EmployeeId
	{
		get => _employeeId;
		set
		{
			if(_employeeId == value)
				return;

			_employeeId = value;
			//_MASK_[56 / 8] |= (byte)Math.Pow(2, 56 % 8);
			_MASKS_[7] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmployeeId)));
		}
	}
	public Department Department
	{
		get => _department;
		set
		{
			if(object.Equals(_department, value))
				return;

			_department = value;
			//_MASK_[57 / 8] |= (byte)Math.Pow(2, 57 % 8);
			_MASKS_[7] |= 2;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Department)));
		}
	}
	public Address OfficeAddress
	{
		get => _officeAddress;
		set
		{
			if(object.Equals(_officeAddress, value))
				return;

			_officeAddress = value;
			//_MASK_[58 / 8] |= (byte)Math.Pow(2, 58 % 8);
			_MASKS_[7] |= 4;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OfficeAddress)));
		}
	}
	public decimal Salary
	{
		get => _salary;
		set
		{
			if(_salary == value)
				return;

			_salary = value;
			//_MASK_[59 / 8] |= (byte)Math.Pow(2, 59 % 8);
			_MASKS_[7] |= 8;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Salary)));
		}
	}

	public string Name
	{
		get => _name;
		set
		{
			if(_name == value)
				return;

			_name = value;
			//_MASK_[60 / 8] |= (byte)Math.Pow(2, 60 % 8);
			_MASKS_[7] |= 16;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
		}
	}
	public Gender? Gender
	{
		get => _gender;
		set
		{
			if(_gender == value)
				return;

			_gender = value;
			//_MASK_[61 / 8] |= (byte)Math.Pow(2, 61 % 8);
			_MASKS_[7] |= 32;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gender)));
		}
	}
	public DateTime Birthdate
	{
		get => _birthdate;
		set
		{
			if(_birthdate == value)
				return;

			_birthdate = value;
			//_MASK_[62 / 8] |= (byte)Math.Pow(2, 62 % 8);
			_MASKS_[7] |= 64;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Birthdate)));
		}
	}
	public string BloodType
	{
		get => _bloodType;
		set
		{
			if(_bloodType == value)
				return;

			_bloodType = value;
			//_MASK_[63 / 8] |= (byte)Math.Pow(2, 63 % 8);
			_MASKS_[7] |= 128;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BloodType)));
		}
	}
	public Address HomeAddress
	{
		get => _homeAddress;
		set
		{
			if(_homeAddress == value)
				return;

			_homeAddress = value;
			//_MASK_[64 / 8] |= (byte)Math.Pow(2, 64 % 8);
			_MASKS_[8] |= 1;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomeAddress)));
		}
	}
	#endregion

	#region 接口实现
	public int GetCount() => 0;
	public void Reset(params string[] names) { }
	public bool Reset(string name, out object value)
	{
		value = null;
		return false;
	}

	bool Zongsoft.Data.IModel.HasChanges(params string[] names)
	{
		if(names == null || names.Length == 0)
		{
			for(int i = 0; i < _MASKS_.Length; i++)
			{
				if(_MASKS_[i] != 0)
					return true;
			}

			return false;
		}

		for(var i = 0; i < names.Length; i++)
		{
			if(__TOKENS__.TryGetValue(names[i], out var property) && property.Setter != null && (_MASKS_[property.Ordinal / 8] >> (property.Ordinal % 8) & 1) == 1)
				return true;
		}

		return false;
	}

	IDictionary<string, object> Zongsoft.Data.IModel.GetChanges()
	{
		var dictionary = new Dictionary<string, object>(__NAMES__.Length);

		for(int i = 0; i < __NAMES__.Length; i++)
		{
			if((_MASKS_[i / 8] >> (i % 8) & 1) == 1)
			{
				dictionary[__NAMES__[i]] = __TOKENS__[__NAMES__[i]].Getter(this);
			}
		}

		return dictionary.Count == 0 ? null : dictionary;
	}

	bool Zongsoft.Data.IModel.TryGetValue(string name, out object value)
	{
		value = null;

		if(__TOKENS__.TryGetValue(name, out var property) && (property.Ordinal < 0 || (_MASKS_[property.Ordinal / 8] >> (property.Ordinal % 8) & 1) == 1))
		{
			value = property.Getter(this);
			return true;
		}

		return false;
	}

	bool Zongsoft.Data.IModel.TrySetValue(string name, object value)
	{
		if(__TOKENS__.TryGetValue(name, out var property) && property.Setter != null)
		{
			property.Setter(this, value);
			return true;
		}

		return false;
	}
	#endregion
}
