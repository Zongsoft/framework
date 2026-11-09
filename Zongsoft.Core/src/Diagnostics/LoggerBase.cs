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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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

		this.Name = name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取日志记录器名称。</summary>
	public string Name { get; }
	/// <summary>获取或设置日志断言。</summary>
	public Common.IPredication<TLog> Predication { get; set; }
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

	#region 刷新方法
	ValueTask ILogger.FlushAsync(CancellationToken cancellation) => this.OnFlushAsync(cancellation);
	protected virtual ValueTask OnFlushAsync(CancellationToken cancellation) => ValueTask.CompletedTask;
	#endregion
}

public abstract class LoggerBase<TLog, TModel>(string name = null) : LoggerBase<TLog>(name), ILogger<TLog, TModel> where TLog : ILog
{
	#region 公共属性
	/// <summary>获取或设置日志格式化器。</summary>
	public ILogFormatter<TLog, TModel> Formatter { get; protected set; }
	#endregion
}
