using System;
using System.Data;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataTypeTest
{
	[Fact]
	public void Test()
	{
		var type1 = DataType.Get("string");
		Assert.Equal(nameof(DbType.String), type1.Name, true);
		Assert.Equal(DbType.String, type1.DbType);
		Assert.False(type1.IsArray);

		var type2 = (DataType)DbType.String;
		Assert.Equal(nameof(DbType.String), type2.Name, true);
		Assert.Equal(DbType.String, type2.DbType);
		Assert.False(type2.IsArray);

		Assert.Equal(type1, type2);
		Assert.Equal(type1.GetHashCode(), type2.GetHashCode());

		var binary1 = DataType.Get("binary");
		Assert.Equal("binary", binary1.Name, true);
		Assert.Equal(DbType.Binary, binary1.DbType);
		Assert.False(binary1.IsArray);
		Assert.Equal(DataType.Binary, binary1);

		var binary2 = DataType.Get("varbinary");
		Assert.Equal("binary", binary2.Name, true);
		Assert.Equal(DbType.Binary, binary2.DbType);
		Assert.False(binary2.IsArray);
		Assert.Equal(DataType.Binary, binary2);

		Assert.Equal(binary1, binary2);
		Assert.Equal(binary1.GetHashCode(), binary2.GetHashCode());

		var json1 = DataType.Get("Json");
		Assert.Equal("json", json1.Name, true);
		Assert.Equal(DbType.String, json1.DbType);
		Assert.False(json1.IsArray);
		Assert.Equal(DataType.Json, json1);

		var json2 = DataType.Get("JSON");
		Assert.Equal("json", json2.Name, true);
		Assert.Equal(DbType.String, json2.DbType);
		Assert.False(json2.IsArray);
		Assert.Equal(DataType.Json, json2);

		Assert.Equal(json1, json2);
		Assert.Equal(json1.GetHashCode(), json2.GetHashCode());
	}

	[Fact]
	public void TestArray()
	{
		var type = DataType.Get(" double [ ] ");
		Assert.Equal(nameof(DbType.Double), type.Name, true);
		Assert.Equal(DbType.Double, type.DbType);
		Assert.True(type.IsArray);
	}
}
