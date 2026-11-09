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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("RedisDecreaseCommand.Name")]
[Description("RedisDecreaseCommand.Description")]
[CommandOption("seed", typeof(int), DefaultValue = 0, Description = "RedisDecreaseCommand.Options.Seed")]
[CommandOption("interval", typeof(int), DefaultValue = 1, Description = "RedisDecreaseCommand.Options.Interval")]
[CommandOption("expiry", 'x', typeof(TimeSpan), Description = "RedisDecreaseCommand.Options.Expiry")]
public class RedisDecrementCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisDecrementCommand() : base("Decrease") { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.MissingArguments_Message);

		int seed = context.Options.GetValue<int>("seed");
		var interval = context.Options.GetValue<int>("interval");
		var expiry = context.Options.GetValue<TimeSpan>("expiry");
		var result = new long[context.Arguments.Count];

		for(int i = 0; i < context.Arguments.Count; i++)
		{
			var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);
			result[i] = await redis.DecreaseAsync(context.Arguments[i], interval, seed, expiry, cancellation);
			context.Output.WriteLine(result[i].ToString());
		}

		if(result.Length == 1)
			return result[0];
		else
			return result;
	}
	#endregion
}
