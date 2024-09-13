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
	/// 表示连接设置值（连接字符串）的接口。
	/// </summary>
	public interface IConnectionSettingValues : IEnumerable<KeyValuePair<string, string>>
	{
		#region 通用属性
		/// <summary>获取连接字符串的键值对数量。</summary>
		int Count { get; }

		/// <summary>获取或设置指定键名的值。</summary>
		/// <param name="name">指定的连接字符串的键名，支持标准名和映射的原始名。</param>
		/// <returns>返回指定键名的设置值，如果为空(null)则表示指定的键名不存在。</returns>
		string this[string name] { get; set; }

		/// <summary>获取连接字符串的原始键名的映射项集合。</summary>
		IEnumerable<KeyValuePair<string, string>> Mapping { get; }
		#endregion

		#region 特定属性
		string Group { get; set; }
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
		string Instance { get; set; }
		string Application { get; set; }
		#endregion

		#region 方法定义
		void Clear();
		bool Contains(string name);
		bool Remove(string name);
		bool Remove(string name, out string value);
		bool SetValue(string name, string value);
		T GetValue<T>(string name, T defaultValue = default);
		bool TryGetValue<T>(string name, out T value);
		#endregion
	}
}
