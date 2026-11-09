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
