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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Etcd.Commands;

[CommandOption("expiry", 'x', typeof(TimeSpan), "60s")]
[CommandOption("renewal", 'r', typeof(TimeSpan?))]
public sealed class EtcdLockAcquireCommand() : EtcdCommandBase("Acquire")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		RequireArguments(context);
		var etcd = GetEtcd(context);
		var locks = new List<IDistributedLock>(context.Arguments.Count);
		var options = new DistributedLockOptions(context.Options.GetValue("expiry", TimeSpan.FromSeconds(60)))
		{
			RenewalInterval = context.Options.GetValue<TimeSpan?>("renewal"),
		};

		foreach(var key in context.Arguments)
		{
			var locker = await etcd.AcquireAsync(key, options, cancellation);
			locks.Add(locker);
			context.Output.WriteLine($"{key} {Convert.ToHexString(locker.Token)} {locker.IsHeld} #{locker.FencingToken}");
		}

		return locks;
	}
}
