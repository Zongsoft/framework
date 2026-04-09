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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
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
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.CopyCommand.Instance.Name);
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.MoveCommand.Instance.Name);
				Components.CommandExecutor.Default.Root.Children.Remove(Commands.DeleteCommand.Instance.Name);
			}

			Components.CommandExecutor.Default.Root.Children.Add(Commands.CopyCommand.Instance);
			Components.CommandExecutor.Default.Root.Children.Add(Commands.MoveCommand.Instance);
			Components.CommandExecutor.Default.Root.Children.Add(Commands.DeleteCommand.Instance);

			//设置初始化完成
			_initialized = true;
		}
	}
}
