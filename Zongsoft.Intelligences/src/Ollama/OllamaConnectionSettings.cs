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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences.Ollama;

public class OllamaConnectionSettings : ConnectionSettingsBase<OllamaConnectionSettingsDriver>
{
	#region 构造函数
	public OllamaConnectionSettings(OllamaConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public OllamaConnectionSettings(OllamaConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[ConnectionSetting(true, Ignored = true)]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true, Ignored = true)]
	public string Model
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(false, Ignored = true)]
	public string History
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(false, Ignored = true)]
	[DefaultValue("12h")]
	public TimeSpan Expiration
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}
	#endregion
}
