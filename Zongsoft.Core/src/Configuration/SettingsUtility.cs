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

namespace Zongsoft.Configuration;

public static class SettingsUtility
{
	public static T GetValue<T>(this ISettings settings, string name, T defaultValue = default)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if(name != null && settings.TryGetValue(name, out var value))
			return Common.Convert.ConvertValue(value, defaultValue);

		return defaultValue;
	}

	public static T GetValue<T>(this ISettings settings, string name, Func<T> defaultValue)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(defaultValue);

		if(name != null && settings.TryGetValue(name, out var value))
			return Common.Convert.ConvertValue(value, defaultValue);

		return defaultValue();
	}
}
