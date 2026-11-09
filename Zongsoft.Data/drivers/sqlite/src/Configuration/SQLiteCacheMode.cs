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

public enum SQLiteCacheMode
{
	Default,
	Private,
	Shared
}

internal static class SQLiteCacheModeUtility
{
	public static Microsoft.Data.Sqlite.SqliteCacheMode Convert(this SQLiteCacheMode mode) => mode switch
	{
		SQLiteCacheMode.Default => Microsoft.Data.Sqlite.SqliteCacheMode.Default,
		SQLiteCacheMode.Private => Microsoft.Data.Sqlite.SqliteCacheMode.Private,
		SQLiteCacheMode.Shared => Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
		_ => throw new ArgumentOutOfRangeException(nameof(mode))
	};
}