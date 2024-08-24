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
using System.Collections.Generic;

namespace Zongsoft.Diagnostics
{
	public class ConsoleLogger : LoggerBase<string>
	{
		#region 公共方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		protected override void OnLog(LogEntry entry)
		{
			if(entry == null)
				return;

			//根据日志级别来调整控制台的前景色
			switch(entry.Level)
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
				Console.WriteLine(this.Format(entry));
			}
			finally
			{
				//恢复默认颜色
				Console.ResetColor();
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual string Format(LogEntry entry)
		{
			return (this.Formatter ?? XmlLogFormatter.Instance).Format(entry);
		}
		#endregion
	}
}
