using System;

using Xunit;

namespace Zongsoft.Data.Metadata.Tests;

public class DataEntityPropertyFunctionTest
{
	[Fact]
	public void TestNow()
	{
		var function = DataEntityPropertyFunction.Get(null);
		Assert.Null(function);
		function = DataEntityPropertyFunction.Get(string.Empty);
		Assert.Null(function);

		function = DataEntityPropertyFunction.Get("now()");
		Assert.NotNull(function);
		Assert.Equal("now", function.Name, true);
		Assert.False(function.HasArguments);

		var value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<DateTime>(value);
		Assert.Equal(DateTimeKind.Local, ((DateTime)value).Kind);
		Assert.Equal(DateTime.Today, ((DateTime)value).Date);

		function = DataEntityPropertyFunction.Get("Now ( utc ) ");
		Assert.NotNull(function);
		Assert.Equal("now", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<DateTime>(value);
		Assert.Equal(DateTimeKind.Utc, ((DateTime)value).Kind);
		Assert.Equal(DateTime.Today, ((DateTime)value).Date);
	}

	[Fact]
	public void TestToday()
	{
		var function = DataEntityPropertyFunction.Get(null);
		Assert.Null(function);
		function = DataEntityPropertyFunction.Get(string.Empty);
		Assert.Null(function);

		function = DataEntityPropertyFunction.Get("today()");
		Assert.NotNull(function);
		Assert.Equal("today", function.Name, true);
		Assert.False(function.HasArguments);

		var value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<DateTime>(value);
		Assert.Equal(DateTimeKind.Local, ((DateTime)value).Kind);
		Assert.Equal(DateTime.Today, (DateTime)value);

		function = DataEntityPropertyFunction.Get("Today(utc) ");
		Assert.NotNull(function);
		Assert.Equal("today", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("utc", function.Arguments[0], true);

		value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<DateTime>(value);
		Assert.Equal(DateTimeKind.Utc, ((DateTime)value).Kind);
		Assert.Equal(DateTime.UtcNow.Date, (DateTime)value);
	}

	[Fact]
	public void TestGuid()
	{
		var function = DataEntityPropertyFunction.Get("GUID ()");
		Assert.NotNull(function);
		Assert.Equal("guid", function.Name, true);
		Assert.False(function.HasArguments);

		var value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<Guid>(value);
		Assert.NotEqual(Guid.Empty, (Guid)value);

		#if NET9_0_OR_GREATER
		function = DataEntityPropertyFunction.Get("guid(sequential)");
		Assert.NotNull(function);
		Assert.Equal("guid", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("sequential", function.Arguments[0], true);

		value = function.Execute(null);
		Assert.NotNull(value);
		Assert.IsType<Guid>(value);
		Assert.True(((Guid)value).Version >= 7);
		Assert.NotEqual(Guid.Empty, (Guid)value);
		#endif
	}

	[Fact]
	public void TestRandom()
	{
		var function = DataEntityPropertyFunction.Get("random()");
		Assert.NotNull(function);
		Assert.Equal("random", function.Name, true);
		Assert.False(function.HasArguments);

		var value = function.Execute(null);
		Assert.Null(value);
		value = function.Execute(new DataEntitySimplexProperty("Guid", DataType.Guid, false));
		Assert.IsType<Guid>(value);
		value = function.Execute(new DataEntitySimplexProperty("Byte", DataType.Byte, false));
		Assert.IsType<byte>(value);
		value = function.Execute(new DataEntitySimplexProperty("SByte", DataType.SByte, false));
		Assert.IsType<sbyte>(value);
		value = function.Execute(new DataEntitySimplexProperty("Int16", DataType.Int16, false));
		Assert.IsType<short>(value);
		value = function.Execute(new DataEntitySimplexProperty("Int32", DataType.Int32, false));
		Assert.IsType<int>(value);
		value = function.Execute(new DataEntitySimplexProperty("Int64", DataType.Int64, false));
		Assert.IsType<long>(value);
		value = function.Execute(new DataEntitySimplexProperty("UInt16", DataType.UInt16, false));
		Assert.IsType<ushort>(value);
		value = function.Execute(new DataEntitySimplexProperty("UInt32", DataType.UInt32, false));
		Assert.IsType<uint>(value);
		value = function.Execute(new DataEntitySimplexProperty("UInt64", DataType.UInt64, false));
		Assert.IsType<ulong>(value);
		value = function.Execute(new DataEntitySimplexProperty("Single", DataType.Single, false));
		Assert.IsType<float>(value);
		value = function.Execute(new DataEntitySimplexProperty("Double", DataType.Double, false));
		Assert.IsType<double>(value);
		value = function.Execute(new DataEntitySimplexProperty("Decimal", DataType.Decimal, false));
		Assert.IsType<decimal>(value);
		value = function.Execute(new DataEntitySimplexProperty("Binary", DataType.Binary, 50, false));
		Assert.IsType<byte[]>(value);
		value = function.Execute(new DataEntitySimplexProperty("String", DataType.String, 50, false));
		Assert.IsType<string>(value);

		function = DataEntityPropertyFunction.Get("random(15)");
		Assert.NotNull(function);
		Assert.Equal("random", function.Name, true);
		Assert.True(function.HasArguments);
		Assert.Single(function.Arguments);
		Assert.Equal("15", function.Arguments[0], true);

		value = function.Execute(new DataEntitySimplexProperty("Binary", DataType.Binary, 50, false));
		Assert.IsType<byte[]>(value);
		Assert.Equal(15, ((byte[])value).Length);

		value = function.Execute(new DataEntitySimplexProperty("String", DataType.String, 50, false));
		Assert.IsType<string>(value);
		Assert.Equal(15, ((string)value).Length);
	}
}
