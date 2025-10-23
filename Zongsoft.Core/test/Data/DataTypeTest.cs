using System;
using System.Data;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataTypeTest
{
	[Fact]
	public void Test()
	{
		var type1 = new DataType("string");
		Assert.Equal(nameof(DbType.String), type1.Name, true);
		Assert.Equal(DbType.String, type1.DbType);
		Assert.False(type1.IsArray);

		var type2 = new DataType(DbType.String);
		Assert.Equal(nameof(DbType.String), type2.Name, true);
		Assert.Equal(DbType.String, type2.DbType);
		Assert.False(type2.IsArray);

		Assert.Equal(type1, type2);
		Assert.Equal(type1.GetHashCode(), type2.GetHashCode());

		var json1 = new DataType(DbType.String, "json");
		Assert.Equal("json", json1.Name, true);
		Assert.Equal(DbType.String, json1.DbType);
		Assert.False(json1.IsArray);

		var json2 = new DataType("JSON", DbType.String);
		Assert.Equal("json", json2.Name, true);
		Assert.Equal(DbType.String, json2.DbType);
		Assert.False(json2.IsArray);

		Assert.Equal(json1, json2);
		Assert.Equal(json1.GetHashCode(), json2.GetHashCode());
	}

	[Fact]
	public void TestArray()
	{
		var type = new DataType(" double [ ] ");
		Assert.Equal(nameof(DbType.Double), type.Name, true);
		Assert.Equal(DbType.Double, type.DbType);
		Assert.True(type.IsArray);
	}
}
