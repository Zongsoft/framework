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

namespace Zongsoft.Data.Common;

/// <summary>提供按数据源共享的数据连接器管理器。</summary>
public static class DataConnectorManager
{
	#region 静态字段
	private static readonly object _syncLock = new();
	private static readonly ConditionalWeakTable<IDataSource, DataConnector> _connectors = new();
	#endregion

	#region 公共方法
	/// <summary>获取指定数据源对应的共享连接器。</summary>
	/// <param name="source">指定的数据源。</param>
	/// <returns>返回指定数据源对应的共享连接器。</returns>
	public static DataConnector GetConnector(IDataSource source)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(_connectors.TryGetValue(source, out var connector))
			return connector;

		lock(_syncLock)
			return _connectors.GetValue(source, key => new DataConnector(key, GetOptions(key)));
	}
	#endregion

	#region 私有方法
	private static DataConnector.CircuitBreakerOptions GetOptions(IDataSource source)
	{
		var options = new DataConnector.CircuitBreakerOptions();
		var properties = source.Properties;

		if(properties == null)
			return options;

		if(properties.TryGetValue(DataConnector.CircuitBreakerOptions.FAILURE_THRESHOLD_PROPERTY, out var value) &&
		   Zongsoft.Common.Convert.TryConvertValue<int>(value, out var failureThreshold) &&
		   failureThreshold > 0)
			options.FailureThreshold = failureThreshold;

		if(properties.TryGetValue(DataConnector.CircuitBreakerOptions.DURATION_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<TimeSpan>(value, out var duration) &&
		   duration > TimeSpan.Zero)
			options.Duration = duration;

		if(properties.TryGetValue(DataConnector.CircuitBreakerOptions.MAXIMUM_DURATION_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<TimeSpan>(value, out var maximumDuration) &&
		   maximumDuration > TimeSpan.Zero)
			options.MaximumDuration = maximumDuration;

		if(properties.TryGetValue(DataConnector.CircuitBreakerOptions.JITTER_PROPERTY, out value) &&
		   Zongsoft.Common.Convert.TryConvertValue<double>(value, out var jitter) &&
		   jitter is >= 0 and <= 1)
			options.Jitter = jitter;

		return options;
	}
	#endregion
}
