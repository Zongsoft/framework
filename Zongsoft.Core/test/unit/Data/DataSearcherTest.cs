using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.Tests;

public sealed class DataSearcherTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ScalarAsync_UsesLegacyConditionNamesAndAwaitsProvider(bool exists)
	{
		using var cancellation = new CancellationTokenSource();
		var countCompletion = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var existsCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		IDataOptions options = exists ? new DataExistsOptions() : new DataAggregateOptions();
		var conditioner = new Conditioner();
		var service = DispatchProxy.Create<IDataService<int>, ServiceProxy>();
		var proxy = (ServiceProxy)service;
		proxy.Response = exists ? new ValueTask<bool>(existsCompletion.Task) : (object)new ValueTask<int?>(countCompletion.Task);
		var searcher = new DataSearcher<int>(service) { Conditioner = conditioner };

		var operation = ExecuteAsync();

		try
		{
			Assert.False(operation.IsCompleted);
			var resolution = Assert.Single(conditioner.Calls);
			Assert.Equal(exists ? "Exists" : "Count", resolution.Method);
			Assert.Equal("name:testing", resolution.Keyword);
			Assert.Same(options, resolution.Options);
			var call = Assert.Single(proxy.Calls);
			Assert.Equal(exists ? nameof(IDataService.ExistsAsync) : nameof(IDataService.AggregateAsync), call.Method.Name);
			Assert.Same(conditioner.Criteria, call.Arguments[exists ? 0 : 1]);
			Assert.Same(options, call.Arguments[^2]);
			Assert.Equal(cancellation.Token, Assert.IsType<CancellationToken>(call.Arguments[^1]));

			if(!exists)
			{
				var aggregate = Assert.IsType<DataAggregate>(call.Arguments[0]);
				Assert.Equal(DataAggregateFunction.Count, aggregate.Function);
				Assert.Null(aggregate.Name);
				Assert.False(aggregate.Distinct);
			}

			existsCompletion.SetResult(true);
			countCompletion.SetResult(37);
			var result = await operation.WaitAsync(_timeout);

			if(exists)
				Assert.True(Assert.IsType<bool>(result));
			else
				Assert.Equal(37, Assert.IsType<int>(result));
		}
		finally
		{
			existsCompletion.TrySetResult(false);
			countCompletion.TrySetResult(0);
			await operation.WaitAsync(_timeout);
		}

		async Task<object> ExecuteAsync()
		{
			if(exists)
				return await searcher.ExistsAsync("name:testing", options, cancellation.Token);

			return await searcher.CountAsync("name:testing", options, cancellation.Token);
		}
	}

	[Fact]
	public async Task SearchAsync_PreservesArgumentsAndDefersEnumeration()
	{
		using var cancellation = new CancellationTokenSource();
		using var enumeration = new CancellationTokenSource();
		var options = new DataSelectOptions();
		var paging = Paging.Page(3, 2);
		Sorting[] sortings = [Sorting.Descending("Id"), Sorting.Ascending("Name")];
		var source = new ControlledSequence([17, 23]);
		var conditioner = new Conditioner();
		var service = DispatchProxy.Create<IDataService<int>, ServiceProxy>();
		var proxy = (ServiceProxy)service;
		proxy.Response = source;
		IDataSearcher<int> searcher = new DataSearcher<int>(service) { Conditioner = conditioner };

		var result = searcher.SearchAsync("ordered", "Id,Name", paging, options, sortings, cancellation.Token);

		Assert.Same(source, result);
		var resolution = Assert.Single(conditioner.Calls);
		Assert.Equal("Search", resolution.Method);
		Assert.Equal("ordered", resolution.Keyword);
		Assert.Same(options, resolution.Options);
		var call = Assert.Single(proxy.Calls);
		Assert.Equal(nameof(IDataService.SelectAsync), call.Method.Name);
		Assert.Same(conditioner.Criteria, call.Arguments[0]);
		Assert.Equal("Id,Name", call.Arguments[1]);
		Assert.Same(paging, call.Arguments[2]);
		Assert.Same(options, call.Arguments[3]);
		Assert.Same(sortings, call.Arguments[4]);
		Assert.Equal(cancellation.Token, Assert.IsType<CancellationToken>(call.Arguments[5]));
		Assert.Equal(0, source.EnumeratorCount);
		Assert.Equal(0, source.MoveCount);

		var values = new List<int>();
		await foreach(var value in result.WithCancellation(enumeration.Token))
			values.Add(value);

		Assert.Equal([17, 23], values);
		Assert.Equal(enumeration.Token, source.Cancellation);
		Assert.Equal(1, source.EnumeratorCount);
		Assert.Equal(3, source.MoveCount);
		Assert.Equal(1, source.DisposalCount);
	}

	[Fact]
	public async Task SearchAsync_NonGenericBoxesValuesAndPreservesPaging()
	{
		var eventArgs = new PagingEventArgs("Search", Paging.Page(1, 2));
		var source = new ControlledSequence([17, 23]) { Page = eventArgs };
		var conditioner = new Conditioner();
		var service = DispatchProxy.Create<IDataService<int>, ServiceProxy>();
		var proxy = (ServiceProxy)service;
		proxy.Response = source;
		IDataSearcher searcher = new DataSearcher<int>(service) { Conditioner = conditioner };
		var options = new DataExistsOptions();

		var result = searcher.SearchAsync("boxed", options, Array.Empty<Sorting>(), CancellationToken.None);
		var pageable = Assert.IsAssignableFrom<IPageable>(result);

		Assert.Same(options, Assert.Single(conditioner.Calls).Options);
		Assert.Null(Assert.Single(proxy.Calls).Arguments[3]);
		Assert.Equal(0, source.EnumeratorCount);
		Assert.False(pageable.Suppressed);
		source.Suppressed = true;
		Assert.True(pageable.Suppressed);
		var notifications = new List<(object Sender, PagingEventArgs Args)>();
		pageable.Paginated += (sender, args, _) =>
		{
			notifications.Add((sender, args));
			return ValueTask.CompletedTask;
		};

		var values = new List<object>();
		await foreach(var value in result)
			values.Add(value);

		Assert.Collection(values,
			value => Assert.Equal(17, Assert.IsType<int>(value)),
			value => Assert.Equal(23, Assert.IsType<int>(value)));
		var notification = Assert.Single(notifications);
		Assert.Same(result, notification.Sender);
		Assert.Same(eventArgs, notification.Args);
		Assert.Equal(1, source.DisposalCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SearchAsync_NonGenericCancelsPendingMoveFromEitherToken(bool cancelMethod)
	{
		using var cancellation = new CancellationTokenSource();
		using var enumeration = new CancellationTokenSource();
		var moveCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var disposeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new ControlledSequence([17, 23])
		{
			MoveGate = moveCompletion.Task,
			DisposeGate = disposeCompletion.Task,
		};
		var service = DispatchProxy.Create<IDataService<int>, ServiceProxy>();
		((ServiceProxy)service).Response = source;
		IDataSearcher searcher = new DataSearcher<int>(service) { Conditioner = new Conditioner() };
		var result = searcher.SearchAsync("pending", Array.Empty<Sorting>(), cancellation.Token);
		var iterator = result.GetAsyncEnumerator(enumeration.Token);
		var move = iterator.MoveNextAsync().AsTask();

		try
		{
			await source.MoveStarted.Task.WaitAsync(_timeout);
			Assert.False(move.IsCompleted);
			Assert.Equal(1, source.MoveCount);
			Assert.Equal(cancellation.Token, Assert.IsType<CancellationToken>(Assert.Single(((ServiceProxy)service).Calls).Arguments[^1]));
			Assert.True(source.Cancellation.CanBeCanceled);

			if(cancelMethod)
				cancellation.Cancel();
			else
				enumeration.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await move.WaitAsync(_timeout));
			Assert.Equal(source.Cancellation, exception.CancellationToken);
			Assert.True(source.Cancellation.IsCancellationRequested);
			Assert.Equal(1, source.MoveCount);

			var disposal = iterator.DisposeAsync().AsTask();
			await source.DisposeStarted.Task.WaitAsync(_timeout);
			Assert.False(disposal.IsCompleted);
			disposeCompletion.SetResult();
			await disposal.WaitAsync(_timeout);
			Assert.Equal(1, source.DisposalCount);
		}
		finally
		{
			moveCompletion.TrySetResult();
			disposeCompletion.TrySetResult();
			await iterator.DisposeAsync().AsTask().WaitAsync(_timeout);
		}
	}

	private sealed class Conditioner : IDataSearcherConditioner
	{
		public ICondition Criteria { get; } = Condition.Equal("Active", true);
		public List<(string Method, string Keyword, IDataOptions Options)> Calls { get; } = [];

		public ICondition Resolve(string method, string keyword, IDataOptions options = null)
		{
			this.Calls.Add((method, keyword, options));
			return this.Criteria;
		}
	}

	public class ServiceProxy : DispatchProxy
	{
		public object Response { get; set; }
		public List<(MethodInfo Method, object[] Arguments)> Calls { get; } = [];

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			this.Calls.Add((targetMethod, args));
			return this.Response;
		}
	}

	private sealed class ControlledSequence(int[] items) : IAsyncEnumerable<int>, IPageable
	{
		public event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated;
		public bool Suppressed { get; set; }
		public PagingEventArgs Page { get; init; }
		public Task MoveGate { get; init; } = Task.CompletedTask;
		public Task DisposeGate { get; init; } = Task.CompletedTask;
		public TaskCompletionSource MoveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource DisposeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public CancellationToken Cancellation { get; private set; }
		public int EnumeratorCount { get; private set; }
		public int MoveCount { get; private set; }
		public int DisposalCount { get; private set; }

		public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			this.EnumeratorCount++;
			this.Cancellation = cancellation;
			return new Enumerator(this, items, cancellation);
		}

		private sealed class Enumerator(ControlledSequence owner, int[] items, CancellationToken cancellation) : IAsyncEnumerator<int>
		{
			private int _index = -1;
			public int Current => items[_index];

			public async ValueTask<bool> MoveNextAsync()
			{
				owner.MoveCount++;
				owner.MoveStarted.TrySetResult();
				await owner.MoveGate.WaitAsync(cancellation);
				cancellation.ThrowIfCancellationRequested();

				if(++_index >= items.Length)
					return false;

				if(_index == 0 && owner.Page != null && owner.Paginated != null)
				{
					foreach(Func<object, PagingEventArgs, CancellationToken, ValueTask> handler in owner.Paginated.GetInvocationList())
						await handler(owner, owner.Page, cancellation);
				}

				return true;
			}

			public async ValueTask DisposeAsync()
			{
				owner.DisposalCount++;
				owner.DisposeStarted.TrySetResult();
				await owner.DisposeGate;
			}
		}
	}
}
