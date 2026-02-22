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
using System.ComponentModel;

using DuckDB.NET.Data;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.DuckDB.Configuration;

public sealed class DuckDBConnectionSettings : ConnectionSettingsBase<DuckDBConnectionSettingsDriver, DuckDBConnectionStringBuilder>
{
	#region 构造函数
	public DuckDBConnectionSettings(DuckDBConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public DuckDBConnectionSettings(DuckDBConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(DuckDBConnectionStringBuilder.DataSource))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override DuckDBConnectionStringBuilder CreateOptions() => new();
	protected override void Populate(DuckDBConnectionStringBuilder options)
	{
		base.Populate(options);
	}
	#endregion
}
