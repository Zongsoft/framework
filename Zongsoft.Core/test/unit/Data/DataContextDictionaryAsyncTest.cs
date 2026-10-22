using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataContextDictionaryAsyncTest : IDisposable
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
	private readonly string _name = $"AsyncDictionary_{Guid.NewGuid():N}";
	private readonly IDataAccess _accessor = DispatchProxy.Create<IDataAccess, DataValidationAsyncTest.UnusedAccessor>();

	public DataContextDictionaryAsyncTest() => Mapping.Entities.Add(new DataEntity(null, _name));
	public void Dispose() => Mapping.Entities.Remove(_name);

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public Task GetDataDictionariesAsync_AwaitsSourceAndPreservesValues(bool upsert, bool structs) => structs ?
		this.VerifyAsync<ValueRow>(upsert, [new() { Value = 17 }, new() { Value = 23 }]) :
		this.VerifyAsync<ValueModel>(upsert, [new() { Value = 17 }, null, new() { Value = 23 }]);

	private async Task VerifyAsync<T>(bool upsert, T[] items)
	{
		using var cancellation = new CancellationTokenSource();
		var source = new Input<T>(items);
		var context = this.CreateContext(upsert ? DataAccessMethod.Upsert : DataAccessMethod.Insert, source, 2);
		using var owner = (IDisposable)context;
		var iterator = GetDictionaries<T>(context, cancellation.Token).GetAsyncEnumerator();
		var moving = iterator.MoveNextAsync().AsTask();

		try
		{
			await source.Entered.Task.WaitAsync(_timeout);
			Assert.False(moving.IsCompleted);
			Assert.Same(source, context.Data);
			Assert.Equal(cancellation.Token, source.Cancellation);
			source.Release.TrySetResult();
			Assert.True(await moving.WaitAsync(_timeout));
			Assert.Equal(17, iterator.Current.GetValue<int>("Value", 0));
			Assert.Equal(items[0], iterator.Current.Data);
			Assert.True(await iterator.MoveNextAsync());
			Assert.Equal(23, iterator.Current.GetValue<int>("Value", 0));
			Assert.Equal(items[^1], iterator.Current.Data);
			source.DisposeRelease.TrySetResult();
			Assert.False(await iterator.MoveNextAsync());
			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			source.Release.TrySetResult();
			source.DisposeRelease.TrySetResult();
			await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task GetDataDictionariesAsync_CancellationOrEarlyExitAwaitsSourceDisposal(bool upsert, bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var source = new Input<ValueRow>([new() { Value = 17 }, new() { Value = 23 }]);
		var context = this.CreateContext(upsert ? DataAccessMethod.Upsert : DataAccessMethod.Insert, source, 2);
		using var owner = (IDisposable)context;
		var iterator = GetDictionaries<ValueRow>(context, cancellation.Token).GetAsyncEnumerator();
		var moving = iterator.MoveNextAsync().AsTask();

		try
		{
			await source.Entered.Task.WaitAsync(_timeout);
			Assert.False(moving.IsCompleted);
			Task completion;

			if(cancel)
			{
				cancellation.Cancel();
				completion = moving;
			}
			else
			{
				source.Release.TrySetResult();
				Assert.True(await moving.WaitAsync(_timeout));
				Assert.Equal(17, iterator.Current.GetValue<int>("Value", 0));
				completion = iterator.DisposeAsync().AsTask();
			}

			await source.Disposing.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			source.DisposeRelease.TrySetResult();

			if(cancel)
			{
				var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, error.CancellationToken);
			}
			else
				await completion.WaitAsync(_timeout);

			Assert.Equal(1, source.Enumerations);
			Assert.Equal(1, source.Disposals);
		}
		finally
		{
			source.Release.TrySetResult();
			source.DisposeRelease.TrySetResult();
			await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));
			await iterator.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(DataAccessMethod.Insert)]
	[InlineData(DataAccessMethod.Upsert)]
	[InlineData(DataAccessMethod.Update)]
	public async Task GetDataDictionariesAsync_ZeroCountDoesNotEnumerateInput(DataAccessMethod method)
	{
		var source = new Input<ValueRow>([new() { Value = 17 }]);
		var context = this.CreateContext(method, source, 0);
		using var owner = (IDisposable)context;
		await using var iterator = GetDictionaries<ValueRow>(context, CancellationToken.None).GetAsyncEnumerator();

		Assert.False(await iterator.MoveNextAsync());
		Assert.Equal(0, source.Enumerations);
		Assert.Equal(0, source.Disposals);
	}

	[Fact]
	public async Task GetDataDictionariesAsync_UpdatePreservesSingleObject()
	{
		var model = new ValueModel { Value = 17 };
		var context = this.CreateContext(DataAccessMethod.Update, model, 1);
		using var owner = (IDisposable)context;
		await using var iterator = GetDictionaries<ValueModel>(context, CancellationToken.None).GetAsyncEnumerator();

		Assert.True(await iterator.MoveNextAsync());
		Assert.Same(model, iterator.Current.Data);
		Assert.Equal(17, iterator.Current.GetValue<int>("Value", 0));
		Assert.False(await iterator.MoveNextAsync());
	}

	private IDataMutateContextBase CreateContext(DataAccessMethod method, object data, int count) => method switch
	{
		DataAccessMethod.Insert => new InsertContext(_accessor, _name, data) { Count = count },
		DataAccessMethod.Upsert => new UpsertContext(_accessor, _name, data) { Count = count },
		_ => new UpdateContext(_accessor, _name, data) { Count = count },
	};

	private static IAsyncEnumerable<IDataDictionary<T>> GetDictionaries<T>(IDataMutateContextBase context, CancellationToken cancellation) => context switch
	{
		DataInsertContextBase insert => insert.GetDataDictionariesAsync<T>(cancellation),
		DataUpsertContextBase upsert => upsert.GetDataDictionariesAsync<T>(cancellation),
		DataUpdateContextBase update => update.GetDataDictionariesAsync<T>(cancellation),
		_ => throw new NotSupportedException(),
	};

	private sealed class ValueModel
	{
		public int Value { get; set; }
	}
	private struct ValueRow
	{
		public int Value { get; set; }
	}
	private sealed class Input<T>(T[] items) : IAsyncEnumerable<T>
	{
		public int Enumerations { get; private set; }
		public int Disposals { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Disposing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource DisposeRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => this.ReadAsync(cancellation).GetAsyncEnumerator();
		private async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken cancellation)
		{
			this.Enumerations++;
			this.Cancellation = cancellation;

			try
			{
				this.Entered.TrySetResult();
				await this.Release.Task.WaitAsync(cancellation);

				foreach(var item in items)
				{
					cancellation.ThrowIfCancellationRequested();
					yield return item;
				}
			}
			finally
			{
				this.Disposals++;
				this.Disposing.TrySetResult();
				await this.DisposeRelease.Task;
			}
		}
	}
	private sealed class InsertContext(IDataAccess accessor, string name, object data) : DataInsertContextBase(accessor, name, true, data, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class UpsertContext(IDataAccess accessor, string name, object data) : DataUpsertContextBase(accessor, name, true, data, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class UpdateContext(IDataAccess accessor, string name, object data) : DataUpdateContextBase(accessor, name, data, null, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
}
