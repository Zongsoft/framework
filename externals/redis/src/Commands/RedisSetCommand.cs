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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Externals.Redis.Commands
{
	[Zongsoft.Services.CommandOption("requires", Type = typeof(Zongsoft.Runtime.Caching.CacheRequires), Description = "${Text.SetCommand.Requires}")]
	[Zongsoft.Services.CommandOption("expiry", Type = typeof(TimeSpan), Description = "${Text.SetCommand.Expiry}")]
	public class RedisSetCommand : Zongsoft.Services.CommandBase<Zongsoft.Services.CommandContext>
	{
		#region 构造函数
		public RedisSetCommand() : base("Set")
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(Services.CommandContext context)
		{
			if(context.Expression.Arguments.Length == 0)
				throw new Zongsoft.Services.CommandException("Missing arguments.");

			if(!context.Expression.Options.TryGetValue<TimeSpan>("expiry", out var expiry))
				expiry = TimeSpan.Zero;

			if(!context.Expression.Options.TryGetValue<Zongsoft.Runtime.Caching.CacheRequires>("requires", out var requires))
				requires = Runtime.Caching.CacheRequires.Always;

			var redis = RedisCommand.GetRedis(context.CommandNode);

			if(context.Expression.Arguments.Length == 1)
			{
				if(context.Parameter != null)
					return redis.SetValue(context.Expression.Arguments[0], context.Parameter, expiry, requires);

				throw new Zongsoft.Services.CommandException("Missing arguments.");
			}

			return redis.SetValue(context.Expression.Arguments[0], context.Expression.Arguments[1], expiry, requires);
		}
		#endregion
	}
}
