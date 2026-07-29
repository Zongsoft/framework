/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

public enum DataConnectionCircuitBreakerState
{
	Closed,
	Opened,
	HalfOpen,
}

public sealed class DataConnectionCircuitBreakerOptions
{
	#region 公共常量
	public const string FAILURE_THRESHOLD_PROPERTY = "CircuitBreaker.FailureThreshold";
	public const string BREAK_DURATION_PROPERTY = "CircuitBreaker.BreakDuration";
	public const string MAXIMUM_BREAK_DURATION_PROPERTY = "CircuitBreaker.MaximumBreakDuration";
	public const string JITTER_PROPERTY = "CircuitBreaker.Jitter";
	#endregion

	#region 成员字段
	private double _jitter = 0.2;
	private int _failureThreshold = 1;
	private TimeSpan _breakDuration = TimeSpan.FromSeconds(1);
	private TimeSpan _maximumBreakDuration = TimeSpan.FromSeconds(30);
	#endregion

	#region 公共属性
	public int FailureThreshold
	{
		get => _failureThreshold;
		set => _failureThreshold = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	public TimeSpan BreakDuration
	{
		get => _breakDuration;
		set => _breakDuration = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	public TimeSpan MaximumBreakDuration
	{
		get => _maximumBreakDuration;
		set => _maximumBreakDuration = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	public double Jitter
	{
		get => _jitter;
		set => _jitter = value is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	#endregion

	#region 内部方法
	internal DataConnectionCircuitBreakerOptions Clone() => new()
	{
		FailureThreshold = this.FailureThreshold,
		BreakDuration = this.BreakDuration,
		MaximumBreakDuration = this.MaximumBreakDuration,
		Jitter = this.Jitter,
	};
	#endregion
}

public sealed class DataConnectionCircuitBreakerStateChangedEventArgs : EventArgs
{
	public DataConnectionCircuitBreakerStateChangedEventArgs(
		IDataSource source,
		DataConnectionCircuitBreakerState originalState,
		DataConnectionCircuitBreakerState currentState,
		DateTimeOffset? retryAt,
		Exception exception)
	{
		this.Source = source ?? throw new ArgumentNullException(nameof(source));
		this.OriginalState = originalState;
		this.CurrentState = currentState;
		this.RetryAt = retryAt;
		this.Exception = exception;
	}

	public IDataSource Source { get; }
	public DataConnectionCircuitBreakerState OriginalState { get; }
	public DataConnectionCircuitBreakerState CurrentState { get; }
	public DateTimeOffset? RetryAt { get; }
	public Exception Exception { get; }
}

public sealed class DataConnectionUnavailableException : DataException
{
	public DataConnectionUnavailableException(
		IDataSource source,
		DataConnectionCircuitBreakerState state,
		DateTimeOffset? retryAt,
		TimeSpan retryAfter,
		Exception innerException) : base(GetMessage(source, state, retryAt), innerException)
	{
		this.SourceName = source?.Name;
		this.DriverName = source?.Driver?.Name;
		this.State = state;
		this.RetryAt = retryAt;
		this.RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
	}

	public string SourceName { get; }
	public string DriverName { get; }
	public DataConnectionCircuitBreakerState State { get; }
	public DateTimeOffset? RetryAt { get; }
	public TimeSpan RetryAfter { get; }

	private static string GetMessage(IDataSource source, DataConnectionCircuitBreakerState state, DateTimeOffset? retryAt)
	{
		var message = $"The '{source?.Name}' data source is temporarily unavailable because its connection circuit breaker is {state}.";
		return retryAt.HasValue ? $"{message} Retry after {retryAt.Value:O}." : message;
	}
}

public sealed class DataConnectionCircuitBreaker : ICircuitBreaker
{
	#region 成员字段
	private readonly object _sync = new();
	private readonly TimeProvider _timeProvider;
	private readonly DataConnectionCircuitBreakerOptions _options;
	private readonly Func<bool> _isEnabled;
	private int _failures;
	private int _breakCount;
	private long _generation;
	private Exception _lastException;
	private DateTimeOffset? _retryAt;
	private DataConnectionCircuitBreakerState _state;
	#endregion

	#region 构造函数
	public DataConnectionCircuitBreaker(
		IDataSource source,
		DataConnectionCircuitBreakerOptions options = null,
		TimeProvider timeProvider = null) : this(source, options, timeProvider, null) { }

	internal DataConnectionCircuitBreaker(
		IDataSource source,
		DataConnectionCircuitBreakerOptions options,
		TimeProvider timeProvider,
		Func<bool> isEnabled)
	{
		this.Source = source ?? throw new ArgumentNullException(nameof(source));
		_options = (options ?? new DataConnectionCircuitBreakerOptions()).Clone();

		if(_options.MaximumBreakDuration < _options.BreakDuration)
			throw new ArgumentException("The maximum break duration cannot be less than the break duration.", nameof(options));

		_timeProvider = timeProvider ?? TimeProvider.System;
		_isEnabled = isEnabled;
	}
	#endregion

	#region 公共事件
	public event EventHandler<DataConnectionCircuitBreakerStateChangedEventArgs> StateChanged;
	#endregion

	#region 公共属性
	public IDataSource Source { get; }

	public int Failures
	{
		get
		{
			lock(_sync)
				return _failures;
		}
	}

	public DataConnectionCircuitBreakerState State
	{
		get
		{
			lock(_sync)
				return _state;
		}
	}

	public DateTimeOffset? RetryAt
	{
		get
		{
			lock(_sync)
				return _retryAt;
		}
	}
	#endregion

	#region 公共方法
	public void Execute(Action operation)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var permit = this.Acquire();

		try
		{
			operation();
			this.Succeed(permit);
		}
		catch(Exception exception)
		{
			this.Fail(permit, exception);
			throw;
		}
	}

	public TResult Execute<TResult>(Func<TResult> operation)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var permit = this.Acquire();

		try
		{
			var result = operation();
			this.Succeed(permit);
			return result;
		}
		catch(Exception exception)
		{
			this.Fail(permit, exception);
			throw;
		}
	}

	public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellation = default)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var permit = this.Acquire();

		try
		{
			await operation(cancellation).ConfigureAwait(false);
			this.Succeed(permit);
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
		{
			this.Cancel(permit);
			throw;
		}
		catch(Exception exception)
		{
			this.Fail(permit, exception);
			throw;
		}
	}

	public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellation = default)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var permit = this.Acquire();

