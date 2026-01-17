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

using Zongsoft.Components;

namespace Zongsoft.Diagnostics;

public sealed class ConsoleFormatter : ConsoleFormatter<LogEntry>
{
	#region 单例字段
	public static readonly ConsoleFormatter Default = new();
	#endregion

	#region 私有构造
	private ConsoleFormatter() { }
	#endregion

	#region 重写方法
	protected override void OnFormatContent(CommandOutletContent content, LogEntry log)
	{
		base.OnFormatContent(content, log);

		if(log.Data != null)
		{
			content.Last
				.AppendLine()
				.Append("\t<data type=")
				.Append(this.GetColor(log.Level), Common.TypeAlias.GetAlias(log.Data.GetType()))
				.AppendLine(">");

			content.Dump(log.Data, 2);
			content.Last.AppendLine("\t</data>");
		}
	}
	#endregion
}

public abstract class ConsoleFormatter<TLog> : ILogFormatter<TLog, CommandOutletContent> where TLog : ILog
{
	#region 构造函数
	protected ConsoleFormatter() { }
	#endregion

	#region 公共属性
	public string Name => "Console";
	#endregion

	#region 公共方法
	public CommandOutletContent Format(TLog log)
	{
		if(log == null)
			return null;

		var result = CommandOutletContent.Create();

		this.OnFormatting(result, log);
		this.OnFormatContent(result, log);
		this.OnFormatted(result, log);

		return result;
	}
	#endregion

	#region 虚拟方法
	protected virtual CommandOutletColor GetColor(LogLevel level) => level switch
	{
		LogLevel.Trace => CommandOutletColor.DarkBlue,
		LogLevel.Debug => CommandOutletColor.DarkCyan,
		LogLevel.Info => CommandOutletColor.DarkGreen,
		LogLevel.Warn => CommandOutletColor.DarkYellow,
		LogLevel.Error => CommandOutletColor.DarkRed,
		LogLevel.Fatal => CommandOutletColor.Magenta,
		_ => CommandOutletColor.DarkGray,
	};

	protected virtual void OnFormatting(CommandOutletContent content, TLog log)
	{
		var color = this.GetColor(log.Level);

		content.Append("<log level=")
			.Append(CommandOutletStyles.Blinking, color, log.Level)
			.Append(" source=")
			.Append(color, log.Source)
			.Append(" timestamp=")
			.Append(color, log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffK"))
			.AppendLine(">");
	}

	protected virtual void OnFormatted(CommandOutletContent content, TLog log)
	{
		content.Last.AppendLine("</log>");
	}

	protected virtual void OnFormatContent(CommandOutletContent content, TLog log)
	{
		var color = this.GetColor(log.Level);

		content.Last
			.Append("\t<message>")
			.Append(color, log.Message)
			.AppendLine("</message>");

		if(log.Exception is AggregateException aggregateException)
		{
			if(aggregateException.InnerExceptions != null && aggregateException.InnerExceptions.Count > 0)
			{
				foreach(var exception in aggregateException.InnerExceptions)
					DumpException(content, exception, color);
			}
			else
			{
				DumpException(content, log.Exception, color);
			}
		}
		else
		{
			DumpException(content, log.Exception, color);
		}
	}
	#endregion

	#region 私有方法
	private static void DumpException(CommandOutletContent content, Exception exception, CommandOutletColor color)
	{
		if(exception == null)
			return;

		content.Last
			.AppendLine()
			.Append("\t<exception type=")
			.Append(color, Common.TypeAlias.GetAlias(exception.GetType()))
			.AppendLine(">");

		if(!string.IsNullOrEmpty(exception.Message))
			content.Last
				.Append("\t\t<message>")
				.Append(color, exception.Message)
				.AppendLine("</message>");

		if(!string.IsNullOrEmpty(exception.StackTrace))
		{
			content.Last
				.AppendLine("\t\t<stackTrace>")
				.AppendLine(color, exception.StackTrace)
				.AppendLine("\t\t</stackTrace>");
		}

		content.Last.AppendLine("\t</exception>");

		if(exception.InnerException != null && exception.InnerException != exception)
			DumpException(content, exception.InnerException, color);
	}
	#endregion
}
