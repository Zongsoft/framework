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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Distributing;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("Text.RedisLockAcquireCommand.Name")]
[Description("Text.RedisLockAcquireCommand.Description")]
[CommandOption(COMMAND_EXPIRY_OPTION, typeof(string), "60s", false, "")]
public class RedisLockAcquireCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string COMMAND_EXPIRY_OPTION = "expiry";
	#endregion

	#region 构造函数
	public RedisLockAcquireCommand() : base("Acquire") { }
	public RedisLockAcquireCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Expression.Arguments.Length == 0)
			throw new CommandException();

		var expiry = TimeSpan.FromMinutes(1);

		if(context.Expression.Options.TryGetValue<string>(COMMAND_EXPIRY_OPTION, out var value))
		{
			if(!Zongsoft.Common.TimeSpanUtility.TryParse(value, out expiry))
				expiry = TimeSpan.FromMinutes(1);
		}

		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException($"Missing the required redis service.");
		var lockers = new List<IDistributedLock>(context.Expression.Arguments.Length);

		for(int i = 0; i < context.Expression.Arguments.Length; i++)
		{
			var key = context.Expression.Arguments[i];
			var locker = await redis.AcquireAsync(key, expiry, cancellation);
			Print(context.Output, i + 1, key, locker, redis.Tokenizer);

			if(locker != null)
				lockers.Add(locker);
		}

		return lockers;
	}
	#endregion

	#region 私有方法
	private static void Print(ICommandOutlet output, int index, string key, IDistributedLock locker, IDistributedLockTokenizer tokenizer)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.Gray, $"[{index}] ")
			.Append(CommandOutletColor.DarkGreen, key);

		if(locker == null)
		{
			content.Append(CommandOutletColor.DarkRed, " NULL");
		}
		else
		{
			content.Append(CommandOutletColor.DarkYellow, $" {GetTokenString(tokenizer, locker.Token)}")
			       .Append(CommandOutletColor.DarkMagenta, $" {nameof(locker.IsHeld)}:{locker.IsHeld}");
		}

		output.WriteLine(content);
	}

	private static string GetTokenString(IDistributedLockTokenizer tokenizer, byte[] token)
	{
		return tokenizer == null || token == null || token.Length == 0 ? null : tokenizer.GetString(token);
	}
	#endregion
}
