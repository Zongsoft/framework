using System;

namespace Zongsoft.Data.Benchmarks
{
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
	}
}
