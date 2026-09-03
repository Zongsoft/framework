using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataTypeTest
{
	private const string EqualsProbeEnvironment = "ZONGSOFT_DATATYPE_EQUALS_PROBE";

	[Fact]
	public async Task Equals_ObjectWithEquivalentDataType_ChildProcessExitsSuccessfully()
	{
		if(string.Equals(Environment.GetEnvironmentVariable(EqualsProbeEnvironment), "1", StringComparison.Ordinal))
		{
			object first = DataType.Get(typeof(int));
			object second = DataType.Get(typeof(int));

			Assert.True(first.Equals(second));
			Assert.False(first.Equals(null));
			Assert.False(first.Equals("int"));
			return;
		}

		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
		};

		start.ArgumentList.Add(typeof(DataTypeTest).Assembly.Location);
		start.ArgumentList.Add("-method");
		start.ArgumentList.Add($"{typeof(DataTypeTest).FullName}.{nameof(Equals_ObjectWithEquivalentDataType_ChildProcessExitsSuccessfully)}");
		start.ArgumentList.Add("-parallel");
		start.ArgumentList.Add("none");
		start.Environment[EqualsProbeEnvironment] = "1";

		using var process = Process.Start(start);
		Assert.NotNull(process);

		var standardOutput = process.StandardOutput.ReadToEndAsync();
		var standardError = process.StandardError.ReadToEndAsync();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

		try
		{
			await process.WaitForExitAsync(cancellation.Token);
		}
		catch(OperationCanceledException)
		{
			process.Kill(true);
			throw new TimeoutException("The isolated DataType.Equals probe did not exit within 20 seconds.");
		}

		var output = await standardOutput;
		var error = await standardError;
		Assert.True(process.ExitCode == 0, $"The isolated DataType.Equals probe exited with code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
	}

	[Fact]
	public void Equality_EquivalentInstances_WorkAsHashKeysAndOperators()
	{
		var first = DataType.Get(typeof(int));
		var second = DataType.Get(typeof(int));
		var array = DataType.Get(typeof(int[]));
		var dictionary = new Dictionary<DataType, string> { [first] = "integer" };

		Assert.NotSame(first, second);
		Assert.True(first.Equals(second));
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.True(first == second);
		Assert.False(first != second);
		Assert.False(first.Equals((DataType)null));
		Assert.False(first.Equals(array));
		Assert.True(dictionary.TryGetValue(second, out var value));
		Assert.Equal("integer", value);
	}

	[Fact]
	public void TestGet()
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
	public void TestConvert()
	{
		var type1 = Common.Convert.ConvertValue<DataType>("string");
		Assert.Equal(nameof(DbType.String), type1.Name, true);
		Assert.Equal(DbType.String, type1.DbType);
		Assert.False(type1.IsArray);

		var type2 = (DataType)DbType.String;
		Assert.Equal(nameof(DbType.String), type2.Name, true);
		Assert.Equal(DbType.String, type2.DbType);
		Assert.False(type2.IsArray);

		Assert.Equal(type1, type2);
		Assert.Equal(type1.GetHashCode(), type2.GetHashCode());

		var binary1 = Common.Convert.ConvertValue<DataType>("binary");
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

		var json1 = Common.Convert.ConvertValue<DataType>("Json");
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
