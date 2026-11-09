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

public static class CommandContextUtility
{
	public static CommandNode Find(this CommandContext context, string path) =>
		context?.CommandNode?.Find(path);
	public static CommandNode Find(this CommandContext context, ICommand command, bool rooting = false) =>
		context?.CommandNode?.Find(command, rooting);
	public static TCommand Find<TCommand>(this CommandContext context, bool rooting = false) where TCommand : class =>
		context?.CommandNode?.Find<TCommand>(rooting);
	public static CommandNode Find(this CommandContext context, Predicate<CommandNode> predicate, bool rooting = false) =>
		context?.CommandNode?.Find(predicate, rooting);
}
