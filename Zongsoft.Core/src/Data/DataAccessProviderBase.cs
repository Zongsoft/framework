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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data;

[Service<IDataAccessProvider, IServiceProvider<IDataAccess>>]
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
	public TDataAccess GetAccessor(string name, IDataAccessOptions options = null)
	{
		if(string.IsNullOrEmpty(name) || options == null || options.Settings == null || !options.Settings.Any())
			name = GetName(name);

		return _accesses.GetOrCreate(name, key =>
		{
			var accessor = this.CreateAccessor(name, options);
			return (accessor, accessor.Disposed);
		});
	}
	#endregion

	#region 抽象方法
	protected abstract TDataAccess CreateAccessor(string name, IDataAccessOptions options);
	#endregion

	#region 私有方法
	private static string GetName(string name)
	{
		var connectionSettings = GetConnectionSettings() ??
			throw new DataException(Properties.Resources.DataAccess_MissingConnections_Message);

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

	private static ConnectionSettingsCollection GetConnectionSettings() => ApplicationContext.Current?.Configuration?.GetOption<ConnectionSettingsCollection>("/Data/ConnectionSettings");
	#endregion

	#region 显式实现
	TDataAccess IServiceProvider<TDataAccess>.GetService(string name) => this.GetAccessor(name);
	IDataAccess IDataAccessProvider.GetAccessor(string name) => this.GetAccessor(name);
	IDataAccess IDataAccessProvider.GetAccessor(string name, IDataAccessOptions options) => this.GetAccessor(name, options);
	#endregion
}
