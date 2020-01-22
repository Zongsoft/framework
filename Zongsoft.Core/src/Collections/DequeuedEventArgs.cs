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

namespace Zongsoft.Collections
{
	[Serializable]
	public class DequeuedEventArgs : EventArgs
	{
		#region 成员变量
		private object _value;
		private bool _isMultiple;
		private CollectionRemovedReason _reason;
		#endregion

		#region 构造函数
		public DequeuedEventArgs(object value, bool isMultiple, CollectionRemovedReason reason)
		{
			_value = value;
			_isMultiple = isMultiple;
			_reason = reason;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取出队的内容值。
		/// </summary>
		public object Value
		{
			get
			{
				return _value;
			}
		}

		/// <summary>
		/// 获取一个指示本次出队是否为批量出队操作，如果为批量出队则<see cref="Value"/>属性返回的则是多值。
		/// </summary>
		public bool IsMultiple
		{
			get
			{
				return _isMultiple;
			}
		}

		/// <summary>
		/// 获取出队事件被激发的原因。
		/// </summary>
		public CollectionRemovedReason Reason
		{
			get
			{
				return _reason;
			}
		}
		#endregion
	}
}
