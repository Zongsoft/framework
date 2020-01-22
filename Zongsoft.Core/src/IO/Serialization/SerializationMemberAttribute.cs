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

namespace Zongsoft.Runtime.Serialization
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
	public class SerializationMemberAttribute : Attribute
	{
		#region 构造函数
		public SerializationMemberAttribute()
		{
		}

		public SerializationMemberAttribute(string name)
		{
			this.Name = name == null ? string.Empty : name.Trim();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置序列化后的成员名称，如果为空(null)或空字符串("")则取对应的成员本身的名称。
		/// </summary>
		public string Name
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置成员序列化方向。
		/// </summary>
		public SerializationDirection Direction
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置是否忽略序列化成员。
		/// </summary>
		public bool Ignored
		{
			get; set;
		}
		#endregion
	}
}
