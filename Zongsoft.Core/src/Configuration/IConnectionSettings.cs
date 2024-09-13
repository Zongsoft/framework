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

namespace Zongsoft.Configuration
{
	/// <summary>
	/// 表示连接设置的接口。
	/// </summary>
	public interface IConnectionSettings : ISetting, IEquatable<IConnectionSettings>, IEnumerable<KeyValuePair<string, string>>
	{
		/// <summary>获取连接的驱动。</summary>
		IConnectionSettingsDriver Driver { get; set; }

		/// <summary>获取连接设置集。</summary>
		IConnectionSettingsOptions Options { get; }

		/// <summary>判断当前连接是否为指定的驱动。</summary>
		/// <param name="name">指定的驱动名称。</param>
		/// <returns>如果当前连接的驱动是<paramref name="name"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool IsDriver(string name) => ConnectionSettingUtility.IsDriver(this.Driver, name);

		/// <summary>判断当前连接是否为指定的驱动。</summary>
		/// <param name="driver">指定的驱动。</param>
		/// <returns>如果当前连接的驱动是<paramref name="driver"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingUtility.IsDriver(this.Driver, driver);
	}
}
