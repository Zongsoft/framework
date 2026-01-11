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

public sealed class ConsoleLogger : ConsoleLogger<LogEntry>
{
	#region 单例字段
	public static readonly ConsoleLogger Instance = new();
	#endregion

	#region 重写方法
	protected override string Format(LogEntry log) => (this.Formatter ?? XmlLogFormatter.Default).Format(log);
	#endregion
}

public abstract class ConsoleLogger<TLog> : LoggerBase<TLog, string> where TLog : ILog
{
	#region 重写方法
	protected override async ValueTask<bool> CanLogAsync(TLog log, CancellationToken cancellation)
	{
		return IsTerminalApplication() && await base.CanLogAsync(log, cancellation);
	}

	protected override ValueTask OnLogAsync(TLog log, CancellationToken cancellation)
	{
		if(log == null)
			return ValueTask.CompletedTask;

		//根据日志级别来调整控制台的前景色
		switch(log.Level)
		{
			case LogLevel.Trace:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			case LogLevel.Debug:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
			case LogLevel.Warn:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case LogLevel.Error:
			case LogLevel.Fatal:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
		}

		try
		{
			//打印日志信息
			Console.WriteLine(this.Format(log));
		}
		finally
		{
			//恢复默认颜色
			Console.ResetColor();
		}

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 虚拟方法
	protected abstract string Format(TLog log);
	#endregion

	#region 私有方法
	private static bool IsTerminalApplication() =>
		string.Equals(Zongsoft.Services.ApplicationContext.Current.ApplicationType, "Console", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(Zongsoft.Services.ApplicationContext.Current.ApplicationType, "Terminal", StringComparison.OrdinalIgnoreCase);
	#endregion
}
