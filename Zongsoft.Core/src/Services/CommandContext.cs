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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Services
{
	/// <summary>
	/// 表示命令执行的上下文类。
	/// </summary>
	public class CommandContext
	{
		#region 成员字段
		private IDictionary<string, object> _states;
		#endregion

		#region 构造函数
		public CommandContext(CommandExecutorContext session, CommandExpression expression, ICommand command, object parameter, IDictionary<string, object> extendedProperties = null)
		{
			this.Session = session;
			this.Command = command ?? throw new ArgumentNullException(nameof(command));
			this.Parameter = parameter;
			this.Expression = expression;

			if(extendedProperties != null && extendedProperties.Count > 0)
				_states = new Dictionary<string, object>(extendedProperties, StringComparer.OrdinalIgnoreCase);
		}

		public CommandContext(CommandExecutorContext session, CommandExpression expression, CommandTreeNode commandNode, object parameter, IDictionary<string, object> extendedProperties = null)
		{
			if(commandNode == null)
				throw new ArgumentNullException(nameof(commandNode));

			if(commandNode.Command == null)
				throw new ArgumentException($"The Command property of '{commandNode.FullPath}' command-node is null.");

			this.Session = session;
			this.CommandNode = commandNode;
			this.Command = commandNode.Command;
			this.Parameter = parameter;
			this.Expression = expression;

			if(extendedProperties != null && extendedProperties.Count > 0)
				_states = new Dictionary<string, object>(extendedProperties, StringComparer.OrdinalIgnoreCase);
		}

		protected CommandContext(CommandContext context)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			this.Session = context.Session;
			this.Expression = context.Expression;
			this.Command = context.Command;
			this.CommandNode = context.CommandNode;
			this.Parameter = context.Parameter;
			_states = context._states;
		}
		#endregion

		#region 公共属性
		/// <summary>获取执行的命令对象。</summary>
		public ICommand Command { get; }

		/// <summary>获取执行的命令所在节点。</summary>
		public CommandTreeNode CommandNode { get; }

		/// <summary>获取命令执行的传入参数。</summary>
		public object Parameter { get; }

		/// <summary>获取当前命令对应的表达式。</summary>
		public CommandExpression Expression { get; }

		/// <summary>获取一个值，指示当前上下文是否包含状态字典。</summary>
		/// <remarks>
		///		<para>在不确定状态字典是否含有内容之前，建议先使用该属性来检测。</para>
		/// </remarks>
		public bool HasStates => _states != null && _states.Count > 0;

		/// <summary>获取当前上下文的状态字典。</summary>
		public IDictionary<string, object> States
		{
			get
			{
				if(_states == null)
					System.Threading.Interlocked.CompareExchange(ref _states, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _states;
			}
		}

		/// <summary>获取当前命令的标准输出器。</summary>
		public virtual ICommandOutlet Output => this.Session?.Executor?.Output;

		/// <summary>获取当前命令的错误输出器。</summary>
		public virtual TextWriter Error => this.Session?.Executor?.Error;

		/// <summary>获取命令所在的命令执行器。</summary>
		public ICommandExecutor Executor => this.Session?.Executor;

		/// <summary>获取当前执行命令的会话，即命令管道执行上下文。</summary>
		public CommandExecutorContext Session { get; }
		#endregion
	}
}
