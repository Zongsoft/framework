using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataAsyncPipelineTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_AwaitsEachStageInOrder(bool scalar)
	{
		using var model = new ModelScope();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new TestDataAccess();
		var stages = new Stages("before", "event-before-1", "event-before-2", "filter-before", "provider", "filter-after", "event-after-1", "event-after-2", "after");
		accessor.Executing += (_, args, token) => StepAsync("event-before-1", args.Context, token);
		accessor.Executing += (_, args, token) => StepAsync("event-before-2", args.Context, token);
		accessor.Executed += (_, args, token) => StepAsync("event-after-1", args.Context, token);
		accessor.Executed += (_, args, token) => StepAsync("event-after-2", args.Context, token);
		accessor.Filters.Add(new AccessFilter<DataExecuteContextBase>
		{
			Before = (context, token) => StepAsync("filter-before", context, token),
			After = (context, token) => StepAsync("filter-after", context, token),
		});
		accessor.Execute = async (context, token) =>
		{
			await StepAsync("provider", context, token);
			context.Result = 9;
		};
		var operation = ExecuteAsync();

		try
		{
			await stages.AdvanceAsync(operation, cancellation.Token);
			Assert.Equal(9, Assert.IsType<int>(await operation.WaitAsync(Stages.Timeout)));
			Assert.Equal(1, accessor.Execution.DisposalCount);
		}
		finally
		{
			stages.ReleaseAll();
			await operation.WaitAsync(Stages.Timeout);
		}

		ValueTask StepAsync(string name, DataExecuteContextBase context, CancellationToken token)
		{
			Assert.Same(accessor.Execution, context);
			return stages.EnterAsync(name, token);
		}

		async ValueTask<bool> BeforeAsync(DataExecuteContextBase context, CancellationToken token)
		{
			await StepAsync("before", context, token);
			return false;
		}

		async Task<object> ExecuteAsync()
		{
			if(scalar)
				return await accessor.ExecuteScalarAsync(model.Name, (DataExecuteOptions)null, BeforeAsync, (context, token) => StepAsync("after", context, token), cancellation.Token);
			return await accessor.ExecuteAsync(model.Name, (DataExecuteOptions)null, BeforeAsync, (context, token) => StepAsync("after", context, token), cancellation.Token);
		}
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(true, 0)]
	[InlineData(false, 1)]
	[InlineData(true, 1)]
	[InlineData(false, 2)]
	[InlineData(true, 2)]
	[InlineData(false, 3)]
	[InlineData(true, 3)]
	public async Task ExecuteAsync_FailureOrShortCircuit_DisposesContext(bool scalar, int stop)
	{
		using var model = new ModelScope();
		await using var accessor = new TestDataAccess();
		var calls = new List<string>();
		var failure = new InvalidOperationException("Expected execution pipeline failure.");
		accessor.Executing += (_, args, _) =>
		{
			calls.Add("event-before");
			args.Context.Result = 9;
			args.Cancel = stop == 1;
			return ValueTask.CompletedTask;
		};
		accessor.Executed += (_, _, _) => { calls.Add("event-after"); return ValueTask.CompletedTask; };
		accessor.Filters.Add(new AccessFilter<DataExecuteContextBase>
		{
			Before = (_, _) => { calls.Add("filter-before"); return ValueTask.CompletedTask; },
			After = (_, _) => { calls.Add("filter-after"); return ValueTask.FromException(failure); },
		});
		accessor.Execute = (context, _) =>
		{
			calls.Add("provider");
			context.Result = 9;
			return stop == 2 ? ValueTask.FromException(failure) : ValueTask.CompletedTask;
		};

		if(stop < 2)
			Assert.Equal(9, Assert.IsType<int>(await ExecuteAsync()));
		else
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(ExecuteAsync));

		Assert.Equal(stop switch
		{
			0 => ["before"],
			1 => ["before", "event-before"],
			2 => ["before", "event-before", "filter-before", "provider"],
			_ => new[] { "before", "event-before", "filter-before", "provider", "filter-after" },
		}, calls);
		Assert.Equal(1, accessor.Execution.DisposalCount);

		ValueTask<bool> BeforeAsync(DataExecuteContextBase context, CancellationToken token)
		{
			calls.Add("before");
			context.Result = 9;
			return ValueTask.FromResult(stop == 0);
		}

		ValueTask AfterAsync(DataExecuteContextBase context, CancellationToken token)
		{
			calls.Add("after");
			return ValueTask.CompletedTask;
		}

		async Task<object> ExecuteAsync()
		{
			if(scalar)
				return await accessor.ExecuteScalarAsync(model.Name, (DataExecuteOptions)null, BeforeAsync, AfterAsync, CancellationToken.None);
			return await accessor.ExecuteAsync(model.Name, (DataExecuteOptions)null, BeforeAsync, AfterAsync, CancellationToken.None);
		}
	}

	[Theory]
	[InlineData(false, true, false)]
	[InlineData(false, false, true)]
	[InlineData(true, true, true)]
	[InlineData(true, false, false)]
	public async Task ExecuteAsync_ResultEnumeration_DisposesSourceAndContext(bool asynchronous, bool shortCircuit, bool fails)
	{
		using var model = new ModelScope();
		await using var accessor = new TestDataAccess();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new InvalidOperationException("Expected iterator failure.");
		var source = new Sequence<int>([17, 23]) { Failure = fails ? failure : null, DisposeGate = release.Task };
		var result = asynchronous ? (object)source.Async : source.Sync;
		var providers = 0;
		accessor.Execute = (context, _) =>
		{
			providers++;
			context.Result = result;
			return ValueTask.CompletedTask;
		};
		var sequence = accessor.ExecuteAsync<int>(model.Name, (DataExecuteOptions)null, (context, _) =>
		{
			context.Result = result;
			return ValueTask.FromResult(shortCircuit);
		}, null, CancellationToken.None);
		var iterator = sequence.GetAsyncEnumerator();

		try
		{
			Assert.True(await iterator.MoveNextAsync());
			Assert.Equal(17, iterator.Current);
			Assert.Equal(shortCircuit ? 0 : 1, providers);
			Assert.Equal(1, accessor.Execution.DisposalCount);

			if(fails)
			{
				var moving = iterator.MoveNextAsync().AsTask();
				if(asynchronous)
				{
					await source.Disposing.Task.WaitAsync(Stages.Timeout);
					Assert.False(moving.IsCompleted);
				}

				release.SetResult();
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(async () => await moving.WaitAsync(Stages.Timeout)));
			}
			else
			{
				var disposal = iterator.DisposeAsync().AsTask();
				if(asynchronous)
				{
					await source.Disposing.Task.WaitAsync(Stages.Timeout);
					Assert.False(disposal.IsCompleted);
				}

				release.SetResult();
				await disposal.WaitAsync(Stages.Timeout);
			}

			Assert.Equal(1, source.Enumerations);
			Assert.Equal(fails ? 2 : 1, source.Moves);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			release.TrySetResult();
			await iterator.DisposeAsync().AsTask().WaitAsync(Stages.Timeout);
		}
	}

	internal sealed class ModelScope : IDisposable
	{
		public string Name { get; } = $"Pipeline_{Guid.NewGuid():N}";
		public ModelScope()
		{
			Mapping.Entities.Add(new DataEntity(null, this.Name));
			Mapping.Commands.Add(new DataCommand(null, this.Name));
		}
		public void Dispose()
		{
			Mapping.Entities.Remove(this.Name);
			Mapping.Commands.Remove(this.Name);
		}
	}

	internal sealed class Stages(params string[] names)
	{
		internal static TimeSpan Timeout => TimeSpan.FromSeconds(5);
		private readonly TaskCompletionSource[] _entered = names.Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
		private readonly TaskCompletionSource[] _released = names.Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
		private readonly ConcurrentQueue<(string Name, CancellationToken Token)> _calls = new();

		public async ValueTask EnterAsync(string name, CancellationToken cancellation)
		{
			var index = Array.IndexOf(names, name);
			Assert.InRange(index, 0, names.Length - 1);
			_calls.Enqueue((name, cancellation));
			_entered[index].TrySetResult();
			await _released[index].Task.WaitAsync(cancellation);
		}
		public async Task AdvanceAsync(Task operation, CancellationToken cancellation)
		{
			for(var i = 0; i < names.Length; i++)
			{
				await _entered[i].Task.WaitAsync(Timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(names.Take(i + 1), _calls.Select(call => call.Name));
				Assert.All(_calls, call => Assert.Equal(cancellation, call.Token));
				_released[i].TrySetResult();
			}
		}
		public void ReleaseAll()
		{
			foreach(var release in _released)
				release.TrySetResult();
		}
	}

	internal sealed class AccessFilter<TContext> : IDataAccessFilter<TContext> where TContext : IDataAccessContextBase
	{
		public Func<TContext, CancellationToken, ValueTask> Before { get; init; } = (_, _) => ValueTask.CompletedTask;
		public Func<TContext, CancellationToken, ValueTask> After { get; init; } = (_, _) => ValueTask.CompletedTask;
		public ValueTask OnFilteringAsync(TContext context, CancellationToken cancellation = default) => this.Before(context, cancellation);
		public ValueTask OnFilteredAsync(TContext context, CancellationToken cancellation = default) => this.After(context, cancellation);
	}

	private sealed class ImportContext(IDataAccess access, string name, object data, IEnumerable<string> members, IDataImportOptions options) : DataImportContextBase(access, name, data, members, options)
	{
		public override T GetFeature<T>() => default;
	}
	internal sealed class TestDataAccess() : DataAccessBase("Pipeline")
	{
		public ExecuteContext Execution { get; private set; }
		public Func<DataImportContextBase, CancellationToken, ValueTask> Import { get; set; } = (_, _) => throw new NotSupportedException();
		public SelectContext Selection { get; private set; }
		public Func<DataExecuteContextBase, CancellationToken, ValueTask> Execute { get; set; } = (_, _) => throw new NotSupportedException();
		public Func<DataSelectContextBase, CancellationToken, ValueTask> Select { get; set; } = (_, _) => throw new NotSupportedException();
		protected override ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => this.Execute(context, cancellation);
		protected override ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation) => this.Select(context, cancellation);
		protected override ISchemaParser CreateSchema() => new EmptySchemaParser();
		protected override IDataSequencer CreateSequencer() => throw new NotSupportedException();
		protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) => this.Execution = new ExecuteContext(this, name, isScalar, resultType, parameters, options);
		protected override DataSelectContextBase CreateSelectContext(string name, Type modelType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) => this.Selection = new SelectContext(this, name, modelType, grouping, criteria, schema, paging, sortings, options);
		protected override DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options) => throw new NotSupportedException();
		protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) => throw new NotSupportedException();
		protected override DataImportContextBase CreateImportContext(string name, object data, IEnumerable<string> members, IDataImportOptions options) => new ImportContext(this, name, data, members, options);
		protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options) => throw new NotSupportedException();
		protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options) => throw new NotSupportedException();
		protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options) => throw new NotSupportedException();
		protected override DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options) => throw new NotSupportedException();
		protected override ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation) => this.Import(context, cancellation);
		protected override ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
		protected override ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => throw new NotSupportedException();
	}

	internal sealed class ExecuteContext(IDataAccess accessor, string name, bool scalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) : DataExecuteContextBase(accessor, name, scalar, resultType, parameters, options)
	{
		public int DisposalCount { get; private set; }
		public override TFeature GetFeature<TFeature>() => default;
		protected override void Dispose(bool disposing) { this.DisposalCount++; base.Dispose(disposing); }
	}

	internal sealed class SelectContext(IDataAccess accessor, string name, Type modelType, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) : DataSelectContextBase(accessor, name, modelType, grouping, criteria, schema, paging, sortings, options)
	{
		public int DisposalCount { get; private set; }
		public override TFeature GetFeature<TFeature>() => default;
		protected override void Dispose(bool disposing) { this.DisposalCount++; base.Dispose(disposing); }
	}

	private sealed class EmptySchemaParser : ISchemaParser
	{
		public ISchema Parse(string name, string expression, Type modelType = null) => null;
	}

	internal sealed class Sequence<T>(T[] items)
	{
		public int Enumerations { get; private set; }
		public int Moves { get; private set; }
		public int Disposals { get; private set; }
		public Exception Failure { get; init; }
		public Task DisposeGate { get; init; } = Task.CompletedTask;
		public TaskCompletionSource Disposing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public IEnumerable<T> Sync => new SyncEnumerable(this);
		public IAsyncEnumerable<T> Async => new AsyncEnumerable(this);

		private bool Move(ref int index)
		{
			this.Moves++;
			if(index == 0 && this.Failure != null)
				throw this.Failure;
			return ++index < items.Length;
		}
		private sealed class SyncEnumerable(Sequence<T> owner) : IEnumerable<T>
		{
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			public IEnumerator<T> GetEnumerator() { owner.Enumerations++; return new SyncEnumerator(owner); }
		}
		private sealed class SyncEnumerator(Sequence<T> owner) : IEnumerator<T>
		{
			private int _index = -1;
			public T Current => owner.GetItem(_index);
			object IEnumerator.Current => this.Current;
			public bool MoveNext() => owner.Move(ref _index);
			public void Reset() => throw new NotSupportedException();
			public void Dispose() => owner.Disposals++;
		}
		private sealed class AsyncEnumerable(Sequence<T> owner) : IAsyncEnumerable<T>
		{
			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) { owner.Enumerations++; return new AsyncEnumerator(owner, cancellation); }
		}
		private sealed class AsyncEnumerator(Sequence<T> owner, CancellationToken cancellation) : IAsyncEnumerator<T>
		{
			private int _index = -1;
			public T Current => owner.GetItem(_index);
			public ValueTask<bool> MoveNextAsync() { cancellation.ThrowIfCancellationRequested(); return ValueTask.FromResult(owner.Move(ref _index)); }
			public async ValueTask DisposeAsync()
			{
				owner.Disposals++;
				owner.Disposing.TrySetResult();
				await owner.DisposeGate;
			}
		}
		private T GetItem(int index) => items[index];
	}
}
