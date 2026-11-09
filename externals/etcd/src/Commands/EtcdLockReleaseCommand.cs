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

public sealed class EtcdLockReleaseCommand() : EtcdCommandBase("Release")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value is IAsyncDisposable asyncDisposable)
			await asyncDisposable.DisposeAsync();
		else if(context.Value is IEnumerable<IDistributedLock> locks)
		{
			foreach(var locker in locks)
				await locker.DisposeAsync();
		}

		if(context.Arguments.Count != 2)
			return false;
		return await GetEtcd(context).ReleaseAsync(context.Arguments[0], Convert.FromHexString(context.Arguments[1]), cancellation);
	}
}
