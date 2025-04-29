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
 * Copyright (C) 2015-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Terminals;

public static class TerminalCommandExecutorUtility
{
	#region 同步命令
	public static ICommand Command(this TerminalCommandExecutor executor, string name, Action<TerminalCommandContext> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalCommand<object>(name, command)).Command;
	}

	public static ICommand Command<TArgument>(this TerminalCommandExecutor executor, string name, Action<TerminalCommandContext, TArgument> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalCommand<TArgument, object>(name, command, argument)).Command;
	}

	public static ICommand Command<TResult>(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, TResult> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalCommand<TResult>(name, command)).Command;
	}

	public static ICommand Command<TArgument, TResult>(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, TArgument, TResult> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalCommand<TArgument, TResult>(name, command, argument)).Command;
	}
	#endregion

	#region 异步命令
	public static ICommand Command(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, CancellationToken, ValueTask> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalAsyncCommand<object>(name, command)).Command;
	}

	public static ICommand Command<TResult>(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, CancellationToken, ValueTask<TResult>> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalAsyncCommand<TResult>(name, command)).Command;
	}

	public static ICommand Command<TArgument>(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, TArgument, CancellationToken, ValueTask> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalAsyncCommand<TArgument, object>(name, command, argument)).Command;
	}

	public static ICommand Command<TArgument, TResult>(this TerminalCommandExecutor executor, string name, Func<TerminalCommandContext, TArgument, CancellationToken, ValueTask<TResult>> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new TerminalAsyncCommand<TArgument, TResult>(name, command, argument)).Command;
	}
	#endregion

	#region 嵌套子类
	[Command(IgnoreOptions = true)]
	internal sealed class TerminalCommand<TResult> : CommandBase<TerminalCommandContext>
	{
		private readonly Func<TerminalCommandContext, TResult> _executor;

		public TerminalCommand(string name, Action<TerminalCommandContext> executor) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_executor = context =>
			{
				executor.Invoke(context);
				return default;
			};
		}

		public TerminalCommand(string name, Func<TerminalCommandContext, TResult> executor) : base(name)
		{
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override ValueTask<object> OnExecuteAsync(TerminalCommandContext context, CancellationToken cancellation) => ValueTask.FromResult<object>(cancellation.IsCancellationRequested ? null : _executor(context));
	}

	[Command(IgnoreOptions = true)]
	internal sealed class TerminalCommand<TArgument, TResult> : CommandBase<TerminalCommandContext>
	{
		private readonly TArgument _argument;
		private readonly Func<TerminalCommandContext, TArgument, TResult> _executor;

		public TerminalCommand(string name, Action<TerminalCommandContext, TArgument> executor, TArgument argument) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_argument = argument;
			_executor = (context, argument) =>
			{
				executor.Invoke(context, argument);
				return default;
			};
		}

		public TerminalCommand(string name, Func<TerminalCommandContext, TArgument, TResult> executor, TArgument argument) : base(name)
		{
			_argument = argument;
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override ValueTask<object> OnExecuteAsync(TerminalCommandContext context, CancellationToken cancellation) => ValueTask.FromResult<object>(cancellation.IsCancellationRequested ? null : _executor(context, _argument));
	}

	[Command(IgnoreOptions = true)]
	internal sealed class TerminalAsyncCommand<TResult> : CommandBase<TerminalCommandContext>
	{
		private readonly Func<TerminalCommandContext, CancellationToken, ValueTask<TResult>> _executor;

		public TerminalAsyncCommand(string name, Func<TerminalCommandContext, CancellationToken, ValueTask> executor) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_executor = async (context, token) =>
			{
				await executor.Invoke(context, token);
				return default;
			};
		}

		public TerminalAsyncCommand(string name, Func<TerminalCommandContext, CancellationToken, ValueTask<TResult>> executor) : base(name)
		{
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override async ValueTask<object> OnExecuteAsync(TerminalCommandContext context, CancellationToken cancellation)
		{
			return await _executor(context, cancellation);
		}
	}

	[Command(IgnoreOptions = true)]
	internal sealed class TerminalAsyncCommand<TArgument, TResult> : CommandBase<TerminalCommandContext>
	{
		private readonly TArgument _argument;
		private readonly Func<TerminalCommandContext, TArgument, CancellationToken, ValueTask<TResult>> _executor;

		public TerminalAsyncCommand(string name, Func<TerminalCommandContext, TArgument, CancellationToken, ValueTask> executor, TArgument argument) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_argument = argument;
			_executor = async (context, argument, token) =>
			{
				await executor.Invoke(context, argument, token);
				return default;
			};
		}

		public TerminalAsyncCommand(string name, Func<TerminalCommandContext, TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument) : base(name)
		{
			_argument = argument;
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override async ValueTask<object> OnExecuteAsync(TerminalCommandContext context, CancellationToken cancellation)
		{
			return await _executor(context, _argument, cancellation);
		}
	}
	#endregion
}
