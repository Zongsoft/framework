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
	/// 表示连接设置项（连接字符串）映射器的接口。
	/// </summary>
	public interface IConnectionSettingsMapper
	{
		/// <summary>获取当前连接的驱动标识。</summary>
		string Driver { get; }

		/// <summary>获取映射的键集。</summary>
		IDictionary<string, string> Mapping { get; }

		/// <summary>尝试映射转换指定键名对应的值。</summary>
		/// <param name="name">指定的待映射的键名。</param>
		/// <param name="options">指定的待映射的连接项集合。</param>
		/// <param name="value">输出参数，返回映射转换成功后的值。</param>
		/// <returns>如果映射成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool Map<T>(string name, IDictionary<string, string> options, out T value);

		/// <summary>验证待写入的键值。</summary>
		/// <param name="name">待写入的键名。</param>
		/// <param name="value">待写入的键值。</param>
		/// <returns>返回验证后的新键值。</returns>
		bool Validate(string name, string value);
	}
}