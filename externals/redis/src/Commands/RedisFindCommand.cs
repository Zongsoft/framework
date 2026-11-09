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
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("RedisFindCommand.Name")]
[Description("RedisFindCommand.Description")]
[CommandOption(COUNT_OPTION, typeof(int), DefaultValue = 100, Description = "RedisFindCommand.Options.Count")]
public class RedisFindCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string COUNT_OPTION = "count";
	#endregion

	#region 构造函数
	public RedisFindCommand() : base("Find") { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.MissingArguments_Message);

		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);

		//查找指定模式的键名集
		var result = redis.FindAsync(context.Arguments[0], cancellation);

		//定义遍历序号
		var index = 1;

		var count = context.Options.GetValue<int>(COUNT_OPTION);
		var list = new List<string>(count > 0 ? count : 16);

		await foreach(var key in result)
		{
			if(count > 0 && index > count)
				break;

			list.Add(key);
			context.Output.Write(CommandOutletColor.DarkGray, $"[{index++}] ");
			context.Output.WriteLine(key);
		}

		return list;
	}
	#endregion
}
