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

public sealed partial class DataConnector
{
	internal enum CircuitBreakerState
	{
		Closed,
		Opened,
		HalfOpen,
	}

	internal sealed class CircuitBreakerOptions
	{
		#region 公共常量
		public const string FAILURE_THRESHOLD_PROPERTY = "CircuitBreaker.FailureThreshold";
		public const string MAXIMUM_DURATION_PROPERTY = "CircuitBreaker.MaximumDuration";
		public const string DURATION_PROPERTY = "CircuitBreaker.Duration";
		public const string JITTER_PROPERTY = "CircuitBreaker.Jitter";
		#endregion

		#region 成员字段
		private double _jitter = 0.2;
		private int _failureThreshold = 1;
		private TimeSpan _duration = TimeSpan.FromSeconds(1);
		private TimeSpan _maximumDuration = TimeSpan.FromSeconds(30);
		#endregion

		#region 公共属性
		public int FailureThreshold
		{
			get => _failureThreshold;
			set => _failureThreshold = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}

		public TimeSpan MaximumDuration
		{
			get => _maximumDuration;
			set => _maximumDuration = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}

		public TimeSpan Duration
		{
			get => _duration;
			set => _duration = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}

		public double Jitter
		{
			get => _jitter;
			set => _jitter = value is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}
		#endregion

		#region 内部方法
		internal CircuitBreakerOptions Clone() => new()
		{
			Jitter = this.Jitter,
			Duration = this.Duration,
			MaximumDuration = this.MaximumDuration,
			FailureThreshold = this.FailureThreshold,
		};
		#endregion
	}

	internal sealed class CircuitBreakerStateChangedEventArgs : EventArgs
	{
		public CircuitBreakerStateChangedEventArgs(IDataSource source, CircuitBreakerState originalState, CircuitBreakerState currentState, DateTimeOffset? retryAt, Exception exception)
		{
			this.Source = source ?? throw new ArgumentNullException(nameof(source));
			this.OriginalState = originalState;
			this.CurrentState = currentState;
			this.RetryAt = retryAt;
			this.Exception = exception;
		}

		public IDataSource Source { get; }
		public CircuitBreakerState OriginalState { get; }
		public CircuitBreakerState CurrentState { get; }
		public DateTimeOffset? RetryAt { get; }
		public Exception Exception { get; }
	}

	internal sealed class CircuitBreaker
	{
		#region 成员字段
		private readonly object _sync = new();
		private readonly TimeProvider _timeProvider;
		private readonly CircuitBreakerOptions _options;
		private int _failures;
		private int _breakCount;
		private long _generation;
		private Exception _lastException;
		private DateTimeOffset? _retryAt;
		private CircuitBreakerState _state;
		#endregion

		#region 构造函数
		internal CircuitBreaker(
			IDataSource source,
			CircuitBreakerOptions options = null,
			TimeProvider timeProvider = null)
		{
			this.Source = source ?? throw new ArgumentNullException(nameof(source));
			_options = (options ?? new CircuitBreakerOptions()).Clone();

			if(_options.MaximumDuration < _options.Duration)
				throw new ArgumentException(Properties.Resources.CircuitBreaker_InvalidMaximumDuration_Message, nameof(options));

			_timeProvider = timeProvider ?? TimeProvider.System;
		}
		#endregion

		#region 公共事件
		public event EventHandler<DataConnectionFailureEventArgs> Failed;
		public event EventHandler<CircuitBreakerStateChangedEventArgs> StateChanged;
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

		public CircuitBreakerState State
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
			catch(OperationCanceledException) when (cancellation.IsCancellationRequested)
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
			catch(OperationCanceledException) when (cancellation.IsCancellationRequested)
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
		internal void EnsureAvailable()
		{
			DataConnectionException unavailable = null;

			lock(_sync)
			{
				var now = _timeProvider.GetUtcNow();

				if(_state == CircuitBreakerState.HalfOpen ||
				   _state == CircuitBreakerState.Opened && _retryAt.HasValue && now < _retryAt.Value)
					unavailable = this.CreateUnavailableException(now);
			}

			if(unavailable != null)
				throw unavailable;
		}

		internal void Reset(bool notify)
		{
			CircuitBreakerStateChangedEventArgs args = null;

			lock(_sync)
			{
				var originalState = _state;

				_failures = 0;
				_breakCount = 0;
				_lastException = null;
				_retryAt = null;
				_generation++;
				_state = CircuitBreakerState.Closed;

				if(originalState != CircuitBreakerState.Closed)
					args = new(this.Source, originalState, _state, null, null);
			}

			if(notify)
				this.OnStateChanged(args);
		}
		#endregion

