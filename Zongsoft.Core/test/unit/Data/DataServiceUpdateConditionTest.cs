using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataServiceUpdateConditionTest
{
	public static TheoryData<string, bool> Cases
	{
		get
		{
			var cases = new TheoryData<string, bool>();
			foreach(var name in new[] { "Equal", "In", "Range", "EqualRange", "Field", "Expression", "EqualCollection", "Conflict", "Repeated", "NotEqual", "GreaterThan", "And", "NestedAnd", "InThenEqual", "OrThenEqual", "Or", "NestedOr" })
			{
				cases.Add(name, false);
				cases.Add(name, true);
			}

			return cases;
		}
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public async Task UpdateAndDeletePreserveCriteriaAndOnlyCopyUnambiguousEqualityKeys(string name, bool invariant)
	{
		var entityName = $"UpdateCondition_{Guid.NewGuid():N}";
		var entity = new DataEntity(null, entityName);
		entity.Properties.Simplex(nameof(UpdateModel.Id), DataType.Int32, false);
		entity.Properties.Simplex(nameof(UpdateModel.Name), DataType.String, true);
		entity.SetKey(nameof(UpdateModel.Id));
		Mapping.Entities.Add(entity);

		try
		{
			using var provider = new ServiceCollection().BuildServiceProvider();
			await using var accessor = new DataAsyncPipelineTest.TestDataAccess();
			var service = new UpdateService(provider, entityName, accessor);
			var model = Model.Build<UpdateModel>(data => data.Name = "Updated");
			ICondition criteria = name switch
			{
				"Equal" => Condition.Equal("id", 17),
				"In" => Condition.In(nameof(UpdateModel.Id), new[] { 17, 23 }),
				"Range" => Condition.Between(nameof(UpdateModel.Id), 17, 23),
				"EqualRange" => Condition.Equal(nameof(UpdateModel.Id), Range.Create(17, 23)),
				"Field" => Condition.Equal(nameof(UpdateModel.Id), Operand.Field("OtherId")),
				"Expression" => Condition.Equal(nameof(UpdateModel.Id), Operand.Field("OtherId") + 1),
				"EqualCollection" => new Condition(nameof(UpdateModel.Id), new[] { 17, 23 }),
				"Conflict" => Condition.Equal(nameof(UpdateModel.Id), 17) & Condition.Equal(nameof(UpdateModel.Id), 23),
				"Repeated" => Condition.Equal(nameof(UpdateModel.Id), 17) & new Condition(nameof(UpdateModel.Id), 17),
				"NotEqual" => Condition.NotEqual(nameof(UpdateModel.Id), 17),
				"GreaterThan" => Condition.GreaterThan(nameof(UpdateModel.Id), 17),
				"And" => Condition.Equal(nameof(UpdateModel.Name), "Before") & (Condition.Equal(nameof(UpdateModel.Id), 17) & Condition.NotEqual(nameof(UpdateModel.Name), "Other")),
				"NestedAnd" => new ConditionCollection(ConditionCombination.And)
				{
					new ConditionCollection(ConditionCombination.And) { Condition.Equal(nameof(UpdateModel.Id), 17) },
				},
				"InThenEqual" => Condition.In(nameof(UpdateModel.Id), new[] { 17, 23 }) & Condition.Equal(nameof(UpdateModel.Id), 17),
				"OrThenEqual" => new ConditionCollection(ConditionCombination.And)
				{
					Condition.Equal(nameof(UpdateModel.Id), 17) | Condition.Equal(nameof(UpdateModel.Id), 23),
					Condition.Equal(nameof(UpdateModel.Id), 17),
				},
				"Or" => Condition.Equal(nameof(UpdateModel.Id), 17) | Condition.Equal(nameof(UpdateModel.Id), 23),
				_ => Condition.Equal(nameof(UpdateModel.Name), "Before") & (Condition.Equal(nameof(UpdateModel.Id), 17) | Condition.Equal(nameof(UpdateModel.Id), 23)),
			};
			var cancellation = TestContext.Current.CancellationToken;
			var result = invariant ?
				await service.UpdateInvariantAsync(model, criteria, cancellation) :
				await service.UpdateAsync(model, criteria, cancellation);

			Assert.Equal(1, result);
			Assert.Equal(1, service.Updates);
			Assert.Same(criteria, service.Criteria);
			Assert.Same(model, service.Data.AsModel());
			Assert.Equal("Updated", model.Name);
			Assert.Equal(cancellation, service.Cancellation);
			var hasKey = name is "Equal" or "And" or "NestedAnd" or "InThenEqual" or "OrThenEqual" or "Repeated";
			Assert.Equal(hasKey, service.Data.ContainsKey(nameof(UpdateModel.Id)));
			Assert.Equal(hasKey ? 17 : 0, model.Id);

			var options = DataDeleteOptions.Return(nameof(UpdateModel.Name)).Build();
			var returning = options.Returning;
			var deleted = invariant ? criteria switch
			{
				Condition condition => await service.DeleteAsync(condition, options, cancellation),
				ConditionCollection conditions => await service.DeleteAsync(conditions, options, cancellation),
				_ => await service.DeleteAsync(criteria, options, cancellation),
			} : await service.DeleteAsync(criteria, options, cancellation);
			Assert.Equal(2, deleted);
			Assert.Same(criteria, service.Criteria);
			Assert.Same(options, service.DeleteOptions);
			Assert.Same(returning, options.Returning);
			Assert.Equal(nameof(UpdateModel.Name), Assert.Single(returning.Columns).Name);
			Assert.Equal(cancellation, service.Cancellation);
		}
		finally
		{
			Mapping.Entities.Remove(entityName);
		}
	}

	[Theory]
	[InlineData("17", false)]
	[InlineData("17,23", false)]
	[InlineData("17", true)]
	[InlineData("17,23", true)]
	public async Task KeyRoutesPreserveSingleAndMultipleSelection(string key, bool deletion)
	{
		var entityName = $"KeyCondition_{Guid.NewGuid():N}";
		var entity = new DataEntity(null, entityName);
		entity.Properties.Simplex(nameof(UpdateModel.Id), DataType.Int32, false);
		entity.Properties.Simplex(nameof(UpdateModel.Name), DataType.String, true);
		entity.SetKey(nameof(UpdateModel.Id));
		Mapping.Entities.Add(entity);

		try
		{
			using var provider = new ServiceCollection().BuildServiceProvider();
			await using var accessor = new DataAsyncPipelineTest.TestDataAccess();
			var service = new UpdateService(provider, entityName, accessor);
			var model = Model.Build<UpdateModel>(data => data.Name = "Updated");
			var cancellation = TestContext.Current.CancellationToken;

			if(deletion)
				Assert.Equal(2, await service.DeleteAsync(key, cancellation));
			else
				Assert.Equal(1, await service.UpdateAsync(key, model, cancellation));

			var condition = Assert.IsType<Condition>(service.Criteria);
			Assert.Equal(key.Contains(',') ? ConditionOperator.In : ConditionOperator.Equal, condition.Operator);

			if(key.Contains(','))
			{
				Assert.Equal(new[] { "17", "23" }, Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(condition.Value));
				Assert.Equal(0, model.Id);
			}
			else
			{
				Assert.True(condition.TryGetValue<int>("Id", out var id));
				Assert.Equal(17, id);
				Assert.Equal(deletion ? 0 : 17, model.Id);
			}
		}
		finally
		{
			Mapping.Entities.Remove(entityName);
		}
	}

	public abstract class UpdateModel
	{
		public abstract int Id { get; set; }
		public abstract string Name { get; set; }
	}

	private sealed class UpdateService : DataServiceBase<UpdateModel>
	{
		public UpdateService(IServiceProvider provider, string name, IDataAccess accessor) : base(provider, DataServiceMutability.All)
		{
			this.Name = name;
			this.DataAccess = accessor;
		}

		public int Updates { get; private set; }
		public ICondition Criteria { get; private set; }
		public IDataDictionary<UpdateModel> Data { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public DataDeleteOptions DeleteOptions { get; private set; }
		public ValueTask<int> UpdateInvariantAsync(object data, ICondition criteria, CancellationToken cancellation) => this.UpdateAsync(data, criteria, (ISchema)null, cancellation);
		protected override ValueTask AuthorizeAsync(DataServiceMethod method, IDataOptions options, CancellationToken cancellation) => ValueTask.CompletedTask;
		protected override ValueTask<int> OnUpdateAsync(IDataDictionary<UpdateModel> data, ICondition criteria, ISchema schema, DataUpdateOptions options, CancellationToken cancellation)
		{
			this.Updates++;
			this.Criteria = criteria;
			this.Data = data;
			this.Cancellation = cancellation;
			return ValueTask.FromResult(1);
		}
		protected override ValueTask<int> OnDeleteAsync(ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation)
		{
			this.Criteria = criteria;
			this.DeleteOptions = options;
			this.Cancellation = cancellation;
			return ValueTask.FromResult(2);
		}
	}
}
