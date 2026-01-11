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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Diagnostics;

public abstract class LoggerBase<TLog> : ILogger<TLog> where TLog : ILog
{
	#region 构造函数
	protected LoggerBase(string name = null)
	{
		if(string.IsNullOrEmpty(name))
		{
			name = this.GetType().Name;

			if(name.Length > 6 && name.EndsWith("Logger"))
				name = name[..^6];
		}

		this.Name = name ?? this.GetType().Name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取日志记录器名称。</summary>
	public string Name { get; }
	/// <summary>获取或设置日志断言。</summary>
	public Common.IPredication<TLog> Predication { get; protected set; }
	#endregion

	#region 公共方法
	ValueTask ILogger.LogAsync<T>(T log, CancellationToken cancellation)
	{
		if(log is TLog entry)
			return this.LogAsync(entry, cancellation);

		return ValueTask.CompletedTask;
	}

	public async ValueTask LogAsync(TLog log, CancellationToken cancellation = default)
	{
		if(log == null)
			return;

		if(await this.CanLogAsync(log, cancellation))
			await this.OnLogAsync(log, cancellation);
	}
	#endregion

	#region 判断方法
	protected virtual ValueTask<bool> CanLogAsync(TLog log, CancellationToken cancellation)
	{
		var predication = this.Predication;
		if(predication == null)
			return ValueTask.FromResult(true);

		return predication.PredicateAsync(log, cancellation);
	}
	#endregion

	#region 日志方法
	protected abstract ValueTask OnLogAsync(TLog log, CancellationToken cancellation);
	#endregion
}

public abstract class LoggerBase<TLog, TModel>(string name = null) : LoggerBase<TLog>(name), ILogger<TLog, TModel> where TLog : ILog
{
	#region 公共属性
	/// <summary>获取或设置日志格式化器。</summary>
	public ILogFormatter<TLog, TModel> Formatter { get; protected set; }
	#endregion
}
