using System;

namespace Zongsoft.Benchmarks;

public enum Gender : byte
{
	Female,
	Male,
}

public class Address
{
	#region 公共属性
	public string City { get; set; }
	public string Detail { get; set; }
	public string PostalCode { get; set; }
	public int CountryId { get; set; }
	#endregion
}

public abstract class Person
{
	#region 公共属性
	public abstract string Name { get; set; }
	public abstract Gender? Gender { get; set; }
	public abstract DateTime Birthdate { get; set; }
	public abstract string BloodType { get; set; }
	public abstract Address HomeAddress { get; set; }
	#endregion
}

public class PersonModel
{
	#region 公共属性
	public string Name { get; set; }
	public Gender? Gender { get; set; }
	public DateTime Birthdate { get; set; }
	public string BloodType { get; set; }
	public Address HomeAddress { get; set; }
	#endregion

	#region 静态方法
	public static PersonModel Create() => new()
	{
		Name = "Popeye Zhong",
		Gender = Zongsoft.Benchmarks.Gender.Male,
		Birthdate = DateTime.Now,
		BloodType = "AB",
		HomeAddress = new()
		{
			City = "Shanghai",
			Detail = "Pudong New Area, Zhangjiang High-Tech Park",
			PostalCode = "201203",
			CountryId = 86,
		},
	};
	#endregion
}
