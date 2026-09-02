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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Storages.Data library.
 *
 * The Zongsoft.Messaging.Storages.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Storages.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Storages.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Storages;

/// <summary>提供关系型数据库消息存储器工厂。</summary>
public sealed class DataMessageStorageFactory : MessageStorageFactoryBase<DataMessageStorage>
{
	#region 静态字段
	public static readonly DataMessageStorageFactory MsSql = new("MsSql");
	public static readonly DataMessageStorageFactory MySql = new("MySql");
	public static readonly DataMessageStorageFactory Sqlite = new("SQLite");
	public static readonly DataMessageStorageFactory PostgreSql = new("PostgreSql");
	#endregion

	#region 成员字段
	private readonly string _driver;
	#endregion

	#region 私有构造
	private DataMessageStorageFactory(string driver) => _driver = driver;
	#endregion

	#region 公共属性
	/// <summary>获取当前工厂对应的数据驱动规范名。</summary>
	public string Driver => _driver;
	#endregion

	#region 重写方法
	protected override DataMessageStorage OnCreate(string name)
	{
		var settings = ApplicationContext.Current?.Configuration.GetConnectionSettings("/Data/ConnectionSettings", name, _driver) ??
		               ApplicationContext.Current?.Configuration.GetConnectionSettings("/Messaging/Storages/ConnectionSettings", name, _driver);

		if(settings == null)
			throw new ConfigurationException(string.Format(Properties.Resources.DataMessageStorageFactory_ConnectionSettingNotFound_Message, name, _driver));

		var provider = ApplicationContext.Current?.Services.Resolve<IDataAccessProvider>();
		if(provider == null)
			throw new ConfigurationException(Properties.Resources.DataMessageStorageFactory_DataAccessProviderUnavailable_Message);

		var accessor = provider.GetAccessor(settings.Name);
		if(accessor == null)
			throw new ConfigurationException(string.Format(Properties.Resources.DataMessageStorageFactory_AccessorCreationFailed_Message, settings.Name));

		return new DataMessageStorage(name, accessor, settings, this.GetPartition(settings.Name));
	}
	#endregion
}
