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

[DisplayName("RedisCommand.Name")]
[Description("RedisCommand.Description")]
[CommandOption("name", 'n')]
public class RedisCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private RedisService _redis;
	#endregion

	#region 构造函数
	public RedisCommand() : base("Redis") { }
	#endregion

	#region 公共属性
	public RedisService Redis
	{
		get => _redis;
		set => _redis = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var name = context.Options.GetValue<string>("name");

		if(!string.IsNullOrEmpty(name))
			_redis = RedisServiceProvider.GetRedis(name) ?? throw new CommandException(string.Format(Properties.Resources.CannotObtainCommandTarget_Message, name));

		if(_redis == null)
			context.Output.WriteLine(CommandOutletColor.Magenta, Properties.Resources.NoRedis_Message);
		else
			context.Output.WriteLine(CommandOutletColor.Green, _redis.Settings);

		return ValueTask.FromResult<object>(_redis);
	}
	#endregion
}
