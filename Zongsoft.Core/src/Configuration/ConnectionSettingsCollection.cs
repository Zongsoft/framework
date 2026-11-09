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
			         (settings.Driver != null && settings.Driver.IsDriver(driver)) ? settings : null;

		return settings != null;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IConnectionSettings settings) => settings.Name;
	#endregion
}
