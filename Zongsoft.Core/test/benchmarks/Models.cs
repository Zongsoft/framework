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

	public interface IPerson
	{
		string Name { get; set; }
		Gender? Gender { get; set; }
		DateTime Birthdate { get; set; }
		string BloodType { get; set; }
		Address HomeAddress { get; set; }
	}

	public class PersonModel : IPerson
	{
		public int PersonId { get; set; }
		public string Name { get; set; }
		public Gender? Gender { get; set; }
		public DateTime Birthdate { get; set; }
		public string BloodType { get; set; }
		public Address HomeAddress { get; set; }
	}
}
