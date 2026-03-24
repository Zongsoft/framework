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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Velopack.Configuration;

public class VelopackConnectionSettingsDriver : ConnectionSettingsDriver<VelopackConnectionSettings>
{
	#region 常量定义
	internal const string NAME = "Velopack";
	internal const string PATH = "Externals/Velopack";
	#endregion

	#region 单例字段
	public static readonly VelopackConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private VelopackConnectionSettingsDriver() : base(NAME) { }
	#endregion

	#region 公共方法
	public static VelopackConnectionSettings GetConnectionSettings(string name = null) => GetConnectionSettings(ApplicationContext.Current?.Configuration, name);
	public static VelopackConnectionSettings GetConnectionSettings(Microsoft.Extensions.Configuration.IConfiguration configuration, string name = null)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		if(string.IsNullOrEmpty(name))
			name = ApplicationContext.Current?.Name;

		var settings = configuration.GetConnectionSettings(PATH, name, NAME);
		if(settings == null && !string.IsNullOrEmpty(name))
			settings = configuration.GetConnectionSettings(PATH, null, NAME);

		return settings?.Driver?.GetSettings(settings.Name, settings.Value) as VelopackConnectionSettings;
	}
	#endregion
}