		try
		{
			var result = await operation(cancellation).ConfigureAwait(false);
			this.Succeed(permit);
			return result;
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
		{
			this.Cancel(permit);
			throw;
		}
		catch(Exception exception)
		{
			this.Fail(permit, exception);
			throw;
		}
	}

	public void Reset() => this.Reset(true);
	#endregion

	#region 内部方法
	internal void Reset(bool notify)
	{
		DataConnectionCircuitBreakerStateChangedEventArgs args = null;

		lock(_sync)
		{
			var originalState = _state;

			_failures = 0;
			_breakCount = 0;
			_lastException = null;
			_retryAt = null;
			_generation++;
			_state = DataConnectionCircuitBreakerState.Closed;

			if(originalState != DataConnectionCircuitBreakerState.Closed)
				args = new(this.Source, originalState, _state, null, null);
		}

		if(notify)
			this.OnStateChanged(args);
	}
	#endregion

	#region 私有方法
	private Permit Acquire()
	{
		if(_isEnabled != null && !_isEnabled())
			return new(0, false, true);

		Permit permit;
		DataConnectionUnavailableException unavailable = null;
		DataConnectionCircuitBreakerStateChangedEventArgs args = null;

		lock(_sync)
		{
			var now = _timeProvider.GetUtcNow();

			switch(_state)
			{
				case DataConnectionCircuitBreakerState.Closed:
					permit = new(_generation, false);
					break;
				case DataConnectionCircuitBreakerState.Opened:
					if(_retryAt.HasValue && now < _retryAt.Value)
					{
						permit = default;
						unavailable = this.CreateUnavailableException(now);
						break;
					}

					var originalState = _state;
					_state = DataConnectionCircuitBreakerState.HalfOpen;
					_generation++;
					permit = new(_generation, true);
					args = new(this.Source, originalState, _state, null, _lastException);
					break;
				default:
					permit = default;
					unavailable = this.CreateUnavailableException(now);
					break;
			}
		}

		this.OnStateChanged(args);

		if(unavailable != null)
			throw unavailable;

		return permit;
	}

