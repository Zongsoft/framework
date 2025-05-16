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

namespace Zongsoft.Components;

public static class CommandExecutorUtility
{
	#region 同步命令
	public static ICommand Command(this ICommandExecutor executor, string name, Action<CommandContext> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new CommandWrapper<object>(name, command)).Command;
	}

	public static ICommand Command<TArgument>(this ICommandExecutor executor, string name, Action<CommandContext, TArgument> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new CommandWrapper<TArgument, object>(name, command, argument)).Command;
	}

	public static ICommand Command<TResult>(this ICommandExecutor executor, string name, Func<CommandContext, TResult> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new CommandWrapper<TResult>(name, command)).Command;
	}

	public static ICommand Command<TArgument, TResult>(this ICommandExecutor executor, string name, Func<CommandContext, TArgument, TResult> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new CommandWrapper<TArgument, TResult>(name, command, argument)).Command;
	}
	#endregion

	#region 异步命令
	public static ICommand Command(this ICommandExecutor executor, string name, Func<CommandContext, CancellationToken, ValueTask> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new AsyncCommandWrapper<object>(name, command)).Command;
	}

	public static ICommand Command<TResult>(this ICommandExecutor executor, string name, Func<CommandContext, CancellationToken, ValueTask<TResult>> command, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new AsyncCommandWrapper<TResult>(name, command)).Command;
	}

	public static ICommand Command<TArgument>(this ICommandExecutor executor, string name, Func<CommandContext, TArgument, CancellationToken, ValueTask> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new AsyncCommandWrapper<TArgument, object>(name, command, argument)).Command;
	}

	public static ICommand Command<TArgument, TResult>(this ICommandExecutor executor, string name, Func<CommandContext, TArgument, CancellationToken, ValueTask<TResult>> command, TArgument argument, string path = null)
	{
		if(executor == null)
			throw new ArgumentNullException(nameof(executor));
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var node = string.IsNullOrEmpty(path) || path == "/" ?
			executor.Root : executor.Find(path) ?? throw new CommandNotFoundException(path);

		return node.Children.Add(new AsyncCommandWrapper<TArgument, TResult>(name, command, argument)).Command;
	}
	#endregion

	#region 嵌套子类
	[Command(IgnoreOptions = true)]
	internal sealed class CommandWrapper<TResult> : CommandBase<CommandContext>
	{
		private readonly Func<CommandContext, TResult> _executor;

		public CommandWrapper(string name, Action<CommandContext> executor) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_executor = context =>
			{
				executor.Invoke(context);
				return default;
			};
		}

		public CommandWrapper(string name, Func<CommandContext, TResult> executor) : base(name)
		{
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => ValueTask.FromResult<object>(cancellation.IsCancellationRequested ? null : _executor(context));
	}

	[Command(IgnoreOptions = true)]
	internal sealed class CommandWrapper<TArgument, TResult> : CommandBase<CommandContext>
	{
		private readonly TArgument _argument;
		private readonly Func<CommandContext, TArgument, TResult> _executor;

		public CommandWrapper(string name, Action<CommandContext, TArgument> executor, TArgument argument) : base(name)
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

		public CommandWrapper(string name, Func<CommandContext, TArgument, TResult> executor, TArgument argument) : base(name)
		{
			_argument = argument;
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => ValueTask.FromResult<object>(cancellation.IsCancellationRequested ? null : _executor(context, _argument));
	}

	[Command(IgnoreOptions = true)]
	internal sealed class AsyncCommandWrapper<TResult> : CommandBase<CommandContext>
	{
		private readonly Func<CommandContext, CancellationToken, ValueTask<TResult>> _executor;

		public AsyncCommandWrapper(string name, Func<CommandContext, CancellationToken, ValueTask> executor) : base(name)
		{
			if(executor == null)
				throw new ArgumentNullException(nameof(executor));

			_executor = async (context, token) =>
			{
				await executor.Invoke(context, token);
				return default;
			};
		}

		public AsyncCommandWrapper(string name, Func<CommandContext, CancellationToken, ValueTask<TResult>> executor) : base(name)
		{
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			return await _executor(context, cancellation);
		}
	}

	[Command(IgnoreOptions = true)]
	internal sealed class AsyncCommandWrapper<TArgument, TResult> : CommandBase<CommandContext>
	{
		private readonly TArgument _argument;
		private readonly Func<CommandContext, TArgument, CancellationToken, ValueTask<TResult>> _executor;

		public AsyncCommandWrapper(string name, Func<CommandContext, TArgument, CancellationToken, ValueTask> executor, TArgument argument) : base(name)
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

		public AsyncCommandWrapper(string name, Func<CommandContext, TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument) : base(name)
		{
			_argument = argument;
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		}

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			return await _executor(context, _argument, cancellation);
		}
	}
	#endregion
}
