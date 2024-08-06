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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	/// <summary>
	/// 表示数据源的接口。
	/// </summary>
	public interface IDataSource : IEquatable<IDataSource>
	{
		/// <summary>获取数据源的名称。</summary>
		string Name { get; }

		/// <summary>获取数据源的连接字符串。</summary>
		string ConnectionString { get; }

		/// <summary>获取数据源支持的访问方式。</summary>
		DataAccessMode Mode { get; set; }

		/// <summary>获取数据源关联的数据驱动器。</summary>
		IDataDriver Driver { get; }

		/// <summary>获取支持的功能特性集。</summary>
		FeatureCollection Features { get; }

		/// <summary>获取扩展属性集。</summary>
		IDictionary<string, object> Properties { get; }
	}
}
