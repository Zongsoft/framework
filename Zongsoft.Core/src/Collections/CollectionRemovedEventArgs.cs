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

namespace Zongsoft.Collections
{
	public class CollectionRemovedEventArgs : EventArgs
	{
		#region 成员字段
		private CollectionRemovedReason _reason;
		private object _item;
		#endregion

		#region 构造函数
		public CollectionRemovedEventArgs(CollectionRemovedReason reason, object item)
		{
			_reason = reason;
			_item = item;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取被删除的原因。
		/// </summary>
		public CollectionRemovedReason Reason
		{
			get
			{
				return _reason;
			}
		}

		/// <summary>
		/// 获取被删除的集合元素。
		/// </summary>
		public object Item
		{
			get
			{
				return _item;
			}
		}
		#endregion
	}
}
