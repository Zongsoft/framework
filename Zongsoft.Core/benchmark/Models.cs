using System;

namespace Zongsoft.Benchmarks;

public enum Gender
{
	Female,
	Male,
}

public class Address
{
	public string City { get; set; }
	public string Detail { get; set; }
	public string PostalCode { get; set; }
	public int CountryId { get; set; }
}

public abstract class Person
{
	public abstract string Name { get; set; }
	public abstract Gender? Gender { get; set; }
	public abstract DateTime Birthdate { get; set; }
	public abstract string BloodType { get; set; }
	public abstract Address HomeAddress { get; set; }
}

public class PersonModel
{
	public string Name { get; set; }
	public Gender? Gender { get; set; }
	public DateTime Birthdate { get; set; }
	public string BloodType { get; set; }
	public Address HomeAddress { get; set; }

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
}
