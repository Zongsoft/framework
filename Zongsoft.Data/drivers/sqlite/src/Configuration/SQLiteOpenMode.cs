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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.SQLite.Configuration;

public enum SQLiteOpenMode
{
	ReadWriteCreate,
	ReadWrite,
	ReadOnly,
	Memory
}

internal static class SQLiteOpenModeUtility
{
	public static Microsoft.Data.Sqlite.SqliteOpenMode Convert(this SQLiteOpenMode mode) => mode switch
	{
		SQLiteOpenMode.ReadWriteCreate => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
		SQLiteOpenMode.ReadWrite => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWrite,
		SQLiteOpenMode.ReadOnly => Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
		SQLiteOpenMode.Memory => Microsoft.Data.Sqlite.SqliteOpenMode.Memory,
		_ => throw new ArgumentOutOfRangeException(nameof(mode))
	};
}