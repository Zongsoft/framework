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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

public sealed class DataConnectionCircuitBreakerManager : ICircuitBreakerManager
{
	#region 成员字段
	private readonly object _sync = new();
	private readonly TimeProvider _timeProvider;
	private readonly ConditionalWeakTable<IDataSource, DataConnectionCircuitBreaker> _breakers = new();
	private bool _enabled = true;
	#endregion

	#region 构造函数
	public DataConnectionCircuitBreakerManager(TimeProvider timeProvider = null) =>
		_timeProvider = timeProvider ?? TimeProvider.System;
	#endregion

	#region 公共事件
	public event EventHandler<DataConnectionCircuitBreakerStateChangedEventArgs> StateChanged;
	#endregion

	#region 公共属性
	public bool Enabled
	{
		get
		{
			lock(_sync)
				return _enabled;
		}
		set
		{
			lock(_sync)
			{
				if(_enabled == value)
					return;

				_enabled = value;

				if(!value)
				{
					foreach(var breaker in _breakers)
						breaker.Value.Reset(false);
				}
			}
		}
	}
	#endregion

	#region 公共方法
	public DataConnectionCircuitBreaker GetBreaker(IDataSource source)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		lock(_sync)
		{
			var breaker = _breakers.GetValue(source, key =>
			{
				var result = new DataConnectionCircuitBreaker(key, GetOptions(key), _timeProvider, () => this.Enabled);
				result.StateChanged += this.Breaker_StateChanged;
				return result;
			});

			return breaker;
		}
	}

	public void Execute(IDataSource source, Action operation)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var breaker = this.GetBreaker(source);

		breaker.Execute(operation);
	}

	public TResult Execute<TResult>(IDataSource source, Func<TResult> operation)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var breaker = this.GetBreaker(source);
		return breaker.Execute(operation);
	}

	public Task ExecuteAsync(IDataSource source, Func<CancellationToken, Task> operation, CancellationToken cancellation = default)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var breaker = this.GetBreaker(source);
		return breaker.ExecuteAsync(operation, cancellation);
	}

	public Task<TResult> ExecuteAsync<TResult>(IDataSource source, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellation = default)
	{
		if(operation == null)
			throw new ArgumentNullException(nameof(operation));

		var breaker = this.GetBreaker(source);
		return breaker.ExecuteAsync(operation, cancellation);
	}

	public bool Reset(IDataSource source)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(!_breakers.TryGetValue(source, out var breaker))
			return false;

		breaker.Reset();
		return true;
	}
	#endregion

	#region 显式接口实现
	ICircuitBreaker ICircuitBreakerManager.GetBreaker(IDataSource source) => this.GetBreaker(source);
	#endregion

	#region 私有方法
	private static DataConnectionCircuitBreakerOptions GetOptions(IDataSource source)
	{
		var options = new DataConnectionCircuitBreakerOptions();
		var properties = source.Properties;

		if(properties == null)
			return options;

		if(properties.TryGetValue(DataConnectionCircuitBreakerOptions.FAILURE_THRESHOLD_PROPERTY, out var value) &&
		   Zongsoft.Common.Convert.TryConvertValue<int>(value, out var failureThreshold) &&
		   failureThreshold > 0)
			options.FailureThreshold = failureThreshold;

		if(properties.TryGetValue(DataConnectionCircuitBreakerOptions.BREAK_DURATION_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<TimeSpan>(value, out var breakDuration) &&
		   breakDuration > TimeSpan.Zero)
			options.BreakDuration = breakDuration;

		if(properties.TryGetValue(DataConnectionCircuitBreakerOptions.MAXIMUM_BREAK_DURATION_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<TimeSpan>(value, out var maximumBreakDuration) &&
		   maximumBreakDuration > TimeSpan.Zero)
			options.MaximumBreakDuration = maximumBreakDuration;

		if(properties.TryGetValue(DataConnectionCircuitBreakerOptions.JITTER_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<double>(value, out var jitter) &&
		   jitter is >= 0 and <= 1)
			options.Jitter = jitter;

		return options;
	}

	private void Breaker_StateChanged(object sender, DataConnectionCircuitBreakerStateChangedEventArgs args)
	{
		var handlers = this.StateChanged;

		if(handlers == null)
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
}
