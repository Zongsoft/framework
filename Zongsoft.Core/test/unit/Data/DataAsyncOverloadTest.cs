using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Data.Tests;

public sealed class DataAsyncOverloadTest
{
	[Fact]
	public async Task CountAsync_DefaultAndNamedOptionsReachAggregate()
	{
		var service = DispatchProxy.Create<IDataService, Recorder>();
		var recorder = (Recorder)service;
		using var cancellation = new CancellationTokenSource();
		var options = new DataAggregateOptions();

		Assert.Equal(11, await service.CountAsync());
		Assert.Equal(11, await service.CountAsync(options: options));
		Assert.Equal(11, await service.CountAsync(cancellation: cancellation.Token));
		Assert.Equal(11, await service.CountAsync(criteria: default(ICondition), options: null, cancellation: default));

		Assert.Equal(4, recorder.Calls.Count);
		Assert.All(recorder.Calls, call =>
		{
			Assert.Equal(nameof(IDataService.AggregateAsync), call.Method.Name);
			Assert.Single(call.Method.GetGenericArguments());
			Assert.Null(call.Arguments[1]);
		});
		Assert.Same(options, recorder.Calls[1].Arguments[2]);
		Assert.Equal(cancellation.Token, recorder.Calls[2].Arguments[^1]);
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(true, 0)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	public async Task CountAsync_ConditionsAreNotKeys(bool collection, int memberMode)
	{
		var service = DispatchProxy.Create<IDataService, Recorder>();
		var recorder = (Recorder)service;
		var condition = Condition.Equal("Status", 1);
		var conditions = new ConditionCollection(ConditionCombination.And) { condition };
		var result = collection ? memberMode switch
		{
			0 => await service.CountAsync(conditions),
			1 => await service.CountAsync("Id", conditions),
			_ => await service.CountAsync("Id", true, conditions),
		} : memberMode switch
		{
			0 => await service.CountAsync(condition),
			1 => await service.CountAsync("Id", condition),
			_ => await service.CountAsync("Id", true, condition),
		};

		Assert.Equal(11, result);
		var call = Assert.Single(recorder.Calls);
		Assert.Single(call.Method.GetGenericArguments());
		Assert.True(typeof(ICondition).IsAssignableFrom(call.Method.GetParameters()[1].ParameterType));
		Assert.Equal("criteria", call.Method.GetParameters()[1].Name);
		Assert.Same(collection ? conditions : (ICondition)condition, call.Arguments[1]);
		var aggregate = Assert.IsType<DataAggregate>(call.Arguments[0]);
		Assert.Equal(DataAggregateFunction.Count, aggregate.Function);
		Assert.Equal(memberMode == 0 ? null : "Id", aggregate.Name);
		Assert.Equal(memberMode == 2, aggregate.Distinct);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public async Task CountAsync_InferredCompositeKeysReachKeyOverloads(int count)
	{
		var service = DispatchProxy.Create<IDataService, Recorder>();
		var recorder = (Recorder)service;
		var result = count switch
		{
			1 => await service.CountAsync(1),
			2 => await service.CountAsync(1, 2),
			3 => await service.CountAsync(1, 2, 3),
			4 => await service.CountAsync(1, 2, 3, 4),
			_ => await service.CountAsync(1, 2, 3, 4, 5),
		};

		Assert.Equal(11, result);
		var call = Assert.Single(recorder.Calls);
		Assert.Equal(count + 1, call.Method.GetGenericArguments().Length);
		Assert.Equal(Enumerable.Range(1, count), call.Arguments.Skip(1).Take(count).Cast<int>());
	}

	[Fact]
	public void SelectAsync_OptionalExpandedArrayAndMethodGroupCallsCompile()
	{
		var accessor = DispatchProxy.Create<IDataAccess, Recorder>();
		var service = DispatchProxy.Create<IDataService, Recorder>();
		var typed = DispatchProxy.Create<IDataService<string>, Recorder>();
		Sorting[] sortings = [Sorting.Ascending("Id"), Sorting.Descending("Name")];
		var options = new DataSelectOptions();

		Assert.NotNull(accessor.SelectAsync<int>());
		Assert.NotNull(accessor.SelectAsync<int>(sortings: sortings));
		Assert.NotNull(accessor.SelectAsync<int>(options, sortings[0], sortings[1]));
		Assert.NotNull(accessor.SelectAsync<int>(options: null, sortings: sortings, cancellation: default));
		Assert.NotNull(accessor.SelectAsync<int>("Items", sortings: sortings));
		Func<DataSelectOptions, Sorting[], IAsyncEnumerable<int>> select = accessor.SelectAsync<int>;
		Assert.NotNull(select(options, sortings));

		Assert.NotNull(service.SelectAsync());
		Assert.NotNull(service.SelectAsync(sortings: sortings));
		Assert.NotNull(service.SelectAsync(options, sortings[0], sortings[1]));
		Assert.NotNull(service.SelectAsync(options: null, sortings: sortings, cancellation: default));
		Func<DataSelectOptions, Sorting[], IAsyncEnumerable<object>> selectService = service.SelectAsync;
		Assert.NotNull(selectService(options, sortings));

		Assert.NotNull(typed.SelectAsync());
		Assert.NotNull(typed.SelectAsync(sortings: sortings));
		Assert.NotNull(typed.SelectAsync(options, sortings[0], sortings[1]));
		Assert.NotNull(typed.SelectAsync(options: null, sortings: sortings, cancellation: default));
		Func<DataSelectOptions, Sorting[], IAsyncEnumerable<string>> selectTyped = typed.SelectAsync;
		Assert.NotNull(selectTyped(options, sortings));

		AssertSortingCalls((Recorder)accessor, sortings, 6);
		AssertSortingCalls((Recorder)service, sortings, 5);
		AssertSortingCalls((Recorder)typed, sortings, 5);

		var criteria = Condition.Equal("Status", 1);
		using var cancellation = new CancellationTokenSource();
		Assert.NotNull(accessor.SelectAsync<int>(criteria, "Id", cancellation.Token));
		Assert.NotNull(service.SelectAsync(criteria, "Id", cancellation.Token));
		Assert.NotNull(typed.SelectAsync(criteria, "Id", cancellation.Token));

		foreach(var recorder in new[] { (Recorder)accessor, (Recorder)service, (Recorder)typed })
		{
			var call = recorder.Calls[^1];
			Assert.Same(criteria, call.Arguments[0]);
			Assert.Equal("Id", call.Arguments[1]);
			Assert.Equal(cancellation.Token, call.Arguments[^1]);
			Assert.Equal(typeof(CancellationToken), call.Method.GetParameters()[^1].ParameterType);
		}
	}

	[Fact]
	public async Task AverageAsync_NamedCallbacksDoNotRequireDistinctOrOptions()
	{
		var accessor = DispatchProxy.Create<IDataAccess, Recorder>();
		var recorder = (Recorder)accessor;
		Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> before = (_, _) => ValueTask.FromResult(false);
		Func<DataAggregateContextBase, CancellationToken, ValueTask> after = (_, _) => ValueTask.CompletedTask;
		using var cancellation = new CancellationTokenSource();

		Assert.Equal(11, await accessor.AverageAsync<int>("Items", "Amount", aggregating: before, cancellation: cancellation.Token));
		Assert.Equal(11, await accessor.AverageAsync<string, int>("Amount", aggregated: after));

		Assert.Equal(2, recorder.Calls.Count);
		Assert.Same(before, recorder.Calls[0].Arguments[^3]);
		Assert.Equal(cancellation.Token, recorder.Calls[0].Arguments[^1]);
		Assert.Same(after, recorder.Calls[1].Arguments[^2]);
		Assert.All(recorder.Calls, call =>
		{
			var aggregate = Assert.Single(call.Arguments.OfType<DataAggregate>());
			Assert.Equal(DataAggregateFunction.Average, aggregate.Function);
			Assert.Equal("Amount", aggregate.Name);
			Assert.False(aggregate.Distinct);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExistsAsync_ConcreteServicePreservesConditions(bool collection)
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var service = new ConcreteService(provider);
		var condition = Condition.Equal("Status", 1);
		var conditions = new ConditionCollection(ConditionCombination.And) { condition };
		using var cancellation = new CancellationTokenSource();

		Assert.True(collection ? await service.ExistsAsync(conditions, cancellation.Token) : await service.ExistsAsync(condition, cancellation.Token));

		Assert.Same(collection ? conditions : (ICondition)condition, service.Criteria);
		Assert.Equal(cancellation.Token, service.Cancellation);
	}

	[Fact]
	public async Task GetAsync_ArrayOverloadForwardsCancellation()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var service = new ConcreteService(provider) { DataAccess = DispatchProxy.Create<IDataAccess, Recorder>() };
		using var cancellation = new CancellationTokenSource();
		Sorting[] sortings = [Sorting.Descending("Id")];

		Assert.Equal("item", await service.GetAsync("42", sortings, cancellation.Token));

		Assert.Equal(cancellation.Token, service.Cancellation);
		Assert.Equal(Condition.Equal("Id", "42"), service.Criteria);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetAsync_ExpandedSortingsPreserveStringKey(bool schema)
	{
		var service = DispatchProxy.Create<IDataService, Recorder>();
		Sorting[] sortings = [Sorting.Ascending("Id"), Sorting.Descending("Name")];

		Assert.Equal("item", schema ? await service.GetAsync("42", "Id", sortings[0], sortings[1]) : await service.GetAsync("42", sortings[0], sortings[1]));

		var call = Assert.Single(((Recorder)service).Calls);
		Assert.False(call.Method.IsGenericMethod);
		Assert.Equal("key", call.Method.GetParameters()[0].Name);
		Assert.Equal("42", call.Arguments[0]);
		Assert.Equal(sortings, Assert.IsType<Sorting[]>(call.Arguments[^1]));

		if(schema)
			Assert.Equal("Id", call.Arguments[1]);

		using var provider = new ServiceCollection().BuildServiceProvider();
		var concrete = new ConcreteService(provider) { DataAccess = DispatchProxy.Create<IDataAccess, Recorder>() };

		Assert.Equal("item", schema ? await concrete.GetAsync("42", "Id", sortings[0], sortings[1]) : await concrete.GetAsync("42", sortings[0], sortings[1]));
		Assert.Equal(Condition.Equal("Id", "42"), concrete.Criteria);
	}

	[Fact]
	public async Task IncreaseAsync_OmittedIntervalAndTokenUseExpectedOverloads()
	{
		var sequencer = DispatchProxy.Create<IDataSequencer, Recorder>();
		using var cancellation = new CancellationTokenSource();

		Assert.Equal(11, await sequencer.IncreaseAsync("Order", "Id"));
		Assert.Equal(11, await sequencer.IncreaseAsync("Order", "Id", CancellationToken.None));
		Assert.Equal(11, await sequencer.IncreaseAsync("Order", "Id", cancellation.Token));

		var calls = ((Recorder)sequencer).Calls;
		Assert.Equal(3, calls.Count);
		Assert.All(calls, call =>
		{
			Assert.Equal("Order", call.Arguments[0]);
			Assert.Equal("Id", call.Arguments[1]);
		});
		Assert.Equal("cancellation", calls[0].Method.GetParameters()[^1].Name);
		Assert.Equal(CancellationToken.None, calls[0].Arguments[^1]);
		Assert.Equal("cancellation", calls[1].Method.GetParameters()[^1].Name);
		Assert.Equal(CancellationToken.None, calls[1].Arguments[^1]);
		Assert.Equal(cancellation.Token, calls[2].Arguments[^1]);
	}

	[Fact]
	public async Task IncreaseAsync_ExplicitZeroIntervalIsPreserved()
	{
		var sequencer = DispatchProxy.Create<IDataSequencer, Recorder>();
		using var cancellation = new CancellationTokenSource();

		Assert.Equal(11, await sequencer.IncreaseAsync("Items", "Id", 0));
		Assert.Equal(11, await sequencer.IncreaseAsync("Items", "Id", 0, cancellation.Token));
		Assert.Equal(11, await sequencer.IncreaseAsync("Items", "Id", default(int)));

		var calls = ((Recorder)sequencer).Calls;
		Assert.Equal(3, calls.Count);
		Assert.All(calls, call =>
		{
			Assert.Equal("interval", call.Method.GetParameters()[2].Name);
			Assert.Equal(0, call.Arguments[2]);
		});
		Assert.Equal(cancellation.Token, calls[1].Arguments[^1]);
	}

	private static void AssertSortingCalls(Recorder recorder, Sorting[] expected, int count)
	{
		Assert.Equal(count, recorder.Calls.Count);
		foreach(var call in recorder.Calls.Skip(1))
		{
			var sortings = Assert.Single(call.Arguments.OfType<Sorting[]>());
			Assert.Equal(expected, sortings);
		}
	}

	public class Recorder : DispatchProxy
	{
		public List<(MethodInfo Method, object[] Arguments)> Calls { get; } = [];
		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			this.Calls.Add((targetMethod, args));
			if(targetMethod.ReturnType == typeof(ISchemaParser))
				return new EmptySchemaParser();
			if(targetMethod.ReturnType == typeof(ValueTask<object>))
				return new ValueTask<object>("item");
			if(targetMethod.ReturnType == typeof(ValueTask<long>))
				return new ValueTask<long>(11);
			if(targetMethod.ReturnType == typeof(ValueTask<int?>))
				return new ValueTask<int?>(11);
			if(targetMethod.ReturnType == typeof(IAsyncEnumerable<int>))
				return new EmptySequence<int>();
			if(targetMethod.ReturnType == typeof(IAsyncEnumerable<object>))
				return new EmptySequence<object>();
			if(targetMethod.ReturnType == typeof(IAsyncEnumerable<string>))
				return new EmptySequence<string>();
			throw new NotSupportedException();
		}
	}

	private sealed class EmptySchemaParser : ISchemaParser
	{
		public ISchema Parse(string name, string expression, Type modelType = null) => null;
	}

	private sealed class EmptySequence<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
	{
		public T Current => default;
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;
		public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class ConcreteService(IServiceProvider provider) : DataServiceBase<string>(provider)
	{
		public ICondition Criteria { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		protected override ValueTask AuthorizeAsync(DataServiceMethod method, IDataOptions options, CancellationToken cancellation) => ValueTask.CompletedTask;
		protected override ValueTask<ICondition> OnValidateAsync(DataServiceMethod method, ICondition criteria, IDataOptions options, CancellationToken cancellation) => ValueTask.FromResult(criteria);
		protected override ICondition OnCondition(DataServiceMethod method, string key, IDataOptions options, out bool singular)
		{
			singular = true;
			return Condition.Equal("Id", key);
		}
		protected override ValueTask<string> OnGetAsync(ICondition criteria, ISchema schema, DataSelectOptions options, CancellationToken cancellation)
		{
			this.Criteria = criteria;
			this.Cancellation = cancellation;
			return ValueTask.FromResult("item");
		}
		protected override ValueTask<bool> OnExistsAsync(ICondition criteria, DataExistsOptions options, CancellationToken cancellation)
		{
			this.Criteria = criteria;
			this.Cancellation = cancellation;
			return ValueTask.FromResult(true);
		}
	}
}
