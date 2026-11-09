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

namespace Zongsoft.Diagnostics;

public class LogEntry : ILog
{
	#region 构造函数
	public LogEntry(LogLevel level, string source, string message, object data = null) : this(level, source, message, null, data) { }
	public LogEntry(LogLevel level, string source, Exception exception, object data = null) : this(level, source, null, exception, data) { }
	public LogEntry(LogLevel level, string source, string message, Exception exception, object data = null)
	{
		if(exception == null)
		{
			if(data is Exception ex)
			{
				exception = ex;
				data = null;
			}
		}

		this.Level = level;
		this.Source = string.IsNullOrEmpty(source) ? (exception == null ? string.Empty : exception.Source) : source.Trim();
		this.Exception = exception;
		this.Message = message ?? (exception == null ? string.Empty : exception.Message);
		this.Data = data ?? (exception != null && exception.Data != null && exception.Data.Count > 0 ? exception.Data : null);
		this.Timestamp = DateTime.Now;
	}
	#endregion

	#region 公共属性
	public LogLevel Level { get; }
	public string Source { get; }
	public string Message { get; }
	public object Data { get; }
	public DateTime Timestamp { get; }
	public Exception Exception { get; }
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Level}]{this.Source}@{this.Timestamp}";
	#endregion
}
