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

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data
{
	[Service(typeof(IServiceProvider<IDataAccess>))]
	public abstract class DataAccessProviderBase<TDataAccess> : IDataAccessProvider, IServiceProvider<TDataAccess> where TDataAccess : class, IDataAccess
	{
		#region 成员字段
		private readonly MemoryCache _accesses = new();
		#endregion

		#region 公共属性
		public int Count => _accesses.Count;
		#endregion

		#region 公共方法
		public TDataAccess GetAccessor(string name = null) => this.GetAccessor(name, null);
		public TDataAccess GetAccessor(string name, params IEnumerable<IConnectionSettings> settings)
		{
			if(string.IsNullOrEmpty(name) || settings == null)
				name = GetName(name);

			return _accesses.GetOrCreate(name, key =>
			{
				var accessor = this.CreateAccessor(name, settings);
				return (accessor, accessor.Disposed);
			});
		}
		#endregion

		#region 抽象方法
		protected abstract TDataAccess CreateAccessor(string name, params IEnumerable<IConnectionSettings> settings);
		#endregion

		#region 私有方法
		private static string GetName(string name)
		{
			var connectionSettings = GetConnectionSettings() ??
				throw new DataException($"Missing database connection settings.");

			if(!string.IsNullOrEmpty(name) && connectionSettings.Contains(name))
				return name;

			var defaultSetting = connectionSettings.GetDefault();
			if(defaultSetting != null)
				return defaultSetting.Name;

			throw new DataException(
				string.IsNullOrEmpty(name) ?
				$"Missing the default database connection setting." :
				$"The specified '{name}' database connection setting does not exist and the default database connection setting is not defined."
			);
		}

		private static ConnectionSettingsCollection GetConnectionSettings() => ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("/Data/ConnectionSettings");
		#endregion

		#region 显式实现
		TDataAccess IServiceProvider<TDataAccess>.GetService(string name) => this.GetAccessor(name);
		IDataAccess IDataAccessProvider.GetAccessor(string name) => this.GetAccessor(name);
		IDataAccess IDataAccessProvider.GetAccessor(string name, params IEnumerable<IConnectionSettings> settings) => this.GetAccessor(name, settings);
		#endregion
	}
}
