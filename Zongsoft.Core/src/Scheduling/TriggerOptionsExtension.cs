/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Scheduling
{
	public static class TriggerOptionsExtension
	{
		public static ITriggerOptions Identifier(this ITriggerOptions options, string id)
		{
			if(options == null)
				return string.IsNullOrEmpty(id) ? null : new TriggerOptions(id);

			options.Identifier = id;
			return options;
		}

		public static TriggerOptions.Cron Cron(this ITriggerOptions options, string expression)
		{
			if(options is TriggerOptions.Cron cron)
			{
				cron.Expression = expression;
				return cron;
			}

			return options is null ? new (expression) : new (options.Identifier, expression);
		}

		public static TriggerOptions.Latency Delay(this ITriggerOptions options, TimeSpan duration)
		{
			if(options is TriggerOptions.Latency latency)
			{
				latency.Duration = duration;
				return latency;
			}

			return options is null ? new (duration) : new (options.Identifier, duration);
		}
	}
}
