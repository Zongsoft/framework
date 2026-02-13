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
	private AsyncLocal<SemaphoreSlim> _currentSemaphore;
	private SemaphoreSlim _topmostSemaphore;
	private bool _isDisposed;

	public Locker()
	{
		_topmostSemaphore = _semaphorePool.Get();
		_currentSemaphore = new AsyncLocal<SemaphoreSlim>();
	}

	public ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellation = default)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_currentSemaphore.Value ??= _topmostSemaphore;
		SemaphoreSlim current = _currentSemaphore.Value;
		var next = _semaphorePool.Get();
		_currentSemaphore.Value = next;
		var release = new SemaphoreRelease(current, next, this);
		return TakeLockCoreAsync(current, release, cancellation);

		static async ValueTask<IAsyncDisposable> TakeLockCoreAsync(SemaphoreSlim currentSemaphore, SemaphoreRelease release, CancellationToken cancellation)
		{
			await currentSemaphore.WaitAsync(cancellation);
			return release;
		}
	}

	public IDisposable Lock()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_currentSemaphore.Value ??= _topmostSemaphore;
		SemaphoreSlim currentSem = _currentSemaphore.Value;
		currentSem.Wait();
		var nextSem = _semaphorePool.Get();
		_currentSemaphore.Value = nextSem;
		return new SemaphoreRelease(currentSem, nextSem, this);
	}

	public async ValueTask DisposeAsync()
	{
		if(_isDisposed)
			return;

		_isDisposed = true;
		// Ensure the lock isn't held. If it is, wait for it to be released
		// before completing the dispose.
		await _topmostSemaphore.WaitAsync();
		_topmostSemaphore.Release();
		_semaphorePool.Return(_topmostSemaphore);
		_topmostSemaphore = null;
	}

	private struct SemaphoreRelease(SemaphoreSlim currentSemaphore, SemaphoreSlim nextSemaphore, Locker locker) : IAsyncDisposable, IDisposable
	{
		private SemaphoreSlim _currentSemaphore = currentSemaphore;
		private SemaphoreSlim _nextSemaphore = nextSemaphore;
		private Locker _locker = locker;

		public ValueTask DisposeAsync()
		{
			System.Diagnostics.Debug.Assert(_nextSemaphore == _locker._currentSemaphore.Value, "_nextSemaphore was expected to by the current semaphore");

			// Update _asyncLock._currentSemaphore in the calling ExecutionContext
			// and defer any awaits to DisposeCoreAsync(). If this isn't done, the
			// update will happen in a copy of the ExecutionContext and the caller
			// won't see the changes.
			if(_currentSemaphore == _locker._topmostSemaphore)
				_locker._currentSemaphore.Value = null;
			else
				_locker._currentSemaphore.Value = _currentSemaphore;

			return this.DisposeCoreAsync();
		}

		private async ValueTask DisposeCoreAsync()
		{
			await _nextSemaphore.WaitAsync();
			_currentSemaphore.Release();
			_nextSemaphore.Release();
			_semaphorePool.Return(_nextSemaphore);
		}

		public void Dispose()
		{
			if(_currentSemaphore == _locker._topmostSemaphore)
				_locker._currentSemaphore.Value = null;
			else
				_locker._currentSemaphore.Value = _currentSemaphore;

			_nextSemaphore.Wait();
			_currentSemaphore.Release();
			_nextSemaphore.Release();
			_semaphorePool.Return(_nextSemaphore);
		}
	}

	private class SemaphoreSlimPooledObjectPolicy : PooledObjectPolicy<SemaphoreSlim>
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
