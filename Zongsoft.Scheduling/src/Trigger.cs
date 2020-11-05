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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Scheduling library.
 *
 * The Zongsoft.Scheduling is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Scheduling is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Scheduling library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Scheduling
{
	public static class Trigger
	{
		#region 静态字段
		private static readonly IDictionary<string, ITriggerBuilder> _builders = new Dictionary<string, ITriggerBuilder>(StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<string, ITrigger> _triggers = new ConcurrentDictionary<string, ITrigger>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 静态属性
		public static IDictionary<string, ITriggerBuilder> Builders { get => _builders; }
		#endregion

		#region 静态方法
		public static bool IsExpired(this ITrigger trigger)
		{
			if(trigger == null)
				return true;

			return trigger.ExpirationTime.HasValue && trigger.ExpirationTime.Value.ToUniversalTime() < DateTime.UtcNow;
		}

		public static ITrigger Cron(string expression, DateTime? expiration = null, DateTime? effective = null, string description = null)
		{
			if(string.IsNullOrWhiteSpace(expression))
				return null;

			return Get("cron", expression, expiration, effective, description);
		}

		public static ITrigger Get(string scheme, string expression, DateTime? expiration = null, DateTime? effective = null, string description = null)
		{
			if(string.IsNullOrWhiteSpace(scheme))
				throw new ArgumentNullException(nameof(scheme));

			scheme = scheme.Trim().ToLowerInvariant();

			if(!_builders.TryGetValue(scheme, out var builder))
				throw new InvalidProgramException($"The '{scheme}' trigger builder not found.");

			//如果生效时间早于此刻，则忽略指定的生效时间
			if(effective.HasValue && effective.Value.ToUniversalTime() <= DateTime.UtcNow)
				effective = null;

			//如果过期时间早于此刻（即已经过期），则将过期时间指定为一个固定的过去时间
			if(expiration.HasValue && expiration.Value.ToUniversalTime() <= DateTime.UtcNow)
				expiration = DateTime.MinValue;

			var key = scheme + ":" + expression + "|" +
				(effective.HasValue ? effective.Value.Ticks.ToString() : "?") + "~" +
				(expiration.HasValue ? expiration.Value.Ticks.ToString() : "?");

			return _triggers.GetOrAdd(key, _ => builder.Build(expression, expiration, effective, description));
		}
		#endregion
	}
}
