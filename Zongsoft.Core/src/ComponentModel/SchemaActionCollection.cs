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
using System.Collections.ObjectModel;

namespace Zongsoft.ComponentModel
{
	public class SchemaActionCollection : KeyedCollection<string, SchemaAction>
	{
		#region 构造函数
		public SchemaActionCollection() : base(StringComparer.OrdinalIgnoreCase) { }
		public SchemaActionCollection(Schema schema) : base(StringComparer.OrdinalIgnoreCase) => this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
		#endregion

		#region 公共属性
		public Schema Schema { get; }
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(SchemaAction item) => item.Name;

		protected override void InsertItem(int index, SchemaAction item)
		{
			base.InsertItem(index, item);

			var alias = item.Alias;
			if(alias != null && alias.Length > 0)
			{
				for(int i = 0; i < alias.Length; i++)
					base.Dictionary.TryAdd(alias[i], item);
			}
		}

		protected override void SetItem(int index, SchemaAction item)
		{
			var original = base.Items[index];

			if(original != null)
			{
				var alias = original.Alias;
				if(alias != null && alias.Length > 0)
				{
					for(int i = 0; i < alias.Length; i++)
					{
						if(!this.Comparer.Equals(original.Name, alias[i]))
							this.Remove(alias[i]);
					}
				}
			}

			base.SetItem(index, item);

			if(item != null)
			{
				var alias = item.Alias;
				if(alias != null && alias.Length > 0)
				{
					for(int i = 0; i < alias.Length; i++)
						base.Dictionary.TryAdd(alias[i], item);
				}
			}
		}

		protected override void RemoveItem(int index)
		{
			var item = base.Items[index];

			if(item != null)
			{
				base.RemoveItem(index);

				var alias = item.Alias;
				if(alias != null && alias.Length > 0)
				{
					for(int i = 0; i < alias.Length; i++)
					{
						if(!this.Comparer.Equals(item.Name, alias[i]))
							this.Remove(alias[i]);
					}
				}
			}
		}
		#endregion
	}
}
