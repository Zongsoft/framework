using System;
using System.Data;

using Xunit;

using Zongsoft.Tests;
using Zongsoft.Serialization;

namespace Zongsoft.Data.Tests;

public class ModelDescriptorTest
{
	[Fact]
	public void Test()
	{
		TestLogModel(Model.GetDescriptor<Log>());
		TestEmployeeModel(Model.GetDescriptor<EmployeeBase>());
	}

	[Fact]
	public void TestSerialize1()
	{
		var descriptor = Model.GetDescriptor<Log>();
		var json = Serializer.Json.Serialize(descriptor);
		Assert.NotEmpty(json);

		var result = Serializer.Json.Deserialize<ModelDescriptor>(json);
		Assert.NotNull(result);
		TestLogModel(result);
	}

	[Fact]
	public void TestSerialize2()
	{
		var descriptor = Model.GetDescriptor<EmployeeBase>();
		var json = Serializer.Json.Serialize(descriptor);
		Assert.NotEmpty(json);

		var result = Serializer.Json.Deserialize<ModelDescriptor>(json);
		Assert.NotNull(result);
		TestEmployeeModel(result);
	}

	private static void TestEmployeeModel(ModelDescriptor descriptor)
	{
		Assert.NotNull(descriptor);
		Assert.Equal(nameof(EmployeeBase), descriptor.Name);
		Assert.Equal(typeof(EmployeeBase), descriptor.Type);
		Assert.NotNull(descriptor.Properties);
		Assert.NotEmpty(descriptor.Properties);

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.EmployeeId), out var property));
		Assert.Equal(nameof(EmployeeBase.EmployeeId), property.Name);
		Assert.Equal(typeof(int), property.Type);
		Assert.True(property.IsSimplex(out var simplex));
		Assert.Equal(DataType.Int32, simplex.DataType);

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.Name), out property));
		Assert.Equal(nameof(EmployeeBase.Name), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.String, simplex.DataType);

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.Gender), out property));
		Assert.Equal(nameof(EmployeeBase.Gender), property.Name);
		Assert.Equal(typeof(Gender?), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.Byte, simplex.DataType);

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.Department), out property));
		Assert.Equal(nameof(EmployeeBase.Department), property.Name);
		Assert.Equal(typeof(Department), property.Type);
		Assert.True(property.IsComplex(out var complex));
		Assert.Equal(nameof(Department), complex.Port);
		Assert.Equal(Metadata.DataAssociationMultiplicity.ZeroOrOne, complex.Multiplicity);
	}

	private static void TestLogModel(ModelDescriptor descriptor)
	{
		Assert.NotNull(descriptor);
		Assert.Equal("Logs", descriptor.Name);
		Assert.Equal(typeof(Log), descriptor.Type);
		Assert.NotNull(descriptor.Properties);
		Assert.NotEmpty(descriptor.Properties);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.LogId), out var property));
		Assert.Equal(nameof(Log.LogId), property.Name);
		Assert.Equal(typeof(long), property.Type);
		Assert.True(property.IsSimplex(out var simplex));
		Assert.Equal(DataType.Int64, simplex.DataType);
		Assert.True(simplex.IsPrimaryKey);
		Assert.True(simplex.Sortable);
		Assert.False(simplex.Nullable);
		Assert.Equal("Id", simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Source), out property));
		Assert.Equal(nameof(Log.Source), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.AnsiString, simplex.DataType);
		Assert.Equal(50, simplex.Length);
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Message), out property));
		Assert.Equal(nameof(Log.Message), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.String, simplex.DataType);
		Assert.Equal(500, simplex.Length);
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Content), out property));
		Assert.Equal(nameof(Log.Content), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.String, simplex.DataType);
		Assert.False(simplex.IsPrimaryKey);
		Assert.True(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Timestamp), out property));
		Assert.Equal(nameof(Log.Timestamp), property.Name);
		Assert.Equal(typeof(DateTime), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.DateTime, simplex.DataType);
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.False(descriptor.Properties.Contains(nameof(Log.IgnoredField)));
	}

	[Model("Logs")]
	private struct Log
	{
		[ModelProperty("Id", IsPrimaryKey = true)]
		public long LogId;

		[ModelProperty(DbType.AnsiString, 50, false)]
		public string Source;
		[ModelProperty(DbType.String, 500, false)]
		public string Message;
		[ModelProperty(DbType.String, 0, true)]
		public string Content;

		[ModelProperty(Ignored = true)]
		public string IgnoredField;

		[ModelProperty(DbType.DateTime, false)]
		public DateTime Timestamp;
	}
}