	private void Succeed(Permit permit)
	{
		DataConnectionCircuitBreakerStateChangedEventArgs args = null;

		lock(_sync)
		{
			if(permit.Bypass || permit.Generation != _generation)
				return;

			if(permit.Probe && _state == DataConnectionCircuitBreakerState.HalfOpen)
			{
				var originalState = _state;
				_state = DataConnectionCircuitBreakerState.Closed;
				_failures = 0;
				_breakCount = 0;
				_lastException = null;
				_retryAt = null;
				_generation++;
				args = new(this.Source, originalState, _state, null, null);
			}
			else if(_state == DataConnectionCircuitBreakerState.Closed)
			{
				_failures = 0;
				_lastException = null;
			}
		}

		this.OnStateChanged(args);
	}

	private void Fail(Permit permit, Exception exception)
	{
		DataConnectionCircuitBreakerStateChangedEventArgs args = null;

		lock(_sync)
		{
			if(permit.Bypass || permit.Generation != _generation)
				return;

			_lastException = exception;

			if(permit.Probe && _state == DataConnectionCircuitBreakerState.HalfOpen)
				args = this.Trip(DataConnectionCircuitBreakerState.HalfOpen, exception);
			else if(_state == DataConnectionCircuitBreakerState.Closed && ++_failures >= _options.FailureThreshold)
				args = this.Trip(DataConnectionCircuitBreakerState.Closed, exception);
		}

		this.OnStateChanged(args);
	}

	private void Cancel(Permit permit)
	{
		DataConnectionCircuitBreakerStateChangedEventArgs args = null;

		lock(_sync)
		{
			if(permit.Bypass || permit.Generation != _generation || !permit.Probe || _state != DataConnectionCircuitBreakerState.HalfOpen)
				return;

			var originalState = _state;
			_state = DataConnectionCircuitBreakerState.Opened;
			_retryAt = _timeProvider.GetUtcNow() + this.GetBreakDuration();
			_generation++;
			args = new(this.Source, originalState, _state, _retryAt, _lastException);
		}

		this.OnStateChanged(args);
	}

	private DataConnectionCircuitBreakerStateChangedEventArgs Trip(DataConnectionCircuitBreakerState originalState, Exception exception)
	{
		_breakCount++;
		_state = DataConnectionCircuitBreakerState.Opened;
		_retryAt = _timeProvider.GetUtcNow() + this.GetBreakDuration();
		_generation++;

		return new(this.Source, originalState, _state, _retryAt, exception);
	}

	private TimeSpan GetBreakDuration()
	{
		var exponent = Math.Min(_breakCount - 1, 30);
		var ticks = Math.Min(
			_options.BreakDuration.Ticks * Math.Pow(2, exponent),
			_options.MaximumBreakDuration.Ticks);

		if(_options.Jitter > 0)
			ticks *= 1 + (Random.Shared.NextDouble() * 2 - 1) * _options.Jitter;

		return TimeSpan.FromTicks((long)Math.Clamp(ticks, 1, _options.MaximumBreakDuration.Ticks));
	}

	private DataConnectionUnavailableException CreateUnavailableException(DateTimeOffset now) => new(this.Source, _state, _retryAt, _retryAt.HasValue ? _retryAt.Value - now : TimeSpan.Zero, _lastException);

	private void OnStateChanged(DataConnectionCircuitBreakerStateChangedEventArgs args)
	{
		var handlers = this.StateChanged;

		if(args == null || handlers == null)
			return;

		foreach(EventHandler<DataConnectionCircuitBreakerStateChangedEventArgs> handler in handlers.GetInvocationList())
		{
			try
			{
				handler(this, args);
			}
			catch
			{
				//状态事件订阅者不能影响数据连接操作。
			}
		}
	}
	#endregion

	#region 嵌套结构
	private readonly struct Permit(long generation, bool probe, bool bypass = false)
	{
		public long Generation { get; } = generation;
		public bool Probe { get; } = probe;
		public bool Bypass { get; } = bypass;
	}
	#endregion
}
