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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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