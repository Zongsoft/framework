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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data.Common
{
	public class DataSourceProvider : IDataSourceProvider
	{
		#region 成员字段
		private readonly List<IDataSource> _sources;
		#endregion

		#region 构造函数
		public DataSourceProvider(IEnumerable<IConnectionSettings> settings)
		{
			if(settings == null)
				_sources = new();
			else
				_sources = new(settings.Select(setting => new DataSource(setting)));
		}
		#endregion

		#region 公共方法
		public IEnumerable<IDataSource> GetSources(string name)
		{
			if(_sources == null || _sources.Count == 0)
			{
				var connectionSettings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("/Data/ConnectionSettings");

				if(connectionSettings == null || connectionSettings.Count == 0)
					return [];

				if(string.IsNullOrEmpty(name))
				{
					name = connectionSettings.Default;

					if(string.IsNullOrEmpty(name))
						return [];
				}

				foreach(var connectionSetting in connectionSettings)
				{
					if(string.Equals(connectionSetting.Name, name, StringComparison.OrdinalIgnoreCase) ||
					   connectionSetting.Name.StartsWith(name + DataSource.SEPARATOR, StringComparison.OrdinalIgnoreCase))
						_sources.Add(new DataSource(connectionSetting));
				}
			}

			return _sources;
		}
		#endregion
	}
}
