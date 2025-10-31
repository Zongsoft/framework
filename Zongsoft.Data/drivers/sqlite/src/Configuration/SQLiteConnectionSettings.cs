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
using System.ComponentModel;

using Microsoft.Data.Sqlite;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.SQLite.Configuration;

public sealed class SQLiteConnectionSettings : ConnectionSettingsBase<SQLiteConnectionSettingsDriver, SqliteConnectionStringBuilder>
{
	#region 构造函数
	public SQLiteConnectionSettings(SQLiteConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public SQLiteConnectionSettings(SQLiteConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(SqliteConnectionStringBuilder.DataSource))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqliteConnectionStringBuilder.Password))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue(SqliteOpenMode.ReadWrite)]
	public SQLiteOpenMode Mode
	{
		get => this.GetValue<SQLiteOpenMode>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue(SqliteCacheMode.Default)]
	public SQLiteCacheMode Cache
	{
		get => this.GetValue<SQLiteCacheMode>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	public bool ForeignKeys
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(false)]
	public bool RecursiveTriggers
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[Alias(nameof(SqliteConnectionStringBuilder.DefaultTimeout))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(SqliteConnectionStringBuilder.Pooling))]
	public bool Pooling
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override SqliteConnectionStringBuilder CreateOptions() => new(string.Empty);
	protected override void Populate(SqliteConnectionStringBuilder options)
	{
		base.Populate(options);
		options.Mode = this.Mode.Convert();
		options.Cache = this.Cache.Convert();
	}
	#endregion
}
