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

[DisplayName("RedisSetCommand.Name")]
[Description("RedisSetCommand.Description")]
[CommandOption(REQUISITE_OPTION, typeof(Caching.CacheRequisite), DefaultValue = Caching.CacheRequisite.Always, Description = "RedisSetCommand.Options.Requisite")]
[CommandOption(EXPIRY_OPTION, 'x', typeof(TimeSpan?), DefaultValue = null, Description = "RedisSetCommand.Options.Expiry")]
public class RedisSetCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string REQUISITE_OPTION = "requisite";
	private const string EXPIRY_OPTION = "expiry";
	#endregion

	#region 构造函数
	public RedisSetCommand() : base("Set") { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.MissingArguments_Message);

		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);
		var expiry = context.Options.GetValue<TimeSpan?>(EXPIRY_OPTION) ?? TimeSpan.Zero;
		var requisite = context.Options.GetValue<Caching.CacheRequisite>(REQUISITE_OPTION);

		if(context.Arguments.Count == 1)
		{
			if(context.Value != null)
				return await redis.SetValueAsync(context.Arguments[0], context.Value, expiry, requisite, cancellation);

			throw new CommandException(Properties.Resources.MissingArguments_Message);
		}

		return await redis.SetValueAsync(context.Arguments[0], context.Arguments[1], expiry, requisite, cancellation);
	}
	#endregion
}
