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

namespace Zongsoft.ComponentModel
{
	public class SchemaActionCollection : Zongsoft.Collections.NamedCollectionBase<SchemaAction>
	{
		#region 私有变量
		private readonly Dictionary<string, SchemaAction> _alias;
		#endregion

		#region 构造函数
		public SchemaActionCollection() : base(StringComparer.OrdinalIgnoreCase)
		{
			_alias = new Dictionary<string, SchemaAction>(StringComparer.OrdinalIgnoreCase);
		}

		public SchemaActionCollection(Schema schema) : base(StringComparer.OrdinalIgnoreCase)
		{
			_alias = new Dictionary<string, SchemaAction>(StringComparer.OrdinalIgnoreCase);
			this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
		}
		#endregion

		#region 公共属性
		public Schema Schema
		{
			get;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(SchemaAction item)
		{
			return item.Name;
		}

		protected override bool TryGetItem(string name, out SchemaAction value)
		{
			return base.TryGetItem(name, out value) || _alias.TryGetValue(name, out value);
		}

		protected override void AddItem(SchemaAction item)
		{
			//调用基类同名方法
			base.AddItem(item);

			var alias = item.Alias;

			if(alias != null && alias.Length > 0)
			{
				for(int i = 0; i < alias.Length; i++)
				{
					_alias.Add(alias[i], item);
				}
			}
		}

		protected override void SetItem(string name, SchemaAction value)
		{
			//调用基类同名方法
			base.SetItem(name, value);

			var alias = value.Alias;

			if(alias != null && alias.Length > 0)
			{
				for(int i = 0; i < alias.Length; i++)
				{
					_alias[alias[i]] = value;
				}
			}
		}

		protected override bool RemoveItem(string name)
		{
			if(base.InnerDictionary.TryGetValue(name, out var item) && base.RemoveItem(name))
			{
				var alias = item.Alias;

				if(alias != null && alias.Length > 0)
				{
					for(int i = 0; i < alias.Length; i++)
					{
						_alias.Remove(alias[i]);
					}
				}

				return true;
			}

			return false;
		}

		protected override void ClearItems()
		{
			//调用基类同名方法
			base.ClearItems();

			//清空别名集
			_alias.Clear();
		}
		#endregion
	}
}
