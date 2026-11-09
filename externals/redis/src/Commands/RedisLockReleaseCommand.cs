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

[DisplayName("RedisLockReleaseCommand.Name")]
[Description("RedisLockReleaseCommand.Description")]
public class RedisLockReleaseCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisLockReleaseCommand() : base("Release") { }
	public RedisLockReleaseCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);

		if(context.Value is IAsyncDisposable disposable)
		{
			await disposable.DisposeAsync();
		}
		else if(context.Value is IEnumerable<Services.Distributing.IDistributedLock> lockers)
		{
			foreach(var entry in lockers)
				await entry.DisposeAsync();
		}

		if(context.Arguments.Count != 2)
			return false;

		var result = await redis.ReleaseAsync(
			context.Arguments[0],
			GetToken(redis.Tokenizer.Name, context.Arguments[1]),
			cancellation);

		return Print(context.Output, result);
	}
	#endregion

	#region 私有方法
	private static bool Print(ICommandOutlet output, bool result)
	{
		if(result)
			output.WriteLine(CommandOutletColor.Green, Properties.Resources.Succeeded);
		else
			output.WriteLine(CommandOutletColor.DarkRed, Properties.Resources.Failed);

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
