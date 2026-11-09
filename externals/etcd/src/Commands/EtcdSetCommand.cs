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

[CommandOption("expiry", 'x', typeof(TimeSpan?))]
public sealed class EtcdSetCommand() : EtcdCommandBase("Set")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		RequireArguments(context);
		var value = context.Arguments.Count > 1 ? context.Arguments[1] : context.Value?.ToString();
		if(value == null)
			throw new CommandException(Properties.Resources.Command_ValueRequired_Message);
		await GetEtcd(context).SetValueAsync(context.Arguments[0], value, context.Options.GetValue<TimeSpan?>("expiry"), cancellation);
		return true;
	}
}
