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

namespace Zongsoft.Externals.Etcd.Commands;

[CommandOption("seed", typeof(int), DefaultValue = 0)]
[CommandOption("interval", typeof(int), DefaultValue = 1)]
[CommandOption("expiry", 'x', typeof(TimeSpan?))]
public sealed class EtcdIncrementCommand() : EtcdCommandBase("Increase")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) =>
		await ChangeAsync(context, false, cancellation);

	internal static async ValueTask<object> ChangeAsync(CommandContext context, bool decrease, CancellationToken cancellation)
	{
		RequireArguments(context);
		var etcd = GetEtcd(context);
		var seed = context.Options.GetValue<int>("seed");
		var interval = context.Options.GetValue<int>("interval");
		var expiry = context.Options.GetValue<TimeSpan?>("expiry");
		var values = new long[context.Arguments.Count];

		for(var index = 0; index < values.Length; index++)
		{
			values[index] = decrease ? await etcd.DecreaseAsync(context.Arguments[index], interval, seed, expiry, cancellation) : await etcd.IncreaseAsync(context.Arguments[index], interval, seed, expiry, cancellation);
			context.Output.WriteLine(values[index].ToString());
		}

		return values.Length == 1 ? values[0] : values;
	}
}
