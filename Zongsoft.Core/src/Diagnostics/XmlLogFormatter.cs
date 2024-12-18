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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Diagnostics
{
	public class XmlLogFormatter : ILogFormatter<string>
	{
		#region 单例字段
		public static readonly XmlLogFormatter Instance = new XmlLogFormatter();
		#endregion

		#region 私有构造
		private XmlLogFormatter() { }
		#endregion

		#region 公共属性
		public string Name { get => "xml"; }
		#endregion

		#region 公共方法
		public string Format(LogEntry entry)
		{
			if(entry == null)
				return null;

			var builder = new StringBuilder(512);

			builder.Append($"<log level=\"{entry.Level}\" source=\"{entry.Source}\" timestamp=\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\">");
			builder.AppendLine();
			builder.AppendLine($"\t<message><![CDATA[{entry.Message}]]></message>");

			if(entry.Exception is AggregateException aggregateException)
			{
				if(aggregateException.InnerExceptions != null && aggregateException.InnerExceptions.Count > 0)
				{
					foreach(var exception in aggregateException.InnerExceptions)
						WriteException(builder, exception);
				}
				else
				{
					WriteException(builder, entry.Exception);
				}
			}
			else
			{
				WriteException(builder, entry.Exception);
			}

			if(entry.Data != null)
			{
				builder.AppendLine();
				builder.AppendLine($"\t<data type=\"{Common.TypeAlias.GetAlias(entry.Data.GetType())}\">");
				builder.AppendLine("\t<![CDATA[");

				if(entry.Data is byte[] bytes)
					builder.AppendLine(Convert.ToBase64String(bytes));
				else
					builder.AppendLine(System.Text.Json.JsonSerializer.Serialize(entry.Data));

				builder.AppendLine("\t]]>");
				builder.AppendLine("\t</data>");
			}

			if(!string.IsNullOrEmpty(entry.StackTrace))
			{
				builder.AppendLine();
				builder.AppendLine("\t<stackTrace>");
				builder.AppendLine("\t<![CDATA[");
				builder.AppendLine(entry.StackTrace);
				builder.AppendLine("\t]]>");
				builder.AppendLine("\t</stackTrace>");
			}

			builder.AppendLine("</log>");
			return builder.ToString();
		}
		#endregion

		#region 私有方法
		private static void WriteException(StringBuilder builder, Exception exception)
		{
			if(builder == null || exception == null)
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
}
