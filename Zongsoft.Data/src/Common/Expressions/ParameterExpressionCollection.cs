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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Common.Expressions
{
	public class ParameterExpressionCollection() : KeyedCollection<string, ParameterExpression>(StringComparer.OrdinalIgnoreCase)
	{
		#region 私有变量
		private int _index;
		#endregion

		#region 公共方法
		public ParameterExpression Add(string name, System.Data.DbType type, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input)
		{
			var parameter = Expression.Parameter(name, type, direction);
			this.Add(parameter);
			return parameter;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(ParameterExpression item) => item.Name;
		protected override void InsertItem(int index, ParameterExpression item)
		{
			//处理下参数名为空或问号(?)的情况
			if(string.IsNullOrEmpty(item.Name) || item.Name == ParameterExpression.Anonymous)
				item.Name = "p" + System.Threading.Interlocked.Increment(ref _index).ToString();

			//调用基类同名方法
			base.InsertItem(index, item);
		}
		#endregion
	}
}
