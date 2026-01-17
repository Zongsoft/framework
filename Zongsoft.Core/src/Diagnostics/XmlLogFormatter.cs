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
using System.Text;

namespace Zongsoft.Diagnostics;

public sealed class XmlLogFormatter : XmlLogFormatter<LogEntry>
{
	#region 单例字段
	public static readonly XmlLogFormatter Default = new();
	#endregion

	#region 私有构造
	private XmlLogFormatter() { }
	#endregion

	#region 重写方法
	protected override void OnFormatContent(StringBuilder builder, LogEntry log)
	{
		base.OnFormatContent(builder, log);

		if(log.Data != null)
		{
			builder.AppendLine();
			builder.AppendLine($"\t<data type=\"{Common.TypeAlias.GetAlias(log.Data.GetType())}\">");
			builder.AppendLine("\t<![CDATA[");

			var content = log.Data switch
			{
				byte[] data => Convert.ToBase64String(data),
				string text => text,
				_ => GetJson(log.Data),
			};

			if(!string.IsNullOrEmpty(content))
				builder.AppendLine(content);

			builder.AppendLine("\t]]>");
			builder.AppendLine("\t</data>");
		}

		static string GetJson(object data)
		{
			try
			{
				return Zongsoft.Serialization.Serializer.Json.Serialize(data);
			}
			catch { return null; }
		}
	}
	#endregion
}

public class XmlLogFormatter<TLog> : ILogFormatter<TLog, string> where TLog : ILog
{
	#region 构造函数
	protected XmlLogFormatter() { }
	#endregion

	#region 公共属性
	public string Name => "Xml";
	#endregion

	#region 公共方法
	public string Format(TLog log)
	{
		if(log == null)
			return null;

		var builder = new StringBuilder(512);

		this.OnFormatting(builder, log);
		this.OnFormatContent(builder, log);
		this.OnFormatted(builder, log);

		return builder.ToString();
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnFormatting(StringBuilder builder, TLog log)
	{
		builder.AppendLine($"<log level=\"{log.Level}\" source=\"{log.Source}\" timestamp=\"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fffK}\">");
	}

	protected virtual void OnFormatted(StringBuilder builder, TLog log)
	{
		builder.AppendLine("</log>");
	}

	protected virtual void OnFormatContent(StringBuilder builder, TLog log)
	{
		builder.AppendLine($"\t<message><![CDATA[{log.Message}]]></message>");

		if(log.Exception is AggregateException aggregateException)
		{
			if(aggregateException.InnerExceptions != null && aggregateException.InnerExceptions.Count > 0)
			{
				foreach(var exception in aggregateException.InnerExceptions)
					WriteException(builder, exception);
			}
			else
			{
				WriteException(builder, log.Exception);
			}
		}
		else
		{
			WriteException(builder, log.Exception);
		}
	}
	#endregion

	#region 私有方法
	private static void WriteException(StringBuilder builder, Exception exception)
	{
		if(exception == null)
			return;

		builder.AppendLine();
		builder.AppendLine($"\t<exception type=\"{Common.TypeAlias.GetAlias(exception.GetType())}\">");

		if(!string.IsNullOrEmpty(exception.Message))
			builder.AppendLine($"\t\t<message><![CDATA[{exception.Message}]]></message>");

		if(!string.IsNullOrEmpty(exception.StackTrace))
		{
			builder.AppendLine("\t\t<stackTrace>");
			builder.AppendLine("\t\t<![CDATA[");
			builder.AppendLine(exception.StackTrace);
			builder.AppendLine("\t\t]]>");
			builder.AppendLine("\t\t</stackTrace>");
		}

		builder.AppendLine("\t</exception>");

		if(exception.InnerException != null && exception.InnerException != exception)
			WriteException(builder, exception.InnerException);
	}
	#endregion
}
