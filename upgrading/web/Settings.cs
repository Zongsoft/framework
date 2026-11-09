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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Upgrading;

public static class Settings
{
	private static readonly Lazy<Zongsoft.Configuration.Settings> _current = new(() =>
	{
		const string SETTINGS_PATH = "/Upgrading/Settings";
		const string SERVER_SETTING = "server";

		var configuration = ApplicationContext.Current?.Configuration;
		var settings = configuration?.GetOption<SettingsCollection>(SETTINGS_PATH);

		if(settings != null && settings.TryGetValue(SERVER_SETTING, out var result))
			return result;

		return null;
	});

	public static Zongsoft.Configuration.Settings Current => _current.Value;
}
