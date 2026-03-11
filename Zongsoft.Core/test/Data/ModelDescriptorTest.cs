using System;
using System.Data;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Tests;
using Zongsoft.Serialization;
using Zongsoft.Data.Metadata;

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

	[Fact]
	public void TestToEntityForLog()
	{
		var model = Model.GetDescriptor<Log>();
		Assert.NotNull(model);

		var entity = model.ToEntity();
		Assert.NotNull(entity);
		Assert.Equal(model.QualifiedName, entity.QualifiedName, true);
		Assert.NotNull(entity.Key);
		Assert.Single(entity.Key);
		Assert.True(entity.Key[0].IsPrimaryKey);
		Assert.True(entity.Key[0].Sortable);
		Assert.False(entity.Key[0].Nullable);
		Assert.NotNull(entity.Key[0].Sequence);
		Assert.Equal(DataType.Int64, entity.Key[0].Type);

		Assert.True(entity.Properties.TryGetValue(nameof(Log.Timestamp), out var property));
		Assert.NotNull(property);
		Assert.True(property.Immutable);
		Assert.True(property.IsSimplex);
		Assert.False(property.IsComplex);
		Assert.IsType<DataEntitySimplexProperty>(property);
		var simplex = (IDataEntitySimplexProperty)property;
		Assert.True(simplex.Sortable);
		Assert.NotNull(simplex.DefaultValue);
		Assert.IsType<DateTime>(simplex.DefaultValue);
	}

	[Fact]
	public void TestToEntityForOrder()
	{
		var model = Model.GetDescriptor<Order>();
		Assert.NotNull(model);

		var entity = model.ToEntity();
		Assert.NotNull(entity);
		Assert.Equal(model.QualifiedName, entity.QualifiedName, true);
		Assert.NotNull(entity.Key);
		Assert.Single(entity.Key);
		Assert.True(entity.Key[0].IsPrimaryKey);
		Assert.True(entity.Key[0].Sortable);
		Assert.False(entity.Key[0].Nullable);
		Assert.NotNull(entity.Key[0].Sequence);
		Assert.Equal(DataType.UInt64, entity.Key[0].Type);
		Assert.Equal(nameof(Order.OrderId), entity.Key[0].Name);

		Assert.True(entity.Properties.TryGetValue(nameof(Order.CreatedTime), out var property));
		Assert.NotNull(property);
		Assert.True(property.Immutable);
		Assert.True(property.IsSimplex);
		Assert.False(property.IsComplex);
		Assert.IsType<DataEntitySimplexProperty>(property);
		var simplex = (IDataEntitySimplexProperty)property;
		Assert.True(simplex.Sortable);
		Assert.NotNull(simplex.DefaultValue);
		Assert.IsType<DateTime>(simplex.DefaultValue);

		Assert.True(entity.Properties.TryGetValue(nameof(Order.Remark), out property));
		Assert.NotNull(property);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.String, simplex.Type);
		Assert.Equal(500, simplex.Length);
		Assert.True(simplex.Nullable);
		Assert.False(simplex.Sortable);
		Assert.False(simplex.Immutable);

		Assert.True(entity.Properties.TryGetValue(nameof(Order.Creator), out property));
		Assert.NotNull(property);
		Assert.True(property.IsComplex(out var complex));
		Assert.Equal(nameof(Order.Creator), complex.Name);
		Assert.Equal(nameof(Employee), complex.Port);
		Assert.Equal(DataEntityComplexPropertyBehaviors.None, complex.Behaviors);
		Assert.Equal(DataAssociationMultiplicity.ZeroOrOne, complex.Multiplicity);
		Assert.Null(complex.Constraints);
		Assert.NotNull(complex.Links);
		Assert.Single(complex.Links);
		Assert.Equal(nameof(Order.CreatorId), complex.Links[0].Anchor);
		Assert.Equal(nameof(Employee.EmployeeId), complex.Links[0].Foreign);

		Assert.True(entity.Properties.TryGetValue(nameof(Order.Details), out property));
		Assert.NotNull(property);
		Assert.True(property.IsComplex(out complex));
		Assert.Equal(nameof(Order.Details), complex.Name);
		Assert.Equal(nameof(OrderDetail), complex.Port);
		Assert.Equal(DataEntityComplexPropertyBehaviors.None, complex.Behaviors);
		Assert.Equal(DataAssociationMultiplicity.Many, complex.Multiplicity);
		Assert.Null(complex.Constraints);
		Assert.NotNull(complex.Links);
		Assert.Single(complex.Links);
		Assert.Equal(nameof(Order.OrderId), complex.Links[0].Anchor);
		Assert.Equal(nameof(OrderDetail.OrderId), complex.Links[0].Foreign);
	}

	[Fact]
	public void TestToEntityForOrderDetail()
	{
		var model = Model.GetDescriptor<OrderDetail>();
		Assert.NotNull(model);

		var entity = model.ToEntity();
		Assert.NotNull(entity);
		Assert.Equal(model.QualifiedName, entity.QualifiedName, true);
		Assert.NotNull(entity.Key);
		Assert.Equal(2, entity.Key.Length);

		Assert.True(entity.Key[0].IsPrimaryKey);
		Assert.True(entity.Key[0].Sortable);
		Assert.False(entity.Key[0].Nullable);
		Assert.Null(entity.Key[0].Sequence);
		Assert.Equal(DataType.UInt64, entity.Key[0].Type);
		Assert.Equal(nameof(OrderDetail.OrderId), entity.Key[0].Name);

		Assert.True(entity.Key[1].IsPrimaryKey);
		Assert.True(entity.Key[1].Sortable);
		Assert.False(entity.Key[1].Nullable);
		Assert.Null(entity.Key[1].Sequence);
		Assert.Equal(DataType.UInt64, entity.Key[1].Type);
		Assert.Equal(nameof(OrderDetail.GoodsId), entity.Key[1].Name);

		Assert.True(entity.Properties.TryGetValue(nameof(OrderDetail.Amount), out var property));
		Assert.NotNull(property);
		Assert.True(property.IsSimplex(out var simplex));
		Assert.Equal(DataType.Decimal, simplex.Type);
		Assert.False(simplex.Nullable);
		Assert.False(simplex.Sortable);
		Assert.False(simplex.Immutable);

		Assert.True(entity.Properties.TryGetValue(nameof(OrderDetail.Quantity), out property));
		Assert.NotNull(property);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.Double, simplex.Type);
		Assert.Equal(12, simplex.Precision);
		Assert.Equal(4, simplex.Scale);
		Assert.False(simplex.Nullable);
		Assert.False(simplex.Sortable);
		Assert.False(simplex.Immutable);

		Assert.True(entity.Properties.TryGetValue(nameof(OrderDetail.Remark), out property));
		Assert.NotNull(property);
		Assert.True(property.IsSimplex(out simplex));
		Assert.Equal(DataType.String, simplex.Type);
		Assert.Equal(500, simplex.Length);
		Assert.True(simplex.Nullable);
		Assert.False(simplex.Sortable);
		Assert.False(simplex.Immutable);

		Assert.True(entity.Properties.TryGetValue(nameof(OrderDetail.Order), out property));
		Assert.NotNull(property);
		Assert.True(property.IsComplex(out var complex));
		Assert.Equal(nameof(OrderDetail.Order), complex.Name);
		Assert.Equal(nameof(Order), complex.Port);
		Assert.Equal(DataEntityComplexPropertyBehaviors.Principal, complex.Behaviors);
		Assert.Equal(DataAssociationMultiplicity.One, complex.Multiplicity);
		Assert.Null(complex.Constraints);
		Assert.NotNull(complex.Links);
		Assert.Single(complex.Links);
		Assert.Equal(nameof(OrderDetail.OrderId), complex.Links[0].Anchor);
		Assert.Equal(nameof(Order.OrderId), complex.Links[0].Foreign);
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
		Assert.Equal("#", simplex.Sequence.Name);
		Assert.True(simplex.Sequence.IsExternal);
		Assert.False(simplex.Sequence.IsBuiltin);
		Assert.False(simplex.Sequence.HasReferences);

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
		Assert.True(simplex.Immutable);
		Assert.True(simplex.Sortable);
		Assert.Null(simplex.Alias);
		Assert.NotNull(simplex.DefaultValue);
		Assert.IsType<DataPropertyFunction>(simplex.DefaultValue);
		Assert.Equal("now()", simplex.DefaultValue.ToString(), true);

		Assert.False(descriptor.Properties.Contains(nameof(Log.IgnoredField)));
	}

	[Model("Logs")]
	private struct Log
	{
		[ModelProperty("Id", IsPrimaryKey = true, Sequence = "#")]
		public long LogId { get; set; }

		[ModelProperty(DbType.AnsiString, 50, false)]
		public string Source { get; set; }
		[ModelProperty(DbType.String, 500, false)]
		public string Message { get; set; }
		[ModelProperty(DbType.String, 0, true)]
		public string Content { get; set; }

		[ModelProperty(Ignored = true)]
		public string IgnoredField { get; set; }

		[ModelProperty(DbType.DateTime, false, Sortable = true, Immutable = true, DefaultValue = "now()")]
		public DateTime Timestamp { get; set; }
	}

	internal abstract class Order
	{
		[ModelProperty(IsPrimaryKey = true, Sequence = "#")]
		public abstract ulong OrderId { get; set; }
		public abstract decimal Amount { get; set; }
		public abstract decimal Discount { get; set; }
		public abstract int CreatorId { get; set; }

		[ModelProperty(nameof(Employee), DataAssociationMultiplicity.ZeroOrOne,
			[$"{nameof(CreatorId)}->{nameof(Employee.EmployeeId)}"])]
		public abstract Employee Creator { get; set; }

		[ModelProperty(Immutable = true, Sortable = true, DefaultValue = "now()")]
		public abstract DateTime CreatedTime { get; set; }

		[ModelProperty(Length = 500)]
		public abstract string Remark { get; set; }

		[ModelProperty(nameof(OrderDetail), DataAssociationMultiplicity.Many, [nameof(OrderId)])]
		public abstract ICollection<OrderDetail> Details { get; set; }
	}

	internal class OrderDetail
	{
		[ModelProperty(IsPrimaryKey = true)]
		public ulong OrderId { get; set; }
		[ModelProperty(IsPrimaryKey = true)]
		public ulong GoodsId { get; set; }

		public decimal Amount { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Discount { get; set; }

		[ModelProperty(Precision = 12, Scale = 4)]
		public double Quantity { get; set; }

		[ModelProperty(Length = 500)]
		public string Remark { get; set; }

		[ModelProperty(
			nameof(Order),
			DataAssociationMultiplicity.One,
			DataEntityComplexPropertyBehaviors.Principal,
			[nameof(OrderId)])]
		public Order Order { get; set; }
	}
}
