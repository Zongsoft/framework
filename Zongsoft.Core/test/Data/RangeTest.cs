using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class RangeTest
	{
		[Fact]
		public void Test()
		{
			object min, max;

			var age = new Range<byte>(100, 18);
			var ageText = age.ToString();

			var birthdate = new Range<DateTime>(new DateTime(1979, 6, 9), null);
			var birthdateText = birthdate.ToString();

			var range = Range<int>.Parse(ageText);
			Assert.Equal(18, range.Minimum);
			Assert.Equal(100, range.Maximum);

			Assert.True(Range.IsRange(age, out min, out max));
			Assert.Equal(18, (byte)min);
			Assert.Equal(100, (byte)max);

			Assert.True(Range.IsRange(birthdate, out min, out max));
			Assert.Equal(new DateTime(1979, 6, 9), (DateTime)min);
			Assert.Null(max);
		}

		[Fact]
		public void TestConverter()
		{
			const string JSON = @"{""age"":""18~40"",""timestamp"":""2020-6-1~2020-12-31""}";
			var data = Serialization.Serializer.Json.Deserialize<TestConditional>(JSON);

			Assert.NotNull(data);
			Assert.NotNull(data.Age);
			Assert.NotNull(data.Timestamp);

			Assert.True(data.Birthday.IsEmpty);

			Assert.Equal(18, (int)data.Age.Value.Minimum);
			Assert.Equal(40, (int)data.Age.Value.Maximum);
			Assert.Equal(new DateTime(2020, 6, 1), data.Timestamp.Value.Minimum);
			Assert.Equal(new DateTime(2020, 12, 31), data.Timestamp.Value.Maximum);
		}
	}

	public class TestConditional
	{
		public Range<byte>? Age
		{
			get; set;
		}

		public Range<DateTime>? Timestamp
		{
			get; set;
		}

		public Range<DateTime> Birthday
		{
			get; set;
		}
	}
}
