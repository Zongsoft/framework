using System;
using System.Data;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Data.Tests;

public class ModelDescriptorTest
{
	[Fact]
	public void TestWithoutMetadata()
	{
		var descriptor = Model.GetDescriptor<EmployeeBase>();

		Assert.NotNull(descriptor);
		Assert.Equal(nameof(EmployeeBase), descriptor.Name);
		Assert.Equal(typeof(EmployeeBase), descriptor.Type);
		Assert.NotNull(descriptor.Properties);
		Assert.NotEmpty(descriptor.Properties);

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.EmployeeId), out var property));
		Assert.Equal(nameof(EmployeeBase.EmployeeId), property.Name);
		Assert.Equal(typeof(int), property.Type);
		Assert.True(property.IsSimplex());

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.Name), out property));
		Assert.Equal(nameof(EmployeeBase.Name), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex());

		Assert.True(descriptor.Properties.TryGetValue(nameof(EmployeeBase.Gender), out property));
		Assert.Equal(nameof(EmployeeBase.Gender), property.Name);
		Assert.Equal(typeof(Gender?), property.Type);
		Assert.True(property.IsSimplex());
	}

	[Fact]
	public void TestWithMetadata()
	{
		var descriptor = Model.GetDescriptor<Log>();

		Assert.NotNull(descriptor);
		Assert.Equal("Logs", descriptor.Name);
		Assert.Equal(typeof(Log), descriptor.Type);
		Assert.NotNull(descriptor.Properties);
		Assert.NotEmpty(descriptor.Properties);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.LogId), out var property));
		Assert.Equal(nameof(Log.LogId), property.Name);
		Assert.Equal(typeof(long), property.Type);
		Assert.True(property.IsSimplex(out var simplex));
		Assert.True(simplex.IsPrimaryKey);
		Assert.True(simplex.Sortable);
		Assert.False(simplex.Nullable);
		Assert.Equal("Id", simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Source), out property));
		Assert.Equal(nameof(Log.Source), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Message), out property));
		Assert.Equal(nameof(Log.Message), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Content), out property));
		Assert.Equal(nameof(Log.Content), property.Name);
		Assert.Equal(typeof(string), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.False(simplex.IsPrimaryKey);
		Assert.True(simplex.Nullable);
		Assert.Null(simplex.Alias);

		Assert.True(descriptor.Properties.TryGetValue(nameof(Log.Timestamp), out property));
		Assert.Equal(nameof(Log.Timestamp), property.Name);
		Assert.Equal(typeof(DateTime), property.Type);
		Assert.True(property.IsSimplex(out simplex));
		Assert.False(simplex.IsPrimaryKey);
		Assert.False(simplex.Nullable);
		Assert.Null(simplex.Alias);
	}

	[Model("Logs")]
	internal struct Log
	{
		[ModelProperty("Id", IsPrimaryKey = true)]
		public long LogId;

		[ModelProperty(DbType.AnsiString, 50, false)]
		public string Source;
		[ModelProperty(DbType.String, 500, false)]
		public string Message;
		[ModelProperty(DbType.String, 0, true)]
		public string Content;

		[ModelProperty(DbType.DateTime, false)]
		public DateTime Timestamp;
	}
}
