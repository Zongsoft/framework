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
using System.ComponentModel;

namespace Zongsoft.Externals.Redis.Commands
{
	[DisplayName("Text.RedisSetCommand.Name")]
	[Description("Text.RedisSetCommand.Description")]
	[Zongsoft.Services.CommandOption(REQUISITE_OPTION, Type = typeof(Caching.CacheRequisite), DefaultValue = Caching.CacheRequisite.Always, Description = "Text.RedisSetCommand.Options.Requisite")]
	[Zongsoft.Services.CommandOption(EXPIRY_OPTION, Type = typeof(TimeSpan?), DefaultValue = null, Description = "Text.RedisSetCommand.Options.Expiry")]
	public class RedisSetCommand : Zongsoft.Services.CommandBase<Zongsoft.Services.CommandContext>
	{
		#region 常量定义
		private const string REQUISITE_OPTION = "requisite";
		private const string EXPIRY_OPTION = "expiry";
		#endregion

		#region 构造函数
		public RedisSetCommand() : base("Set") { }
		#endregion

		#region 执行方法
		protected override object OnExecute(Services.CommandContext context)
		{
			if(context.Expression.Arguments.Length == 0)
				throw new Zongsoft.Services.CommandException("Missing arguments.");

			var expiry = context.Expression.Options.GetValue<TimeSpan?>(EXPIRY_OPTION) ?? TimeSpan.Zero;
			var requisite = context.Expression.Options.GetValue<Caching.CacheRequisite>(REQUISITE_OPTION);

			var redis = context.CommandNode.Find<RedisCommand>(true)?.Redis ?? throw new Zongsoft.Services.CommandException($"Missing the required redis service.");

			if(context.Expression.Arguments.Length == 1)
			{
				if(context.Parameter != null)
					return redis.SetValue(context.Expression.Arguments[0], context.Parameter, expiry, requisite);

				throw new Zongsoft.Services.CommandException("Missing arguments.");
			}

			return redis.SetValue(context.Expression.Arguments[0], context.Expression.Arguments[1], expiry, requisite);
		}
		#endregion
	}
}
