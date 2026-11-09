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
		await using(var writer = new StreamWriter(output, System.Text.Encoding.UTF8, 1024 * 16, true))
		{
			foreach(var log in logs)
				await writer.WriteLineAsync(this.Format(log).AsMemory(), cancellation);
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual string Format(TLog log) => this.Formatter.Format(log);
	#endregion
}
