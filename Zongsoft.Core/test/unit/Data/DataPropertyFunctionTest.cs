using System;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataPropertyFunctionTest
{
	[Fact]
	public void TestParse()
	{
		var function = DataPropertyFunction.Parse(null);
		Assert.Null(function.Name);
		function = DataPropertyFunction.Parse(string.Empty);
		Assert.Null(function.Name);
		function = DataPropertyFunction.Parse("\t");
		Assert.Null(function.Name);

		function = DataPropertyFunction.Parse("now()");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);
		function = DataPropertyFunction.Parse("Now ()");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);
		function = DataPropertyFunction.Parse("Now (  )");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);

		function = DataPropertyFunction.Parse("now(utc)");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = DataPropertyFunction.Parse("now (utc)");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = DataPropertyFunction.Parse("now( utc )");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = DataPropertyFunction.Parse(" now  ( utc ) ");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = DataPropertyFunction.Parse(" foo  ( a,b, c ) ");
		Assert.Equal("foo", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Equal(3, function.Arguments.Length);
		Assert.Equal("a", function.Arguments[0], true);
		Assert.Equal("b", function.Arguments[1], true);
		Assert.Equal("c", function.Arguments[2], true);
	}

	[Fact]
	public void TestConvert()
	{
		Assert.False(Common.Convert.TryConvertValue<DataPropertyFunction>(string.Empty, out _));
		Assert.False(Common.Convert.TryConvertValue<DataPropertyFunction>("\t", out _));

		var function = Common.Convert.ConvertValue<DataPropertyFunction>("now()");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);
		function = Common.Convert.ConvertValue<DataPropertyFunction>("Now ()");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);
		function = Common.Convert.ConvertValue<DataPropertyFunction>("Now (  )");
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);

		function = Common.Convert.ConvertValue<DataPropertyFunction>("now(utc)");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = Common.Convert.ConvertValue<DataPropertyFunction>("now (utc)");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = Common.Convert.ConvertValue<DataPropertyFunction>("now( utc )");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = Common.Convert.ConvertValue<DataPropertyFunction>(" now  ( utc ) ");
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		function = Common.Convert.ConvertValue<DataPropertyFunction>(" foo  ( a,b, c ) ");
		Assert.Equal("foo", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Equal(3, function.Arguments.Length);
		Assert.Equal("a", function.Arguments[0], true);
		Assert.Equal("b", function.Arguments[1], true);
		Assert.Equal("c", function.Arguments[2], true);
	}

	[Fact]
	public void TestSerialize()
	{
		var json = Serialization.Serializer.Json.Serialize(default(DataPropertyFunction));
		Assert.Equal("null", json);
		var function = Serialization.Serializer.Json.Deserialize<DataPropertyFunction>(json);
		Assert.Null(function.Name);

		json = Serialization.Serializer.Json.Serialize(DataPropertyFunction.Parse("now()"));
		Assert.Equal("now()", json.Trim('"'), true);
		function = Serialization.Serializer.Json.Deserialize<DataPropertyFunction>(json);
		Assert.Equal("now", function.Name);
		Assert.False(function.HasArguments);

		json = Serialization.Serializer.Json.Serialize(DataPropertyFunction.Parse("now(utc)"));
		Assert.Equal("now(utc)", json.Trim('"'), true);
		function = Serialization.Serializer.Json.Deserialize<DataPropertyFunction>(json);
		Assert.Equal("now", function.Name);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		json = Serialization.Serializer.Json.Serialize(DataPropertyFunction.Parse(" foo  ( a,b, c ) "));
		Assert.Equal("foo(a,b,c)", json.Trim('"'), true);
		function = Serialization.Serializer.Json.Deserialize<DataPropertyFunction>(json);
		Assert.Equal("foo", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Equal(3, function.Arguments.Length);
		Assert.Equal("a", function.Arguments[0], true);
		Assert.Equal("b", function.Arguments[1], true);
		Assert.Equal("c", function.Arguments[2], true);
	}
}
