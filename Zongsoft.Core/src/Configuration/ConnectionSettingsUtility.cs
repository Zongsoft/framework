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

namespace Zongsoft.Configuration;

public static class ConnectionSettingsUtility
{
	#region 公共方法
	/// <summary>获取指定的连接配置。</summary>
	/// <param name="configuration">指定的配置信息。</param>
	/// <param name="path">指定的连接配置集的配置路径。</param>
	/// <param name="name">指定要查找的连接配置项名称，如果为空或空字符串则表示查找默认连接配置。</param>
	/// <param name="driver">指定要查找的连接驱动标识，如果为空或空字符串则表示忽略连接驱动匹配。</param>
	/// <returns>如果查找成功则返回找到的连接配置，否则返回空。</returns>
	public static ConnectionSettings GetConnectionSettings(this Microsoft.Extensions.Configuration.IConfiguration configuration, string path, string name, string driver = null)
	{
		if(configuration == null)
			return null;

		//获取指定路径的连接配置集
		var settings = configuration.GetOption<ConnectionSettingsCollection>(string.IsNullOrEmpty(path) ? "ConnectionSettings" : path);
		if(settings == null || settings.Count == 0)
			return null;

		//如果指定的连接配置名为空则获取默认项
		var setting = string.IsNullOrEmpty(name) ? settings.GetDefault() : (settings.TryGetValue(name, out var value) ? value : null);

		//如果指定了连接驱动参数，则必须确保找到的连接驱动等于该驱动
		if(setting != null && !string.IsNullOrEmpty(driver))
			return setting.Driver != null && setting.IsDriver(driver) ? setting : null;

		return setting;
	}
	#endregion

	#region 内部方法
	/// <summary>判断指定连接是否为指定的驱动。</summary>
	/// <param name="settings">指定的连接设置。</param>
	/// <param name="name">指定的待判断的驱动名称。</param>
	/// <returns>如果指定连接的驱动是<paramref name="name"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	internal static bool IsDriver(IConnectionSettings settings, string name)
	{
		var driver = settings.Driver;

		if(string.IsNullOrEmpty(name))
			return driver == null || driver.IsDriver(name);
		else
			return driver != null && driver.IsDriver(name);
	}

	/// <summary>判断指定连接是否为指定的驱动。</summary>
	/// <param name="settings">指定的连接设置。</param>
	/// <param name="other">指定的待判断的驱动。</param>
	/// <returns>如果指定连接的驱动是<paramref name="settings"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	internal static bool IsDriver(IConnectionSettings settings, IConnectionSettingsDriver other)
	{
		var driver = settings.Driver;

		if(other == null)
			return driver == null || driver.IsDriver(null);
		else
			return driver != null && driver.IsDriver(other.Name);
	}
	#endregion
}