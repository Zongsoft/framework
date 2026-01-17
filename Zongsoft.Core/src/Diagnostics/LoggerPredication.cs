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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics;

public sealed class LoggerPredication(string name = null) : LoggerPredication<LogEntry>(name)
{
}

public class LoggerPredication<TLog> : Common.IPredication<TLog> where TLog : ILog
{
	#region 构造函数
	public LoggerPredication(string name = null)
	{
		this.Name = name;
		this.Sources = [];
		this.Exceptions = [];
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public ICollection<string> Sources { get; set; }
	public ICollection<Type> Exceptions { get; set; }
	public LogLevel? MinLevel { get; set; }
	public LogLevel? MaxLevel { get; set; }
	#endregion

	#region 初始化器
	private bool _initialized;
	protected virtual void Initialize()
	{
		if(_initialized)
			return;

		lock(this)
		{
			if(_initialized)
				return;

			var path = string.IsNullOrEmpty(this.Name) ? "/Diagnostics/Loggers/Predication" : $"/Diagnostics/Loggers/{this.Name}/Predication";
			var options = ApplicationContext.Current?.Configuration?.GetOption<Configuration.LoggerPredicationOptions>(path);

			if(options != null)
			{
				if(options.MinLevel.HasValue)
					this.MinLevel = options.MinLevel;
				if(options.MaxLevel.HasValue)
					this.MaxLevel = options.MaxLevel;

				if(options.Sources != null && options.Sources.Count > 0)
					this.Sources = [.. options.Sources.Select(source => source.Pattern)];
				if(options.Exceptions != null && options.Exceptions.Count > 0)
					this.Exceptions = [.. options.Exceptions.Select(exception => exception.GetExceptionType())];
			}

			//设置初始化完成
			_initialized = true;
		}
	}
	#endregion

	#region 断言方法
	ValueTask<bool> Common.IPredication.PredicateAsync(object argument, CancellationToken cancellation) => this.PredicateAsync(argument is TLog log ? log : default, cancellation);
	ValueTask<bool> Common.IPredication.PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.PredicateAsync(argument is TLog log ? log : default, parameters, cancellation);

	public ValueTask<bool> PredicateAsync(TLog log, CancellationToken cancellation = default) => this.PredicateAsync(log, null, cancellation);
	public ValueTask<bool> PredicateAsync(TLog log, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		if(log == null)
			return ValueTask.FromResult(false);

		//初始化日志断言器
		this.Initialize();

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

		foreach(var source in this.Sources ?? [])
		{
			if(string.IsNullOrEmpty(source))
				continue;

			var matched = true;

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

			if(matched)
				break;
		}

		foreach(var exception in this.Exceptions ?? [])
		{
			if(exception == null)
				continue;

			var matched = log.Exception.GetType() == typeof(AggregateException) ?
				((AggregateException)log.Exception).InnerExceptions.Any(ex => exception.IsAssignableFrom(ex.GetType())) :
				exception.IsAssignableFrom(log.Exception.GetType());

			if(matched)
				break;
		}

		return ValueTask.FromResult(true);
	}
	#endregion
}
