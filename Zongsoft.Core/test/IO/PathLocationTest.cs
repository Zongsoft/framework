using System;
using System.Text.Json;

using Xunit;

namespace Zongsoft.IO.Tests;

public class PathLocationTest
{
	[Fact]
	public void TestDeserialize()
	{
		const string PATH = "zfs.oss:/data/dir1/popeye-avatar.jpg";

		var json = JsonSerializer.Serialize(new Person()
		{
			PersonId = 100,
			Name = "Popeye Zhong",
			PhotoPath = new PathLocation(PATH),
			Birthdate = new DateTime(2000, 10, 30),
		});

		var result = JsonSerializer.Deserialize<Person>(json);
		Assert.Equal(100U, result.PersonId);
		Assert.Equal("Popeye Zhong", result.Name);
		Assert.Equal(new DateTime(2000, 10, 30), result.Birthdate);
		Assert.Equal(PATH, result.PhotoPath.Path);
	}

	public struct Person
	{
		public uint PersonId { get; set; }
		public string Name { get; set; }
		public PathLocation PhotoPath { get; set; }
		public DateTime? Birthdate { get; set; }
	}
}
