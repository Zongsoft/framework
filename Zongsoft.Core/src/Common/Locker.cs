using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

namespace Zongsoft.Common;

public sealed class Locker
{
	private const int UNLOCKED_ID = 0;

	private SemaphoreSlim _semaphore = new(1, 1);
	private int _reentrances = 0;

	private SemaphoreSlim _retry = new(0, 1);
	private long _owningId = UNLOCKED_ID;
	private int _owningThreadId = UNLOCKED_ID;

	private static long _asyncCounter = 0;
	private static readonly AsyncLocal<long> _asyncLocal = new();
	private static int ThreadId => Environment.CurrentManagedThreadId;

	public Task<IDisposable> LockAsync(CancellationToken cancellation = default)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.AcquireLockAsync(cancellation);
	}

	public Task<bool> TryLockAsync(Action callback, TimeSpan timeout)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.TryAcquireLockAsync(timeout).ContinueWith(task =>
		{
			if(task.Exception is AggregateException ex)
				ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

			var disposable = task.Result;
			if(disposable is null)
				return false;

			try
			{
				callback();
			}
			finally
			{
				disposable.Dispose();
			}

			return true;
		});
	}

	public Task<bool> TryLockAsync(Func<Task> callback, TimeSpan timeout)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.TryAcquireLockAsync(timeout).ContinueWith(task =>
		{
			if(task.Exception is AggregateException ex)
				ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

			var disposable = task.Result;
			if(disposable is null)
				return Task.FromResult(false);

			return callback().ContinueWith((task, state) =>
			{
				(state as IDisposable)?.Dispose();

				if(task.Exception is AggregateException ex)
					ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

				return true;
			}, disposable, TaskScheduler.Default);
		}, TaskScheduler.Default).Unwrap();
	}

	public Task<bool> TryLockAsync(Action callback, CancellationToken cancellation = default)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.TryAcquireLockAsync(cancellation).ContinueWith(task =>
		{
			if(task.Exception is AggregateException ex)
				ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

			var disposable = task.Result;
			if(disposable is null)
				return false;

			try
			{
				callback();
			}
			finally
			{
				disposable.Dispose();
			}

			return true;
		}, TaskScheduler.Default);
	}

	public Task<bool> TryLockAsync(Func<Task> callback, CancellationToken cancellation = default)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.TryAcquireLockAsync(cancellation).ContinueWith(task =>
		{
			if(task.Exception is AggregateException ex)
				ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

			var disposable = task.Result;
			if(disposable is null)
				return Task.FromResult(false);

			return callback().ContinueWith((task, state) =>
			{
				(state as IDisposable)?.Dispose();

				if(task.Exception is AggregateException ex)
					ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

				return true;
			}, disposable, TaskScheduler.Default);
		}, TaskScheduler.Default).Unwrap();
	}

	public IDisposable Lock(CancellationToken cancellation = default)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);
		return token.AcquireLock(cancellation);
	}

	public bool TryLock(Action callback, TimeSpan timeout)
	{
		var token = new LockToken(this, _asyncLocal.Value, ThreadId);
		_asyncLocal.Value = Interlocked.Increment(ref _asyncCounter);

		var disposable = token.TryAcquireLock(timeout);
		if(disposable is null)
			return false;

		try
		{
			callback();
		}
		finally
		{
			disposable.Dispose();
		}

		return true;
	}

	private readonly struct LockToken : IDisposable
	{
		private readonly Locker _parent;
		private readonly long _asyncId;
		private readonly int _threadId;

		internal LockToken(Locker parent, long asyncId, int threadId)
		{
			_parent = parent;
			_asyncId = asyncId;
			_threadId = threadId;
		}

		internal async Task<IDisposable> AcquireLockAsync(CancellationToken cancellation = default)
		{
			while(true)
			{
				await _parent._semaphore.WaitAsync(cancellation).ConfigureAwait(false);

				if(this.TryEnter(false))
					break;

				_parent._semaphore.Release();
				await _parent._retry.WaitAsync(cancellation).ConfigureAwait(false);
			}

			_parent._owningThreadId = ThreadId;
			_parent._semaphore.Release();
			return this;
		}

		internal async Task<IDisposable> TryAcquireLockAsync(TimeSpan timeout)
		{
			if(timeout == TimeSpan.Zero)
			{
				if(_parent._semaphore.Wait(timeout))
				{
					if(this.TryEnter(false))
					{
						_parent._owningThreadId = ThreadId;
						_parent._semaphore.Release();

						return this;
					}

					_parent._semaphore.Release();
				}

				return null;
			}

			var now = DateTimeOffset.UtcNow;
			var last = now;
			var remainder = timeout;

			while(remainder > TimeSpan.Zero)
			{
				await _parent._semaphore.WaitAsync(remainder).ConfigureAwait(false);

				if(this.TryEnter(false))
				{
					_parent._owningThreadId = ThreadId;
					_parent._semaphore.Release();

					return this;
				}

				now = DateTimeOffset.UtcNow;
				remainder -= now - last;
				last = now;
				if(remainder < TimeSpan.Zero)
				{
					_parent._semaphore.Release();
					return null;
				}

				_parent._semaphore.Release();
				if(!await _parent._retry.WaitAsync(remainder).ConfigureAwait(false))
					return null;

				now = DateTimeOffset.UtcNow;
				remainder -= now - last;
				last = now;
			}

			return null;
		}

		internal async Task<IDisposable> TryAcquireLockAsync(CancellationToken cancellation = default)
		{
			try
			{
				while(true)
				{
					await _parent._semaphore.WaitAsync(cancellation).ConfigureAwait(false);

					if(this.TryEnter(false))
						break;

					_parent._semaphore.Release();
					await _parent._retry.WaitAsync(cancellation).ConfigureAwait(false);
				}
			}
			catch(OperationCanceledException)
			{
				return null;
			}

			_parent._owningThreadId = ThreadId;
			_parent._semaphore.Release();
			return this;
		}

		internal IDisposable AcquireLock(CancellationToken cancellation = default)
		{
			while(true)
			{
				_parent._semaphore.Wait(cancellation);

				if(this.TryEnter(true))
				{
					_parent._semaphore.Release();
					break;
				}

				_parent._semaphore.Release();
				_parent._retry.Wait(cancellation);
			}

			return this;
		}

		internal IDisposable TryAcquireLock(TimeSpan timeout)
		{
			if(timeout == TimeSpan.Zero)
			{
				_parent._semaphore.Wait(timeout);

				if(this.TryEnter(true))
				{
					_parent._semaphore.Release();
					return this;
				}

				_parent._semaphore.Release();
				return null;
			}

			var now = DateTimeOffset.UtcNow;
			var last = now;
			var remainder = timeout;

			while(remainder > TimeSpan.Zero)
			{
				if(_parent._semaphore.Wait(remainder))
				{
					if(this.TryEnter(true))
					{
						_parent._semaphore.Release();
						return this;
					}

					now = DateTimeOffset.UtcNow;
					remainder -= now - last;
					last = now;

					_parent._semaphore.Release();
					if(remainder > TimeSpan.Zero && !_parent._retry.Wait(remainder))
						return null;
				}

				now = DateTimeOffset.UtcNow;
				remainder -= now - last;
				last = now;
			}

			return null;
		}

		private bool TryEnter(bool synchronous)
		{
			if(synchronous)
			{
				if(_parent._owningThreadId == UNLOCKED_ID)
					_parent._owningThreadId = ThreadId;
				else if(_parent._owningThreadId != ThreadId)
					return false;

				_parent._owningId = _asyncLocal.Value;
			}
			else
			{
				if(_parent._owningId == UNLOCKED_ID)
					_parent._owningId = _asyncLocal.Value;
				else if(_parent._owningId != _asyncId)
					return false;
				else
					_parent._owningId = _asyncLocal.Value; // Nested reentrance
			}

			_parent._reentrances += 1;
			return true;
		}

		public void Dispose()
		{
			var asyncId = _asyncId;
			var threadId = _threadId;
			_parent._semaphore.Wait();

			try
			{
				_parent._reentrances -= 1;
				_parent._owningId = asyncId;
				_parent._owningThreadId = threadId;

				if(_parent._reentrances == 0)
				{
					_parent._owningId = UNLOCKED_ID;
					_parent._owningThreadId = UNLOCKED_ID;
				}

				if(_parent._retry.CurrentCount == 0)
					_parent._retry.Release();
			}
			finally
			{
				_parent._semaphore.Release();
			}
		}
	}
}
