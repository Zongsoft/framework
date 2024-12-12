using System;
using System.Text.Json;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Serialization;

using Zongsoft.Tests;

namespace Zongsoft.Collections.Tests
{
	public class ParametersTest
	{
		[Fact]
		public void TestJson()
		{
			const string STRING_VALUE_WITH_NAME = "String Value(with name).";
			const string STRING_VALUE_WITHOUT_NAME = "String Value(without name).";

			Guid guid = Guid.NewGuid();
			DateTime today = DateTime.Today;
			DateTimeOffset now = DateTime.UtcNow;
			TimeSpan span = TimeSpan.FromDays(1);
			byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

			var personName = "Popeye";
			var personGender = Gender.Male;
			var personBirthdate = new DateTime(1979, 6, 9, 17, 30, 59);

			var model = Data.Model.Build<IPerson>(person =>
			{
				person.Name = personName;
				person.Gender = personGender;
				person.Birthdate = personBirthdate;
			});

			var parameters = Parameters
				.Parameter("MyNull", null)
				.Parameter("DBNull", DBNull.Value)
				.Parameter("MyGuid", guid)
				.Parameter("MyTrue", true)
				.Parameter("MyFalse", false)
				.Parameter("MyByte", (byte)10)
				.Parameter("MySByte", (sbyte)20)
				.Parameter("MyInt32", 100)
				.Parameter("MyUInt32", 200U)
				.Parameter("MyInt64", 100000L)
				.Parameter("MyUInt64", 200000UL)
				.Parameter("MySingle", 100.5f)
				.Parameter("MyDouble", 200.9d)
				.Parameter("MyDecimal", 300.3m)
				.Parameter("MyString", STRING_VALUE_WITH_NAME)
				.Parameter(STRING_VALUE_WITHOUT_NAME)
				.Parameter("MyTimeSpan", span)
				.Parameter("MyDateTime", today)
				.Parameter("MyDateTimeOffset", now)
				.Parameter("MyBuffer", data)
				.Parameter(model);

			var json = Serializer.Json.Serialize(parameters);
			Assert.NotNull(json);
			Assert.NotEmpty(json);

			var result = Serializer.Json.Deserialize<Parameters>(json);
			Assert.NotNull(result);
			Assert.NotEmpty(result);

			Assert.True(result.TryGetValue("MyNull", out var value));
			Assert.Null(value);
			Assert.True(result.TryGetValue("DbNull", out value));
			Assert.Null(value);
			Assert.True(result.TryGetValue("MyGUID", out value));
			Assert.Equal(guid, (Guid)value);
			Assert.True(result.TryGetValue("MyTrue", out value));
			Assert.True((bool)value);
			Assert.True(result.TryGetValue("MyFalse", out value));
			Assert.False((bool)value);
			Assert.True(result.TryGetValue("myByte", out value));
			Assert.Equal(10, (byte)value);
			Assert.True(result.TryGetValue("mySByte", out value));
			Assert.Equal(20, (sbyte)value);
			Assert.True(result.TryGetValue("MyInt32", out value));
			Assert.Equal(100, (int)value);
			Assert.True(result.TryGetValue("MyUInt32", out value));
			Assert.Equal(200U, (uint)value);
			Assert.True(result.TryGetValue("MyInt64", out value));
			Assert.Equal(100000L, (long)value);
			Assert.True(result.TryGetValue("MyUInt64", out value));
			Assert.Equal(200000UL, (ulong)value);
			Assert.True(result.TryGetValue("MySingle", out value));
			Assert.Equal(100.5f, (float)value);
			Assert.True(result.TryGetValue("MyDouble", out value));
			Assert.Equal(200.9d, (double)value);
			Assert.True(result.TryGetValue("MyDecimal", out value));
			Assert.Equal(300.3m, (decimal)value);

			Assert.True(result.TryGetValue("MyTimeSpan", out value));
			Assert.Equal(span, (TimeSpan)value);
			Assert.True(result.TryGetValue("MyDateTime", out value));
			Assert.Equal(today, (DateTime)value);
			Assert.True(result.TryGetValue("MyDateTimeOffset", out value));
			Assert.Equal(now, (DateTimeOffset)value);

			Assert.True(result.TryGetValue("MyString", out value));
			Assert.Equal(STRING_VALUE_WITH_NAME, (string)value);
			Assert.True(result.TryGetValue(typeof(string), out value));
			Assert.Equal(STRING_VALUE_WITHOUT_NAME, (string)value);

			Assert.True(result.TryGetValue("MyBuffer", out value));
			Assert.NotNull(value);
			Assert.IsType<byte[]>(value);
			Assert.True(System.Linq.Enumerable.SequenceEqual(data, (byte[])value));

			Assert.True(result.TryGetValue<IPerson>(out var person));
			Assert.NotNull(person);
			Assert.Equal(personName, person.Name);
			Assert.Equal(personGender, person.Gender);
			Assert.Equal(personBirthdate, person.Birthdate);
		}
	}
}