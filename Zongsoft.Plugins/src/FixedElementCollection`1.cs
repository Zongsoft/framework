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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public class FixedElementCollection<T> : FixedElementCollection
	{
		#region 构造函数
		internal protected FixedElementCollection()
		{
		}
		#endregion

		#region 公共属性
		public FixedElementType ElementType
		{
			get
			{
				if(typeof(IParser).IsAssignableFrom(typeof(T)))
					return FixedElementType.Parser;
				if(typeof(IBuilder).IsAssignableFrom(typeof(T)))
					return FixedElementType.Builder;

				throw new PluginException();
			}
		}

		public FixedElement<T> this[int index]
		{
			get
			{
				return (FixedElement<T>)this.Get(index);
			}
		}

		public FixedElement<T> this[string name]
		{
			get
			{
				return (FixedElement<T>)this.Get(name);
			}
		}
		#endregion

		#region 公共方法
		public FixedElement<T> Add(string typeName, string name, Plugin plugin)
		{
			FixedElement<T> item = new FixedElement<T>(typeName, name, plugin, this.ElementType);

			base.Insert(item, -1);

			return item;
		}

		public FixedElement<T> Add(Type type, string name, Plugin plugin)
		{
			FixedElement<T> item = new FixedElement<T>(type, name, plugin, this.ElementType);

			base.Insert(item, -1);

			return item;
		}
		#endregion
	}
}
