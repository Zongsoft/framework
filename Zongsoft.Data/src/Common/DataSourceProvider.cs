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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data.Common
{
	public class DataSourceProvider : IDataSourceProvider
	{
		#region 单例字段
		public static readonly DataSourceProvider Default = new DataSourceProvider();
		#endregion

		#region 私有构造
		private DataSourceProvider()
		{
		}
		#endregion

		#region 公共方法
		public IEnumerable<IDataSource> GetSources(string name)
		{
			var connectionSettings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingCollection>("/Data/ConnectionStrings");

			if(connectionSettings != null)
			{
				foreach(var connectionSetting in connectionSettings)
				{
					if(string.Equals(connectionSetting.Name, name, StringComparison.OrdinalIgnoreCase) ||
					   connectionSetting.Name.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase))
						yield return new DataSource(connectionSetting);
				}
			}
		}
		#endregion
	}
}
