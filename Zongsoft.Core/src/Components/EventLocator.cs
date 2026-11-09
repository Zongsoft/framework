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

namespace Zongsoft.Components;

public class EventLocator : EventLocatorBase<IExecutorContext>
{
	public EventLocator() { }
	public EventLocator(EventRegistryBase registry) : base(registry) { }

	protected override Type GetArgumentType(IExecutorContext context) =>
		context.Parameters.TryGetValue("argumentType", out var value) && value is Type type ? type : null;

	protected override string GetName(IExecutorContext context) =>
		context.Parameters.TryGetValue("name", out var value) && value is string name ? name : null;
}
