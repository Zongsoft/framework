// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.ObjectPool;

namespace Zongsoft.Common;

/// <summary>
/// 提供异步操作的同步锁功能。
/// </summary>
/// <remarks>
/// 	<para>注意：本类代码基于 .NET 基础库的内部代码，版权归属 .NET 基金会。</para>
/// 	<para>源码：https://source.dot.net/#System.ServiceModel.Primitives/Internals/System/Runtime/AsyncLock.cs</para>
/// </remarks>
public sealed class Locker : IAsyncDisposable
{
	private static readonly ObjectPool<SemaphoreSlim> _semaphorePool = new DefaultObjectPool<SemaphoreSlim>(new SemaphoreSlimPooledObjectPolicy(), 100);
	private readonly object _syncRoot = new();
	private readonly AsyncLocal<SemaphoreRelease> _currentRelease;
	private SemaphoreSlim _rootSemaphore;
	private Task _disposeTask;

	public Locker()
	{
		_rootSemaphore = _semaphorePool.Get();
		_currentRelease = new AsyncLocal<SemaphoreRelease>();
	}

	public ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellation = default)
	{
		lock(_syncRoot)
		{
			ObjectDisposedException.ThrowIf(_disposeTask != null, this);

			var parent = this.GetCurrentRelease();
			var current = parent?.NextSemaphore ?? _rootSemaphore;
			var next = _semaphorePool.Get();
			var release = new SemaphoreRelease(current, next, parent, this);
			_currentRelease.Value = release;
			return TakeLockCoreAsync(current, release, cancellation);
		}

		static async ValueTask<IAsyncDisposable> TakeLockCoreAsync(SemaphoreSlim currentSemaphore, SemaphoreRelease release, CancellationToken cancellation)
		{
			try
			{
				await currentSemaphore.WaitAsync(cancellation);
				release.Acquire();
				return release;
			}
			catch
			{
				release.Cancel();
				throw;
			}
		}
	}

	public IDisposable Lock()
	{
		lock(_syncRoot)
		{
			ObjectDisposedException.ThrowIf(_disposeTask != null, this);

			var parent = this.GetCurrentRelease();
			var current = parent?.NextSemaphore ?? _rootSemaphore;
			current.Wait();

			var release = new SemaphoreRelease(current, _semaphorePool.Get(), parent, this);
			release.Acquire();
			_currentRelease.Value = release;
			return release;
		}
	}

	public ValueTask DisposeAsync()
	{
		lock(_syncRoot)
			return new ValueTask(_disposeTask ??= this.DisposeCoreAsync());
	}

	private async Task DisposeCoreAsync()
	{
		// Ensure the lock isn't held. If it is, wait for it to be released
		// before completing the dispose.
		await _rootSemaphore.WaitAsync();
		_rootSemaphore.Release();
		_semaphorePool.Return(_rootSemaphore);
		_rootSemaphore = null;
	}

	private SemaphoreRelease GetCurrentRelease()
	{
		var release = _currentRelease.Value;

		while(release != null && !release.IsActive)
			release = release.Parent;

		_currentRelease.Value = release;
		return release;
	}

	private void Pop(SemaphoreRelease release)
	{
		if(ReferenceEquals(_currentRelease.Value, release))
			_currentRelease.Value = release.Parent;
	}

	private sealed class SemaphoreRelease(SemaphoreSlim currentSemaphore, SemaphoreSlim nextSemaphore, SemaphoreRelease parent, Locker locker) : IAsyncDisposable, IDisposable
	{
		private const int PENDING = 0;
		private const int ACQUIRED = 1;
		private const int RELEASING = 2;
		private const int RELEASED = 3;

		private readonly SemaphoreSlim _currentSemaphore = currentSemaphore;
		private readonly SemaphoreSlim _nextSemaphore = nextSemaphore;
		private readonly Locker _locker = locker;
		private int _state;

		public SemaphoreRelease Parent { get; } = parent;
		public SemaphoreSlim NextSemaphore => _nextSemaphore;
		public bool IsActive => Volatile.Read(ref _state) <= ACQUIRED;

		public void Acquire()
		{
			if(Interlocked.CompareExchange(ref _state, ACQUIRED, PENDING) != PENDING)
				throw new InvalidOperationException("The lock acquisition is no longer active.");
		}

		public void Cancel()
		{
			if(Interlocked.CompareExchange(ref _state, RELEASED, PENDING) == PENDING)
				_semaphorePool.Return(_nextSemaphore);
		}

		public ValueTask DisposeAsync()
		{
			if(Interlocked.CompareExchange(ref _state, RELEASING, ACQUIRED) != ACQUIRED)
				return ValueTask.CompletedTask;

			_locker.Pop(this);
			return this.ReleaseAsync();
		}

		private async ValueTask ReleaseAsync()
		{
			try
			{
				await _nextSemaphore.WaitAsync();
				_currentSemaphore.Release();
				_nextSemaphore.Release();
				_semaphorePool.Return(_nextSemaphore);
			}
			finally
			{
				Volatile.Write(ref _state, RELEASED);
			}
		}

		public void Dispose()
		{
			if(Interlocked.CompareExchange(ref _state, RELEASING, ACQUIRED) != ACQUIRED)
				return;

			_locker.Pop(this);

			try
			{
				_nextSemaphore.Wait();
				_currentSemaphore.Release();
				_nextSemaphore.Release();
				_semaphorePool.Return(_nextSemaphore);
			}
			finally
			{
				Volatile.Write(ref _state, RELEASED);
			}
		}
	}

	private sealed class SemaphoreSlimPooledObjectPolicy : PooledObjectPolicy<SemaphoreSlim>
	{
		public override SemaphoreSlim Create() => new(1);
		public override bool Return(SemaphoreSlim semaphore)
		{
			if(semaphore.CurrentCount != 1)
			{
				System.Diagnostics.Debug.Assert(false, "Shouldn't be returning semaphore with a count != 1");
				return false;
			}

			return true;
		}
	}
}
