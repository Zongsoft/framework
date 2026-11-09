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
