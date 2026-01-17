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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Diagnostics;

public sealed class TextFileLogger : TextFileLogger<LogEntry>
{
	#region 单例字段
	public static readonly TextFileLogger Default = new(null);
	#endregion

	#region 构造函数
	public TextFileLogger(string filePath, int fileLimit = FILE_LIMIT) : this(TimeSpan.FromSeconds(LOGGING_PERIOD), LOGGING_LIMIT, filePath, fileLimit) { }
	public TextFileLogger(TimeSpan period, int capacity = LOGGING_LIMIT) : this(period, capacity, null, FILE_LIMIT) { }
	public TextFileLogger(TimeSpan period, int capacity, string filePath, int fileLimit = FILE_LIMIT) : base(period, capacity, filePath, fileLimit)
	{
		this.Formatter = XmlLogFormatter.Default;
		this.Predication = new LoggerPredication(this.Name) { MinLevel = LogLevel.Info };
	}
	#endregion
}

public abstract class TextFileLogger<TLog> : FileLogger<TLog, string> where TLog : ILog
{
	#region 构造函数
	protected TextFileLogger(int fileLimit = FILE_LIMIT) : this(TimeSpan.FromSeconds(LOGGING_PERIOD), LOGGING_LIMIT, null, fileLimit) { }
	protected TextFileLogger(string filePath, int fileLimit = FILE_LIMIT) : this(TimeSpan.FromSeconds(LOGGING_PERIOD), LOGGING_LIMIT, filePath, fileLimit) { }
	protected TextFileLogger(TimeSpan period, int capacity = LOGGING_LIMIT) : this(period, capacity, null, FILE_LIMIT) { }
	protected TextFileLogger(TimeSpan period, int capacity, string filePath, int fileLimit = FILE_LIMIT) : base(period, capacity, filePath, fileLimit) { }
	#endregion

	#region 重写方法
	protected override async ValueTask WriteLogsAsync(Stream output, IEnumerable<TLog> logs, CancellationToken cancellation)
	{
		//注意：此处不能关闭stream参数传入的流，该流由基类确保安全释放！
		using(var writer = new StreamWriter(output, System.Text.Encoding.UTF8, 1024 * 16, true))
		{
			foreach(var log in logs)
				await writer.WriteLineAsync(this.Format(log));
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual string Format(TLog log) => this.Formatter.Format(log);
	#endregion
}
