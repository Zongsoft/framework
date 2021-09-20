/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
	public interface IConnectionSetting : ISetting
	{
		string Driver { get; set; }
		IConnectionSettingValues Values { get; }
	}

	public interface IConnectionSettingValues : IEnumerable<KeyValuePair<string, string>>
	{
		#region 通用属性
		int Count { get; }
		string this[string key] { get; set; }
		#endregion

		#region 特定属性
		string Client { get; set; }
		string Server { get; set; }
		ushort Port { get; set; }
		TimeSpan Timeout { get; set; }
		string Charset { get; set; }
		string Encoding { get; set; }
		string Provider { get; set; }
		string Database { get; set; }
		string UserName { get; set; }
		string Password { get; set; }
		string Application { get; set; }
		#endregion

		#region 方法定义
		void Clear();
		bool Contains(string key);
		bool Remove(string key);
		bool Remove(string key, out string value);
		bool TryGetValue(string key, out string value);
		bool TrySetValue(string key, string value);
		#endregion
	}

	public interface IConnectionSettingValuesMapper
	{
		/// <summary>获取当前连接的驱动标识。</summary>
		string Driver { get; }

		/// <summary>
		/// 获取指定键名的映射名。
		/// </summary>
		/// <param name="key">指定的键名。</param>
		/// <returns>返回映射后的键名。</returns>
		string Map(string key);

		/// <summary>
		/// 获取指定键名对应的值。
		/// </summary>
		/// <param name="key">指定的键名。</param>
		/// <param name="values">当前连接设置的字典。</param>
		/// <returns>返回指定键名的值。</returns>
		string GetValue(string key, IDictionary<string, string> values);

		/// <summary>
		/// 设置指定键名对应的值。
		/// </summary>
		/// <param name="key">指定的键名。</param>
		/// <param name="value">要设置的内容。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetValue(string key, string value);
	}
}
