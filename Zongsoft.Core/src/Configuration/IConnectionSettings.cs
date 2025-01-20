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
		#region 普通属性
		/// <summary>获取连接的驱动。接口实现者应确保该属性值不会为空(<c>null</c>)。</summary>
		IConnectionSettingsDriver Driver { get; }
		/// <summary>获取连接设置的原始值集。</summary>
		IDictionary<string, string> Values { get; }

		/// <summary>获取或设置指定键名的值。</summary>
		/// <param name="name">指定的连接设置项的键名。</param>
		/// <returns>返回指定键名的设置值，如果为空(<c>null</c>)则表示指定键名的设置项不存在。</returns>
		object this[string name] { get; set; }
		#endregion

		#region 特定属性
		/// <summary>获取或设置分组标识。</summary>
		string Group { get; set; }
		/// <summary>获取或设置客户端标识。</summary>
		string Client { get; set; }
		/// <summary>获取或设置服务器地址。</summary>
		string Server { get; set; }
		/// <summary>获取或设置端口号。</summary>
		ushort Port { get; set; }
		/// <summary>获取或设置超时。</summary>
		TimeSpan Timeout { get; set; }
		/// <summary>获取或设置字符集。</summary>
		string Charset { get; set; }
		/// <summary>获取或设置字符编码。</summary>
		string Encoding { get; set; }
		/// <summary>获取或设置提供程序。</summary>
		string Provider { get; set; }
		/// <summary>获取或设置数据库名。</summary>
		string Database { get; set; }
		/// <summary>获取或设置连接账户。</summary>
		string UserName { get; set; }
		/// <summary>获取或设置连接密码。</summary>
		string Password { get; set; }
		/// <summary>获取或设置实例标识。</summary>
		string Instance { get; set; }
		/// <summary>获取或设置应用标识。</summary>
		string Application { get; set; }
		#endregion

		#region 方法定义
		/// <summary>构建当前连接设置的选项实例。</summary>
		/// <typeparam name="TOptions">泛型参数，指示构建的选项类型。</typeparam>
		/// <returns>返回构建成功的选项实例。</returns>
		TOptions GetOptions<TOptions>();

		/// <summary>判断指定名称的连接设置项是否存在。</summary>
		/// <param name="name">指定要判断的设置项名称。</param>
		/// <returns>如果指定名称的设置项存在则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool Contains(string name);

		/// <summary>获取指定名称的连接设置项的值。</summary>
		/// <typeparam name="T">泛型参数，指示要转换的设置项值的类型。</typeparam>
		/// <param name="name">指定要获取的设置项名称。</param>
		/// <param name="defaultValue">指定的设置项默认值，如果指定名称的设置项不存在则返回该默认值。</param>
		/// <returns>如果指定名称的设置项存在则返回其对应的设置值，否则返回<paramref name="defaultValue"/>参数指定的默认值。</returns>
		T GetValue<T>(string name, T defaultValue = default);

		/// <summary>尝试获取指定名称的连接设置项的值。</summary>
		/// <param name="name">指定要获取的设置项名称。</param>
		/// <param name="value">输出参数，表示获取成功的设置项值。</param>
		/// <returns>如果指定名称的设置项存在则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool TryGetValue(string name, out object value);
		#endregion

		#region 默认实现
		/// <summary>判断当前连接是否为指定的驱动。</summary>
		/// <param name="name">指定的驱动名称。</param>
		/// <returns>如果当前连接的驱动是<paramref name="name"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);

		/// <summary>判断当前连接是否为指定的驱动。</summary>
		/// <param name="driver">指定的驱动。</param>
		/// <returns>如果当前连接的驱动是<paramref name="driver"/>参数指定的驱动则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);
		#endregion
	}
}
