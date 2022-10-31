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
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Distributing;

namespace Zongsoft.Externals.Redis.Commands
{
	[DisplayName("Text.RedisLockReleaseCommand.Name")]
	[Description("Text.RedisLockReleaseCommand.Description")]
	public class RedisLockReleaseCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public RedisLockReleaseCommand() : base("Release") { }
		public RedisLockReleaseCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			var redis = RedisCommand.GetRedis(context.CommandNode);

			if(context.Parameter is IDistributedLock locker)
			{
				locker.Dispose();
			}
			else if(context.Parameter is IEnumerable<IDistributedLock> lockers)
			{
				foreach(var entry in lockers)
					entry.DisposeAsync().AsTask().Wait();
			}

			if(context.Expression.Arguments.Length != 2)
				return false;

			return Print(context.Output, redis.ReleaseAsync(context.Expression.Arguments[0], GetToken(redis.Tokenizer.Name, context.Expression.Arguments[1])).AsTask().GetAwaiter().GetResult());
		}
		#endregion

		#region 私有方法
		private static bool Print(ICommandOutlet output, bool result)
		{
			if(result)
				output.WriteLine(CommandOutletColor.Green, "Succeed.");
			else
				output.WriteLine(CommandOutletColor.DarkRed, "Faild.");

			return result;
		}

		private static byte[] GetToken(string name, string text)
		{
			if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(text))
				return null;

			return name.ToLowerInvariant() switch
			{
				"guid" => Guid.TryParse(text, out var guid) ? guid.ToByteArray() : null,
				"random" => ulong.TryParse(text, out var number) ? BitConverter.GetBytes(number) : null,
				_ => null,
			};
		}
		#endregion
	}
}
