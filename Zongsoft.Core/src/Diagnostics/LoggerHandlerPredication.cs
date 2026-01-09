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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Diagnostics;

public sealed class LoggerHandlerPredication : LoggerHandlerPredication<LogEntry>
{
	public static readonly LoggerHandlerPredication Default = new();
}

public class LoggerHandlerPredication<TLog> : Common.IPredication<TLog> where TLog : ILog
{
	#region 公共属性
	public string Source { get; set; }
	public Type ExceptionType { get; set; }
	public LogLevel? MinLevel { get; set; }
	public LogLevel? MaxLevel { get; set; }
	#endregion

	#region 断言方法
	ValueTask<bool> Common.IPredication.PredicateAsync(object argument, CancellationToken cancellation) => this.PredicateAsync(argument is TLog log ? log : default, cancellation);
	ValueTask<bool> Common.IPredication.PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.PredicateAsync(argument is TLog log ? log : default, parameters, cancellation);

	public ValueTask<bool> PredicateAsync(TLog log, CancellationToken cancellation = default) => this.PredicateAsync(log, null, cancellation);
	public ValueTask<bool> PredicateAsync(TLog log, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		if(log == null)
			return ValueTask.FromResult(false);

		if(!string.IsNullOrWhiteSpace(this.Source))
		{
			var matched = true;
			var source = this.Source.Trim();

			if(source[0] == '*' || source[^1] == '*')
			{
				if(source[0] == '*')
				{
					if(source[^1] == '*')
						matched = log.Source.Contains(source.Trim('*'));
					else
						matched = log.Source.EndsWith(source.Trim('*'));
				}
				else
				{
					matched = log.Source.StartsWith(source.Trim('*'));
				}
			}
			else
			{
				matched &= string.Equals(log.Source, source, StringComparison.OrdinalIgnoreCase);
			}

			if(!matched)
				return ValueTask.FromResult(false);
		}

		if(this.MinLevel.HasValue)
		{
			if(log.Level < this.MinLevel.Value)
				return ValueTask.FromResult(false);
		}

		if(this.MaxLevel.HasValue)
		{
			if(log.Level > this.MaxLevel.Value)
				return ValueTask.FromResult(false);
		}

		if(this.ExceptionType != null)
		{
			if(log.Exception == null)
				return ValueTask.FromResult(false);

			var result = log.Exception.GetType() == typeof(AggregateException) ?
				((AggregateException)log.Exception).InnerExceptions.Any(ex => this.ExceptionType.IsAssignableFrom(ex.GetType())) :
				this.ExceptionType.IsAssignableFrom(log.Exception.GetType());

			if(!result)
				return ValueTask.FromResult(false);
		}

		return ValueTask.FromResult(true);
	}
	#endregion
}
