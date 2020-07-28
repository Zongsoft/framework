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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public class DataSourceSelector : IDataSourceSelector
	{
		#region 单例字段
		public static readonly DataSourceSelector Default = new DataSourceSelector();
		#endregion

		#region 私有构造
		private DataSourceSelector()
		{
		}
		#endregion

		#region 公共方法
		public IDataSource GetSource(IDataAccessContextBase context, IReadOnlyList<IDataSource> sources)
		{
			if(sources == null || sources.Count == 0)
				return null;

			var mode = this.GetAccessMode(context);
			var matches = sources.Where(p => (p.Mode & mode) == mode).ToArray();

			if(matches.Length == 1)
				return matches[0];

			//获取一个随机的下标
			var index = Zongsoft.Common.Randomizer.GenerateInt32() % matches.Length;

			return matches[index];
		}
		#endregion

		#region 私有方法
		private DataAccessMode GetAccessMode(IDataAccessContextBase context)
		{
			switch(context.Method)
			{
				case DataAccessMethod.Exists:
				case DataAccessMethod.Select:
				case DataAccessMethod.Aggregate:
					return DataAccessMode.ReadOnly;
				default:
					return DataAccessMode.WriteOnly;
			}
		}
		#endregion
	}
}
