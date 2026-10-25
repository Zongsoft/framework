using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using static Zongsoft.Data.Tests.DataAsyncPipelineTest;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataServiceAsyncPipelineTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImportAwaitsServiceFiltersAndSupportsShortCircuit(bool skip)
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		await using var accessor = new TestDataAccess();
		var service = new TestService(provider, model.Name, accessor);
		var options = new DataImportOptions();
		var source = new Sequence<int>([17, 23]);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = new List<string>();
		DataImportContextBase operation = null;
		service.Filters.Add(new ServiceFilter
		{
			Before = async (context, token) =>
			{
				Assert.Equal("Import", context.Method.Name);
				Assert.Equal(TestContext.Current.CancellationToken, token);
				operation = Assert.IsAssignableFrom<DataImportContextBase>(context.AccessContext);
				Assert.Same(options, operation.Options);
				calls.Add("before");
				await release.Task.WaitAsync(token);
				context.Result = 9;
				return skip;
			},
			After = (context, token) =>
			{
				Assert.Same(operation, context.AccessContext);
				Assert.Equal(2, context.Result);
				calls.Add("after");
				return ValueTask.FromException(new InvalidOperationException("Handler failure"));
			},
		});
		accessor.Import = (context, token) =>
		{
			Assert.Same(operation, context);
			calls.Add("write");
			context.Count = 2;
			return ValueTask.CompletedTask;
		};
		var pending = service.ImportItemsAsync(source.Async, options, TestContext.Current.CancellationToken).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);
			Assert.Equal(["before"], calls);
		}
		finally { release.TrySetResult(); }

		if(skip)
		{
			Assert.Equal(9, await pending);
			Assert.Equal(["before"], calls);
		}
		else
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
			Assert.Equal(["before", "write", "after"], calls);
		}
	}
	[Fact]
	public async Task SelectAsync_AwaitsAuthorizationValidationAndNotifications()
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		var service = new TestService(provider, model.Name, accessor);
		var options = new DataSelectOptions();
		var original = Condition.Equal("Original", 1);
		var replacement = Condition.Equal("Validated", 2);
		var source = new Sequence<int>([17, 23]);
		var stages = new Stages("authorize", "validate", "service-before", "service-filter-before", "access-before", "access-filter-before", "provider", "access-filter-after", "access-after", "service-after", "service-filter-after");
		service.Authorizing = (method, settings, token) =>
		{
			Assert.Equal("Select", method.Name);
			Assert.Same(options, settings);
			return stages.EnterAsync("authorize", token);
		};
		service.Validating = async (method, criteria, settings, token) =>
		{
			Assert.Equal("Select", method.Name);
			Assert.Same(original, criteria);
			Assert.Same(options, settings);
			await stages.EnterAsync("validate", token);
			return replacement;
		};
		service.Selecting += (_, args, token) => StepAsync("service-before", args.Context, token);
		service.Selected += (_, args, token) => StepAsync("service-after", args.Context, token);
		service.Filters.Add(new ServiceFilter
		{
			Before = async (context, token) => { await StepAsync("service-filter-before", (DataSelectContextBase)context.AccessContext, token); return false; },
			After = (context, token) => StepAsync("service-filter-after", (DataSelectContextBase)context.AccessContext, token),
		});
		accessor.Selecting += (_, args, token) => StepAsync("access-before", args.Context, token);
		accessor.Selected += (_, args, token) => StepAsync("access-after", args.Context, token);
		accessor.Filters.Add(new AccessFilter<DataSelectContextBase>
		{
			Before = (context, token) => StepAsync("access-filter-before", context, token),
			After = (context, token) => StepAsync("access-filter-after", context, token),
		});
		accessor.Select = async (context, token) =>
		{
			await StepAsync("provider", context, token);
			context.Result = source.Async;
		};
		var result = service.SelectAsync(original, "Id", null, options, [Sorting.Ascending("Id")], cancellation.Token);
		var operation = CollectAsync(result);

		try
		{
			await stages.AdvanceAsync(operation, cancellation.Token);
			Assert.Equal([17, 23], await operation.WaitAsync(Stages.Timeout));
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(1, accessor.Selection.DisposalCount);
		}
		finally
		{
			stages.ReleaseAll();
			await operation.WaitAsync(Stages.Timeout);
		}

		ValueTask StepAsync(string name, DataSelectContextBase context, CancellationToken token)
		{
			Assert.Same(accessor.Selection, context);
			Assert.Same(replacement, context.Criteria);
			Assert.Same(options, context.Options);
			Assert.Equal(0, source.Enumerations);
			return stages.EnterAsync(name, token);
		}
	}

	[Fact]
	public async Task ServiceFilteringAsync_True_SkipsProviderAndPostNotifications()
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		var service = new TestService(provider, model.Name, accessor);
		var source = new Sequence<int>([17, 23]);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = new List<string>();
		service.Selecting += (_, _, _) => { calls.Add("service-before"); return ValueTask.CompletedTask; };
		service.Selected += (_, _, _) => throw new InvalidOperationException("Short circuit must skip the post event.");
		accessor.Selecting += (_, _, _) => throw new InvalidOperationException("Short circuit must skip access events.");
		service.Filters.Add(new ServiceFilter
		{
			Before = async (context, token) =>
			{
				calls.Add("filter-before");
				Assert.Equal(cancellation.Token, token);
				await release.Task.WaitAsync(token);
				context.Result = source.Async;
				return true;
			},
			After = (_, _) => throw new InvalidOperationException("Short circuit must skip the post filter."),
		});
		var operation = CollectAsync(service.SelectAsync(Condition.Equal("Id", 1), "Id", null, null, [Sorting.Ascending("Id")], cancellation.Token));

		try
		{
			Assert.False(operation.IsCompleted);
			Assert.Equal(["service-before", "filter-before"], calls);
			Assert.Equal(0, source.Enumerations);
			release.SetResult();
			Assert.Equal([17, 23], await operation.WaitAsync(Stages.Timeout));
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(1, accessor.Selection.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await operation.WaitAsync(Stages.Timeout);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetAsync_GettedSnapshot_DoesNotRepeatQueryOrEnumeration(bool empty)
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		var service = new TestService(provider, model.Name, accessor);
		var source = new Sequence<int>(empty ? [] : [17, 23]);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var queries = 0;
		var filters = 0;
		var events = 0;
		DataGettedEventArgs<int> snapshot = null;
		accessor.Select = (context, _) =>
		{
			queries++;
			context.Result = source.Async;
			return ValueTask.CompletedTask;
		};
		service.Filters.Add(new ServiceFilter
		{
			After = (_, _) =>
			{
				filters++;
				Assert.Equal(0, source.Moves);
				return ValueTask.CompletedTask;
			},
		});
		service.Getted += async (_, args, token) =>
		{
			events++;
			snapshot = args;
			Assert.Equal(cancellation.Token, token);
			Assert.Equal(empty ? 0 : 17, args.Result);
			Assert.Equal(empty ? 0 : 17, args.Result);
			Assert.Equal(1, source.Moves);
			entered.SetResult();
			await release.Task.WaitAsync(token);
		};
		var operation = service.GetAsync("17", cancellation.Token).AsTask();

		try
		{
			await entered.Task.WaitAsync(Stages.Timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(1, queries);
			Assert.Equal(1, filters);
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(empty ? 1 : 0, source.Disposals);
			release.SetResult();
			Assert.Equal(empty ? 0 : 17, Assert.IsType<int>(await operation.WaitAsync(Stages.Timeout)));
			Assert.Equal(empty ? 0 : 17, snapshot.Result);
			Assert.Equal(1, source.Moves);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(1, events);
			Assert.Equal(1, accessor.Selection.DisposalCount);
		}
		finally
		{
			release.TrySetResult();
			await operation.WaitAsync(Stages.Timeout);
		}
	}

	[Fact]
	public async Task GetAsync_FilterReplacement_PrecedesFirstRead()
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		await using var accessor = new TestDataAccess();
		var service = new TestService(provider, model.Name, accessor);
		var original = new Sequence<int>([17, 23]);
		var replacement = new Sequence<int>([42, 99]);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var queries = 0;
		var filters = 0;
		var events = 0;
		accessor.Select = (context, _) =>
		{
			queries++;
			context.Result = original.Async;
			return ValueTask.CompletedTask;
		};
		service.Filters.Add(new ServiceFilter
		{
			After = async (context, _) =>
			{
				filters++;
				await release.Task;
				context.Result = replacement.Async;
			},
		});
		service.Getted += (_, args, _) =>
		{
			events++;
			Assert.Equal(42, args.Result);
			return ValueTask.CompletedTask;
		};
		var operation = service.GetAsync("17", CancellationToken.None).AsTask();

		try
		{
			Assert.False(operation.IsCompleted);
			Assert.Equal(0, original.Enumerations);
			Assert.Equal(0, replacement.Enumerations);
			release.SetResult();
			Assert.Equal(42, Assert.IsType<int>(await operation.WaitAsync(Stages.Timeout)));
			Assert.Equal(1, queries);
			Assert.Equal(1, filters);
			Assert.Equal(1, events);
			Assert.Equal(0, original.Enumerations);
			Assert.Equal(0, original.Moves);
			Assert.Equal(1, replacement.Enumerations);
			Assert.Equal(1, replacement.Moves);
			Assert.Equal(1, replacement.Disposals);
		}
		finally
		{
			release.TrySetResult();
			await operation.WaitAsync(Stages.Timeout);
		}
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(true, 0)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	public async Task BulkAsync_ValidationChangesReachAsyncHookWithoutRewrapping(bool upsert, int input)
	{
		using var model = new ModelScope();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		var stages = new Stages("validate:17", "consume:117", "validate:23", "consume:123");
		var service = new BulkService(provider, model.Name, accessor, stages);
		var source = new Sequence<BulkModel>([new() { Value = 17 }, new() { Value = 23 }]);
		var operation = input switch
		{
			0 => upsert ? service.UpsertManyAsync(source.Sync, cancellation.Token).AsTask() : service.InsertManyAsync(source.Sync, cancellation.Token).AsTask(),
			1 => upsert ? service.UpsertManyAsync(source.Async, cancellation.Token).AsTask() : service.InsertManyAsync(source.Async, cancellation.Token).AsTask(),
			_ => service.InvokeInvariantAsync(source.Sync, upsert, cancellation.Token).AsTask(),
		};

		try
		{
			await stages.AdvanceAsync(operation, cancellation.Token);
			Assert.Equal(2, await operation.WaitAsync(Stages.Timeout));
			Assert.Equal(1, service.Authorizations);
			Assert.Equal(1, service.Hooks);
			Assert.Equal(2, service.Validated.Count);
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
			Assert.Equal(upsert ? "UpsertMany" : "InsertMany", service.Method);
		}
		finally { stages.ReleaseAll(); }
	}

	private sealed class BulkModel
	{
		public int Value { get; set; }
	}

	private sealed class BulkService : DataServiceBase<BulkModel>
	{
		private readonly Stages _stages;
		public BulkService(IServiceProvider provider, string name, IDataAccess accessor, Stages stages) : base(provider)
		{
			this.Name = name;
			this.DataAccess = accessor;
			_stages = stages;
		}
		public List<IDataDictionary<BulkModel>> Validated { get; } = [];
		public int Authorizations { get; private set; }
		public int Hooks { get; private set; }
		public string Method { get; private set; }
		public ValueTask<int> InvokeInvariantAsync(IEnumerable source, bool upsert, CancellationToken cancellation) => upsert ? this.UpsertManyAsync(source, (ISchema)null, null, cancellation) : this.InsertManyAsync(source, (ISchema)null, null, cancellation);
		protected override ValueTask AuthorizeAsync(DataServiceMethod method, IDataOptions options, CancellationToken cancellation)
		{
			this.Authorizations++;
			this.Method = method.Name;
			return ValueTask.CompletedTask;
		}
		protected override async ValueTask OnValidateAsync(DataServiceMethod method, ISchema schema, IDataDictionary<BulkModel> data, IDataMutateOptions options, CancellationToken cancellation)
		{
			Assert.Equal(this.Method, method.Name);
			var value = data.GetValue<int>(nameof(BulkModel.Value), 0);
			await _stages.EnterAsync($"validate:{value}", cancellation);
			data.SetValue(nameof(BulkModel.Value), value + 100);
			this.Validated.Add(data);
		}
		protected override ValueTask<int> OnInsertManyAsync(IAsyncEnumerable<IDataDictionary<BulkModel>> items, ISchema schema, DataInsertOptions options, CancellationToken cancellation) => this.ConsumeAsync(items, cancellation);
		protected override ValueTask<int> OnUpsertManyAsync(IAsyncEnumerable<IDataDictionary<BulkModel>> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.ConsumeAsync(items, cancellation);
		private async ValueTask<int> ConsumeAsync(IAsyncEnumerable<IDataDictionary<BulkModel>> items, CancellationToken cancellation)
		{
			this.Hooks++;
			var count = 0;

			await foreach(var item in items.WithCancellation(cancellation))
			{
				Assert.Same(this.Validated[count++], item);
				await _stages.EnterAsync($"consume:{item.GetValue<int>(nameof(BulkModel.Value), 0)}", cancellation);
			}

			return count;
		}
	}

	private static async Task<List<int>> CollectAsync(IAsyncEnumerable<int> source)
	{
		var values = new List<int>();
		await foreach(var value in source)
			values.Add(value);
		return values;
	}

	private sealed class ServiceFilter : IDataServiceFilter<int>
	{
		public Func<IDataServiceContext<int>, CancellationToken, ValueTask<bool>> Before { get; init; } = (_, _) => ValueTask.FromResult(false);
		public Func<IDataServiceContext<int>, CancellationToken, ValueTask> After { get; init; } = (_, _) => ValueTask.CompletedTask;
		public ValueTask<bool> OnFilteringAsync(IDataServiceContext<int> context, CancellationToken cancellation = default) => this.Before(context, cancellation);
		public ValueTask OnFilteredAsync(IDataServiceContext<int> context, CancellationToken cancellation = default) => this.After(context, cancellation);
	}

	private sealed class TestService : DataServiceBase<int>
	{
		public TestService(IServiceProvider provider, string name, IDataAccess accessor) : base(provider)
		{
			this.Name = name;
			this.DataAccess = accessor;
		}
		public ValueTask<int> ImportItemsAsync(IAsyncEnumerable<int> items, DataImportOptions options, CancellationToken cancellation) => this.OnImportAsync(items, [], options, cancellation);
		public Func<DataServiceMethod, IDataOptions, CancellationToken, ValueTask> Authorizing { get; set; } = (_, _, _) => ValueTask.CompletedTask;
		public Func<DataServiceMethod, ICondition, IDataOptions, CancellationToken, ValueTask<ICondition>> Validating { get; set; } = (_, criteria, _, _) => ValueTask.FromResult(criteria);
		protected override ValueTask AuthorizeAsync(DataServiceMethod method, IDataOptions options, CancellationToken cancellation) => this.Authorizing(method, options, cancellation);
		protected override ValueTask<ICondition> OnValidateAsync(DataServiceMethod method, ICondition criteria, IDataOptions options, CancellationToken cancellation) => this.Validating(method, criteria, options, cancellation);
		protected override ICondition OnCondition(DataServiceMethod method, string key, IDataOptions options, out bool singular) { singular = true; return Condition.Equal("Id", key); }
	}
}
