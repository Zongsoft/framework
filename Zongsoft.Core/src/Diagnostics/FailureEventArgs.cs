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

namespace Zongsoft.Diagnostics
{
	[Serializable]
	public class FailureEventArgs : EventArgs
	{
		#region 成员字段
		private Exception _exception;
		private bool _exceptionHandled;
		#endregion

		#region 构造函数
		public FailureEventArgs(Exception exception) : this(exception, false)
		{
		}

		public FailureEventArgs(Exception exception, bool exceptionHandled)
		{
			_exception = exception;
			_exceptionHandled = exceptionHandled;
		}
		#endregion

		#region 公共属性
		public Exception Exception
		{
			get
			{
				return _exception;
			}
			set
			{
				_exception = value;
			}
		}

		public bool ExceptionHandled
		{
			get
			{
				return _exceptionHandled;
			}
			set
			{
				_exceptionHandled = value;
			}
		}
		#endregion
	}
}
