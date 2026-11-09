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

namespace Zongsoft.Upgrading;

partial class Executor
{
	#if NET9_0_OR_GREATER
	private static readonly Lock _locker = new();
	#else
	private static readonly object _locker = new();
	#endif

	private static bool _initialized;
	static partial void Initialize()
	{
		if(_initialized)
			return;

		lock(_locker)
		{
			if(_initialized)
				return;

			if(Components.CommandExecutor.Default.Root.HasChildren)
			{
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.LinkCommand.Instance.Name);
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.CopyCommand.Instance.Name);
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.MoveCommand.Instance.Name);
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.DeleteCommand.Instance.Name);
			}

			Components.CommandExecutor.Default.Root.Children.Add(Commands.LinkCommand.Instance);
			Components.CommandExecutor.Default.Root.Children.Add(Commands.CopyCommand.Instance);
			Components.CommandExecutor.Default.Root.Children.Add(Commands.MoveCommand.Instance);
			Components.CommandExecutor.Default.Root.Children.Add(Commands.DeleteCommand.Instance);

			//设置初始化完成
			_initialized = true;
		}
	}
}
