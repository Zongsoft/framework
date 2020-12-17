/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

using Zongsoft.Services;

namespace Zongsoft.Diagnostics
{
	public abstract class LoggerBase<T> : ILogger<T>
	{
		#region 公共属性
		/// <summary>获取或设置日志格式化器。</summary>
		public ILogFormatter<T> Formatter { get; protected set; }

		/// <summary>获取或设置日志断言。</summary>
		public IPredication<LogEntry> Predication { get; protected set; }
		#endregion

		#region 公共方法
		public void Log(LogEntry entry)
		{
			var predication = this.Predication;

			if(predication == null || predication.Predicate(entry))
				this.OnLog(entry);
		}
		#endregion

		#region 抽象方法
		protected abstract void OnLog(LogEntry entry);
		#endregion
	}
}
