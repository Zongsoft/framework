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
using System.Collections.Generic;

namespace Zongsoft.ComponentModel
{
	[Obsolete]
	public class SchemaProvider : ISchemaProvider
	{
		#region 成员变量
		private SchemaCategory _category;
		private SchemaCollection _schemas;
		private DateTime _lastLoadTime;
		#endregion

		#region 构造函数
		public SchemaProvider()
		{
			_lastLoadTime = DateTime.MinValue;
		}

		public SchemaProvider(SchemaCategory category)
		{
			if(category == null)
				throw new ArgumentNullException("category");

			_category = category;
			_lastLoadTime = DateTime.MinValue;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置应用程序的总<see cref="SchemaCategory"/>目录。
		/// </summary>
		public SchemaCategory Category
		{
			get
			{
				if(_category == null)
					System.Threading.Interlocked.CompareExchange(ref _category, new SchemaCategory(), null);

				return _category;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_category = value;
			}
		}
		#endregion

		#region 公共方法
		public SchemaCollection GetSchemas()
		{
			if(_schemas == null)
			{
				var schemas = System.Threading.Interlocked.CompareExchange(ref _schemas, new SchemaCollection(), null);

				if(schemas == null)
				{
					//加载Schema到结果集中
					this.LoadSchemas(_schemas, _category);

					//更新最后加载的时间
					_lastLoadTime = DateTime.Now;

					//返回加载完成的模式集
					return _schemas;
				}
			}

			if((DateTime.Now - _lastLoadTime).TotalMinutes > 1)
			{
				//在后续加载中必须先清空原有内容，否则很可能会产生键冲突
				_schemas.Clear();

				//加载Schema到结果集中
				this.LoadSchemas(_schemas, _category);

				//更新最后加载的时间
				_lastLoadTime = DateTime.Now;
			}

			return _schemas;
		}

		public SchemaCategory GetHierarchicalSchemas()
		{
			return this.Category;
		}
		#endregion

		#region 私有方法
		private void LoadSchemas(ICollection<Schema> schemas, SchemaCategory category)
		{
			if(category == null)
				return;

			var items = category.Schemas;

			if(items != null && items.Count > 0)
			{
				foreach(var item in items)
					schemas.Add(item);
			}

			foreach(var child in category.Children)
				this.LoadSchemas(schemas, child);
		}
		#endregion
	}
}
