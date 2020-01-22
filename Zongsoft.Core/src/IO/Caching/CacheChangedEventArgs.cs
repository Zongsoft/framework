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

namespace Zongsoft.Runtime.Caching
{
	[Serializable]
	public class CacheChangedEventArgs : EventArgs
	{
		#region 成员字段
		private CacheChangedReason _reason;
		private string _oldKey;
		private string _newKey;
		private object _oldValue;
		private object _newValue;
		#endregion

		#region 构造函数
		public CacheChangedEventArgs(CacheChangedReason reason, string oldKey, object oldValue, string newKey = null, object newValue = null)
		{
			_reason = reason;
			_oldKey = oldKey;
			_oldValue = oldValue;
			_newKey = newKey;
			_newValue = newValue;
		}
		#endregion

		#region 公共属性
		public CacheChangedReason Reason
		{
			get
			{
				return _reason;
			}
		}

		public string OldKey
		{
			get
			{
				return _oldKey;
			}
		}

		public object OldValue
		{
			get
			{
				return _oldValue;
			}
		}

		public string NewKey
		{
			get
			{
				return _newKey;
			}
		}

		public object NewValue
		{
			get
			{
				return _newValue;
			}
		}
		#endregion
	}
}
