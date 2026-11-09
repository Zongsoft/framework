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

namespace Zongsoft.Scheduling;

public static class TriggerOptionsExtension
{
	public static ITriggerOptions Identifier(this ITriggerOptions options, string id)
	{
		if(options == null)
			return string.IsNullOrEmpty(id) ? null : new TriggerOptions(id);

		options.Identifier = id;
		return options;
	}

	public static TriggerOptions.Cron Cron(this ITriggerOptions options, string expression, TimeZoneInfo timezone = null)
	{
		if(options is TriggerOptions.Cron cron)
		{
			cron.Expression = expression;
			cron.Timezone = timezone;
			return cron;
		}

		return options is null ? new(expression) : new(options.Identifier, expression);
	}

	public static TriggerOptions.Latency Delay(this ITriggerOptions options, TimeSpan duration)
	{
		if(options is TriggerOptions.Latency latency)
		{
			latency.Duration = duration;
			return latency;
		}

		return options is null ? new(duration) : new(options.Identifier, duration);
	}
}
