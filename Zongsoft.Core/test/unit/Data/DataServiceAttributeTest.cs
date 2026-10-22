using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataServiceAttributeTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void NearestGenericAttribute_AppliesAcrossConstructorFamilies(int constructor)
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var parent = new ProbeService(provider, DataServiceMutability.None);
		var service = constructor switch
		{
			0 => new DerivedService(provider),
			1 => new DerivedService(provider, DataServiceMutability.None),
			2 => new DerivedService(" Named ", provider),
			3 => new DerivedService(" Named ", provider, DataServiceMutability.None),
			4 => new DerivedService(parent),
			5 => new DerivedService(parent, DataServiceMutability.None),
			6 => new DerivedService(" Named ", parent),
			_ => new DerivedService(" Named ", parent, DataServiceMutability.None),
		};

		Assert.IsType<DataServiceAttribute<DerivedCriteria>>(service.Attribute);
		AssertBehavior(service, typeof(DerivedCriteria), "DerivedId", ["-DerivedId", "Name"], [Sorting.Descending("DerivedId"), Sorting.Ascending("Name")]);
		Assert.Same(provider, service.ServiceProvider);
		Assert.Same(constructor >= 4 ? parent : null, service.Service);
		Assert.Equal(constructor is 2 or 3 or 6 or 7 ? "Named" : "AttributeRecord", service.Name);
		Assert.Equal(constructor % 2 == 0, service.CanInsert);
		Assert.Equal(constructor % 2 == 0, service.CanUpdate);
		Assert.Equal(constructor % 2 == 0, service.CanUpsert);
		Assert.Equal(constructor is 4 or 6, service.CanDelete);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void UndeclaredTypes_UseNearestAncestor(bool derived)
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		ProbeService service = derived ? new DerivedLeaf(provider) : new BaseLeaf(provider);

		if(derived)
			AssertBehavior(service, typeof(DerivedCriteria), "DerivedId", ["-DerivedId", "Name"], [Sorting.Descending("DerivedId"), Sorting.Ascending("Name")]);
		else
			AssertBehavior(service, typeof(BaseCriteria), "BaseId", ["BaseId"], [Sorting.Ascending("BaseId")]);
	}

	[Fact]
	public void NoAttribute_UsesPrimaryKeyFallback()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var name = $"Attribute_{Guid.NewGuid():N}";
		var entity = new DataEntity(null, name);
		entity.Properties.Add(new DataEntitySimplexProperty("Id", DataType.Int32, false));
		entity.SetKey("Id");
		Mapping.Entities.Add(entity);

		try
		{
			var service = new ProbeService(name, provider);
			Assert.Null(service.Attribute);
			AssertCondition(service.ConvertCondition("23", out var singular), "Id", 23);
			Assert.True(singular);
			Assert.Equal([Sorting.Descending("Id")], service.Sortings());
		}
		finally { Mapping.Entities.Remove(name); }
	}

	[Fact]
	public void NonGenericAttribute_OverridesGenericAncestor()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var service = new NonGenericService(provider);

		Assert.IsType<DataServiceAttribute>(service.Attribute);
		AssertBehavior(service, typeof(PlainCriteria), "PlainId", ["-PlainId"], [Sorting.Descending("PlainId")]);
	}

	[Fact]
	public void DifferentAttributeKinds_OnSameTypeRemainAmbiguous()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		Assert.Throws<AmbiguousMatchException>(() => new AmbiguousService(provider));
	}

	private static void AssertBehavior(ProbeService service, Type criteria, string field, string[] declarations, Sorting[] sortings)
	{
		Assert.Equal(criteria, service.Attribute.Criteria);
		Assert.Equal(declarations, service.Attribute.Sortings);
		AssertCondition(service.ConvertCondition("Key:23", out var singular), field, 23);
		Assert.False(singular);
		Assert.Equal(sortings, service.Sortings());
	}

	private static void AssertCondition(ICondition criteria, string name, int expected)
	{
		var condition = Assert.IsType<Condition>(criteria);
		Assert.Equal(name, condition.Name);
		Assert.Equal(ConditionOperator.Equal, condition.Operator);
		Assert.Equal(expected, condition.Value is Operand.ConstantOperand<int> operand ? operand.Value : Assert.IsType<int>(condition.Value));
	}

	public abstract class BaseCriteria : CriteriaBase
	{
		[Condition("BaseId")]
		public abstract int? Key { get; set; }
	}

	public abstract class DerivedCriteria : CriteriaBase
	{
		[Condition("DerivedId")]
		public abstract int? Key { get; set; }
	}

	public abstract class PlainCriteria : CriteriaBase
	{
		[Condition("PlainId")]
		public abstract int? Key { get; set; }
	}

	private sealed class AttributeRecord;

	private class ProbeService : DataServiceBase<AttributeRecord>
	{
		public ProbeService(IServiceProvider provider) : base(provider) { }
		public ProbeService(IServiceProvider provider, DataServiceMutability? mutability) : base(provider, mutability) { }
		public ProbeService(string name, IServiceProvider provider) : base(name, provider) { }
		public ProbeService(string name, IServiceProvider provider, DataServiceMutability? mutability) : base(name, provider, mutability) { }
		public ProbeService(IDataService parent) : base(parent) { }
		public ProbeService(IDataService parent, DataServiceMutability? mutability) : base(parent, mutability) { }
		public ProbeService(string name, IDataService parent) : base(name, parent) { }
		public ProbeService(string name, IDataService parent, DataServiceMutability? mutability) : base(name, parent, mutability) { }
		public ICondition ConvertCondition(string key, out bool singular) => base.OnCondition(DataServiceMethod.Select(), key, new DataSelectOptions(), out singular);
		public Sorting[] Sortings() => base.GetSortings();
	}

	[DataService<BaseCriteria>("BaseId")]
	private class BaseService : ProbeService
	{
		public BaseService(IServiceProvider provider) : base(provider) { }
		public BaseService(IServiceProvider provider, DataServiceMutability? mutability) : base(provider, mutability) { }
		public BaseService(string name, IServiceProvider provider) : base(name, provider) { }
		public BaseService(string name, IServiceProvider provider, DataServiceMutability? mutability) : base(name, provider, mutability) { }
		public BaseService(IDataService parent) : base(parent) { }
		public BaseService(IDataService parent, DataServiceMutability? mutability) : base(parent, mutability) { }
		public BaseService(string name, IDataService parent) : base(name, parent) { }
		public BaseService(string name, IDataService parent, DataServiceMutability? mutability) : base(name, parent, mutability) { }
	}

	[DataService<DerivedCriteria>("-DerivedId", "Name")]
	private class DerivedService : BaseService
	{
		public DerivedService(IServiceProvider provider) : base(provider) { }
		public DerivedService(IServiceProvider provider, DataServiceMutability? mutability) : base(provider, mutability) { }
		public DerivedService(string name, IServiceProvider provider) : base(name, provider) { }
		public DerivedService(string name, IServiceProvider provider, DataServiceMutability? mutability) : base(name, provider, mutability) { }
		public DerivedService(IDataService parent) : base(parent) { }
		public DerivedService(IDataService parent, DataServiceMutability? mutability) : base(parent, mutability) { }
		public DerivedService(string name, IDataService parent) : base(name, parent) { }
		public DerivedService(string name, IDataService parent, DataServiceMutability? mutability) : base(name, parent, mutability) { }
	}

	private sealed class DerivedLeaf(IServiceProvider provider) : DerivedService(provider);
	private class BaseIntermediate(IServiceProvider provider) : BaseService(provider);
	private sealed class BaseLeaf(IServiceProvider provider) : BaseIntermediate(provider);

	[DataService(typeof(PlainCriteria), "-PlainId")]
	private sealed class NonGenericService(IServiceProvider provider) : DerivedService(provider);

	[DataService(typeof(BaseCriteria), "BaseId")]
	[DataService<DerivedCriteria>("-DerivedId")]
	private sealed class AmbiguousService(IServiceProvider provider) : ProbeService(provider);
}
