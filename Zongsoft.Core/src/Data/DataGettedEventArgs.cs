﻿/*
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

namespace Zongsoft.Data;

/// <summary>
/// 为数据服务的获取事件提供数据。
/// </summary>
public class DataGettedEventArgs<T> : DataAccessEventArgs<DataSelectContextBase>
{
	#region 构造函数
	public DataGettedEventArgs(DataSelectContextBase context) : base(context) { }
	#endregion

	#region 公共属性
	/// <summary>
	/// 获取查询操作的首条数据。
	/// </summary>
	public T Result
	{
		get
		{
			var items = this.Context.Result;

			if(items != null)
			{
				var iterator = items.GetEnumerator();

				if(iterator != null && iterator.MoveNext())
					return (T)iterator.Current;
			}

			return default;
		}
	}
	#endregion
}
