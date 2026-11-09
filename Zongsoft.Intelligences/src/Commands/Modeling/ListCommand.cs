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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands.Modeling;

[CommandOption("running", 'r')]
public class ListCommand() : CommandBase<CommandContext>("List")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = context.Find<IServiceAccessor<IModelService>>(true)?.Value ??
			throw new CommandException(Properties.Resources.Model_ServiceRequired_Message);

		if(context.Options.Contains("running"))
		{
			var models = service.GetModelsAsync("running", cancellation);
			await context.Dump(models);
			return models;
		}
		else
		{
			var models = service.GetModelsAsync(null, cancellation);
			await context.Dump(models);
			return models;
		}
	}
}
