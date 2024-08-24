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

namespace Zongsoft.Services
{
	/// <summary>
	/// 表示命令会话执行完成的上下文类。
	/// </summary>
	public class CommandCompletionContext : CommandContext
	{
		#region 成员字段
		private object _result;
		private Exception _exception;
		#endregion

		#region 构造函数
		public CommandCompletionContext(CommandContext context, object result, Exception exception = null) : base(context)
		{
			_result = result;
			_exception = exception;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前命令的执行结果。
		/// </summary>
		public object Result
		{
			get
			{
				return _result;
			}
		}

		/// <summary>
		/// 获取命令执行中发生的异常。
		/// </summary>
		public Exception Exception
		{
			get
			{
				return _exception;
			}
			internal set
			{
				_exception = value;
			}
		}
		#endregion
	}
}
