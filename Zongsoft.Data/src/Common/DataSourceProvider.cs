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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data.Common;

public class DataSourceProvider : IDataSourceProvider
{
	#region 成员字段
	private readonly IDataSource[] _sources;
	#endregion

	#region 构造函数
	public DataSourceProvider(IEnumerable<IConnectionSettings> settings)
	{
		if(settings == null)
			_sources = [];
		else
			_sources = [.. settings.Select(setting => new DataSource(setting))];
	}
	#endregion

	#region 公共方法
	public IEnumerable<IDataSource> GetSources(string name)
	{
		if(_sources != null && _sources.Length > 0)
			return _sources;

		var connectionSettings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("/Data/ConnectionSettings");
		if(connectionSettings == null || connectionSettings.Count == 0)
			return [];

		if(string.IsNullOrEmpty(name))
		{
			name = connectionSettings.Default;

			if(string.IsNullOrEmpty(name))
				return [];
		}

		return connectionSettings
			.Where(setting => string.Equals(setting.Name, name, StringComparison.OrdinalIgnoreCase) ||
			                  setting.Name.StartsWith($"{name}{DataSource.SEPARATOR}", StringComparison.OrdinalIgnoreCase))
			.Select(setting => new DataSource(setting));
	}
	#endregion
}
