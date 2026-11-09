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

[DisplayName("RedisCountCommand.Name")]
[Description("RedisCountCommand.Description")]
public class RedisCountCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisCountCommand() : base("Count") { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);
		var count = await redis.GetCountAsync(cancellation);
		context.Output.WriteLine(count.ToString());
		return count;
	}
	#endregion
}
