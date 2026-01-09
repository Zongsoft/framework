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
		this.StackTrace = string.Empty;
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
	public string StackTrace { get; internal set; }
	public object Data { get; }
	public DateTime Timestamp { get; }
	public Exception Exception { get; }
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Level}]{this.Source}@{this.Timestamp}";
	#endregion
}
