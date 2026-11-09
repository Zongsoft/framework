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

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

public class RedisLockCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisLockCommand() : base("Lock") { }
	public RedisLockCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);
		var normalizer = redis?.Tokenizer;

		if(normalizer != null)
			context.Output.WriteLine(normalizer.Name);

		return ValueTask.FromResult<object>(redis);
	}
	#endregion
}
