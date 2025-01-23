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
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration;

public class ConnectionSettingsCollection() : KeyedCollection<string, IConnectionSettings>(StringComparer.OrdinalIgnoreCase)
{
	#region 成员字段
	private string _default = string.Empty;
	#endregion

	#region 公共属性
	public string Default
	{
		get => _default;
		set => _default = value ?? string.Empty;
	}

	public new IConnectionSettings this[string name] => base[string.IsNullOrEmpty(name) ? _default : name];
	#endregion

	#region 公共方法
	public IConnectionSettings GetDefault() => this.TryGetValue(_default, out var setting) ? setting : null;
	public bool Contains(string name, string driver) => string.IsNullOrEmpty(driver) ?
		this.Contains(name ?? string.Empty) :
		this.TryGetValue(name, driver, out _);

	public new bool TryGetValue(string name, out IConnectionSettings result) => base.TryGetValue(string.IsNullOrEmpty(name) ? _default : name, out result);
	public bool TryGetValue(string name, string driver, out IConnectionSettings settings)
	{
		if(this.TryGetValue(name, out settings) && settings != null && !string.IsNullOrEmpty(driver))
			settings = string.Equals(settings.Driver?.Name, driver, StringComparison.OrdinalIgnoreCase) ||
			         (settings.Driver != null && settings.Driver.IsDriver(driver))? settings : null;

		return settings != null;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IConnectionSettings settings) => settings.Name;
	#endregion
}
