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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Data.DuckDB library.
 *
 * The Zongsoft.Data.DuckDB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.DuckDB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.DuckDB library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Configuration;

namespace Zongsoft.Data.DuckDB.Configuration;

public sealed class DuckDBConnectionSettingsDriver : ConnectionSettingsDriver<DuckDBConnectionSettings>
{
	#region 单例字段
	public static readonly DuckDBConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private DuckDBConnectionSettingsDriver() : base(DuckDBDriver.NAME) { }
	#endregion
}