		#region 私有方法
		private Permit Acquire()
		{
			Permit permit;
			DataConnectionException unavailable = null;
			CircuitBreakerStateChangedEventArgs args = null;

			lock(_sync)
			{
				var now = _timeProvider.GetUtcNow();

				switch(_state)
				{
					case CircuitBreakerState.Closed:
						permit = new(_generation, false);
						break;
					case CircuitBreakerState.Opened:
						if(_retryAt.HasValue && now < _retryAt.Value)
						{
							permit = default;
							unavailable = this.CreateUnavailableException(now);
							break;
						}

						var originalState = _state;
						_state = CircuitBreakerState.HalfOpen;
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
			CircuitBreakerStateChangedEventArgs args = null;

			lock(_sync)
			{
				if(permit.Generation != _generation)
					return;

				if(permit.Probe && _state == CircuitBreakerState.HalfOpen)
				{
					var originalState = _state;
					_state = CircuitBreakerState.Closed;
					_failures = 0;
					_breakCount = 0;
					_lastException = null;
					_retryAt = null;
					_generation++;
					args = new(this.Source, originalState, _state, null, null);
				}
				else if(_state == CircuitBreakerState.Closed)
				{
					_failures = 0;
					_lastException = null;
				}
			}

			this.OnStateChanged(args);
		}

		private void Fail(Permit permit, Exception exception)
		{
			CircuitBreakerStateChangedEventArgs args = null;
			DataConnectionFailureEventArgs failure = null;

			lock(_sync)
			{
				if(permit.Generation != _generation)
					return;

				_lastException = exception;
				_failures++;

				if(permit.Probe && _state == CircuitBreakerState.HalfOpen)
					args = this.Trip(CircuitBreakerState.HalfOpen, exception);
				else if(_state == CircuitBreakerState.Closed && _failures >= _options.FailureThreshold)
					args = this.Trip(CircuitBreakerState.Closed, exception);

				var now = _timeProvider.GetUtcNow();
				failure = new(this.Source, exception, _failures, _retryAt, _retryAt.HasValue ? _retryAt.Value - now : TimeSpan.Zero);
			}

			this.OnFailed(failure);
			this.OnStateChanged(args);
		}

		private void Cancel(Permit permit)
		{
			CircuitBreakerStateChangedEventArgs args = null;

			lock(_sync)
			{
				if(permit.Generation != _generation || !permit.Probe || _state != CircuitBreakerState.HalfOpen)
					return;

				var originalState = _state;
				_state = CircuitBreakerState.Opened;
				_retryAt = _timeProvider.GetUtcNow() + this.GetBreakDuration();
				_generation++;
				args = new(this.Source, originalState, _state, _retryAt, _lastException);
			}

			this.OnStateChanged(args);
		}

		private CircuitBreakerStateChangedEventArgs Trip(CircuitBreakerState originalState, Exception exception)
		{
			_breakCount++;
			_state = CircuitBreakerState.Opened;
			_retryAt = _timeProvider.GetUtcNow() + this.GetBreakDuration();
			_generation++;

			return new(this.Source, originalState, _state, _retryAt, exception);
		}

		private TimeSpan GetBreakDuration()
		{
			var exponent = Math.Min(_breakCount - 1, 30);
			var ticks = Math.Min(
				_options.Duration.Ticks * Math.Pow(2, exponent),
				_options.MaximumDuration.Ticks);

			if(_options.Jitter > 0)
				ticks *= 1 + (Random.Shared.NextDouble() * 2 - 1) * _options.Jitter;

			return TimeSpan.FromTicks((long)Math.Clamp(ticks, 1, _options.MaximumDuration.Ticks));
		}

		private DataConnectionException CreateUnavailableException(DateTimeOffset now) => new(this.Source, _retryAt, _retryAt.HasValue ? _retryAt.Value - now : TimeSpan.Zero, _lastException);

		private void OnFailed(DataConnectionFailureEventArgs args)
		{
			var handlers = this.Failed;

			if(args == null || handlers == null)
				return;

			foreach(EventHandler<DataConnectionFailureEventArgs> handler in handlers.GetInvocationList())
			{
				try
				{
					handler(this, args);
				}
				catch
				{
					//连接故障订阅者不能影响熔断状态
				}
			}
		}

		private void OnStateChanged(CircuitBreakerStateChangedEventArgs args)
		{
			var handlers = this.StateChanged;

			if(args == null || handlers == null)
				return;

			foreach(EventHandler<CircuitBreakerStateChangedEventArgs> handler in handlers.GetInvocationList())
			{
				try
				{
					handler(this, args);
				}
				catch
				{
					//状态事件订阅者不能影响数据连接操作
				}
			}
		}
		#endregion

		#region 嵌套结构
		private readonly struct Permit(long generation, bool probe)
		{
			public readonly long Generation = generation;
			public readonly bool Probe = probe;
		}
		#endregion
	}
}
