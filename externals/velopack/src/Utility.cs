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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Velopack;

namespace Zongsoft.Externals.Velopack;

internal static class Utility
{
	public static bool TryGetToken(IReadOnlyDictionary<string, string> settings, out string value)
	{
		if(settings == null)
		{
			value = null;
			return false;
		}

		if(settings is Configuration.VelopackConnectionSettings connectionSettings)
		{
			value = connectionSettings.Token;
			return !string.IsNullOrEmpty(value);
		}

		return settings.TryGetValue(nameof(Configuration.VelopackConnectionSettings.Token), out value);
	}

	public static TimeSpan GetPeriod(IReadOnlyDictionary<string, string> settings)
	{
		const int DEFAULT_SECONDS = 300;

		if(settings == null)
			return TimeSpan.FromSeconds(DEFAULT_SECONDS);

		if(settings is Configuration.VelopackConnectionSettings connectionSettings)
			return connectionSettings.Period;

		return settings.TryGetValue(nameof(Configuration.VelopackConnectionSettings.Period), out var value) && Common.TimeSpanUtility.TryParse(value, out var duraion) && duraion > TimeSpan.Zero ?
			duraion : TimeSpan.FromSeconds(DEFAULT_SECONDS);
	}

	public static TimeSpan GetTimeout(IReadOnlyDictionary<string, string> settings)
	{
		const int DEFAULT_SECONDS = 60;

		if(settings == null)
			return TimeSpan.FromSeconds(DEFAULT_SECONDS);

		if(settings is Configuration.VelopackConnectionSettings connectionSettings)
			return connectionSettings.Timeout;

		return settings.TryGetValue(nameof(Configuration.VelopackConnectionSettings.Timeout), out var value) && Common.TimeSpanUtility.TryParse(value, out var duraion) && duraion > TimeSpan.Zero ?
			duraion : TimeSpan.FromSeconds(DEFAULT_SECONDS);
	}

	public static UpdateOptions GetOptions(IReadOnlyDictionary<string, string> settings)
	{
		if(settings == null)
			return null;

		if(settings is Configuration.VelopackConnectionSettings connectionSettings)
			return string.IsNullOrEmpty(connectionSettings.Channel) ? null : new UpdateOptions() { ExplicitChannel = connectionSettings.Channel };

		return settings.TryGetValue(nameof(Configuration.VelopackConnectionSettings.Channel), out var value) && !string.IsNullOrEmpty(value) ?
			new UpdateOptions() { ExplicitChannel = value } : null;
	}
}
