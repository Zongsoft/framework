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

namespace Zongsoft.Caching
{
	[Serializable]
	public class CacheChangedEventArgs : EventArgs
	{
		#region 构造函数
		public CacheChangedEventArgs(CacheChangedReason reason, string key, object oldValue, object newValue = null)
		{
			this.Reason = reason;
			this.Key = key;
			this.NewValue = newValue;
            this.OldValue = oldValue;
        }
        #endregion

        #region 公共属性
        public CacheChangedReason Reason { get; }

		public string Key { get; }

		public object OldValue { get; }

		public object NewValue { get; }
        #endregion

        #region 静态方法
        public static CacheChangedEventArgs Updated(string key, object newValue, object oldValue)
        {
            return new CacheChangedEventArgs(CacheChangedReason.Updated, key, oldValue: oldValue, newValue:newValue);
        }

        public static CacheChangedEventArgs Removed(string key, object value)
        {
            return new CacheChangedEventArgs(CacheChangedReason.Removed, key, oldValue: value);
        }

        public static CacheChangedEventArgs Expired(string key, object value)
        {
            return new CacheChangedEventArgs(CacheChangedReason.Expired, key, oldValue: value);
        }
		#endregion
	}
}
