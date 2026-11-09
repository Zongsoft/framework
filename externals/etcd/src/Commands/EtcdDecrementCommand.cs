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
public sealed class EtcdDecrementCommand() : EtcdCommandBase("Decrease")
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => EtcdIncrementCommand.ChangeAsync(context, true, cancellation);
}
