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
	[Zongsoft.Services.CommandOption("seed", Type = typeof(int), DefaultValue = 0)]
	[Zongsoft.Services.CommandOption("interval", Type = typeof(int), DefaultValue = 1)]
	public class RedisIncrementCommand : Zongsoft.Services.CommandBase<Zongsoft.Services.CommandContext>
	{
		#region 构造函数
		public RedisIncrementCommand() : base("Increment")
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(Services.CommandContext context)
		{
			if(context.Expression.Arguments.Length < 1)
				throw new Zongsoft.Services.CommandException("Missing arguments.");

			int seed = context.Expression.Options.GetValue<int>("seed");
			int interval = context.Expression.Options.GetValue<int>("interval");
			var result = new long[context.Expression.Arguments.Length];

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				result[i] = RedisCommand.GetRedis(context.CommandNode).Increment(context.Expression.Arguments[i], interval, seed);
				context.Output.WriteLine(result[i].ToString());
			}

			if(result.Length == 1)
				return result[0];
			else
				return result;
		}
		#endregion
	}
}
