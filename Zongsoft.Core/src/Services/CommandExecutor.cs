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
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Services
{
	public class CommandExecutor : ICommandExecutor
	{
		#region 声明事件
		public event EventHandler<CommandExecutorFailureEventArgs> Failed;
		public event EventHandler<CommandExecutorExecutingEventArgs> Executing;
		public event EventHandler<CommandExecutorExecutedEventArgs> Executed;
		#endregion

		#region 静态字段
		private static CommandExecutor _default;
		#endregion

		#region 成员字段
		private readonly CommandTreeNode _root;
		private readonly IDictionary<string, object> _states;
		private ICommandExpressionParser _parser;
		private ICommandOutlet _output;
		private TextWriter _error;
		#endregion

		#region 构造函数
		public CommandExecutor(ICommandExpressionParser parser = null)
		{
			_root = new CommandTreeNode();
			_parser = parser ?? CommandExpressionParser.Instance;
			_output = NullCommandOutlet.Instance;
			_error = CommandErrorWriter.Instance;
			_states = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 静态属性
		/// <summary>获取或设置默认的<see cref="CommandExecutor"/>命令执行器。</summary>
		public static CommandExecutor Default
		{
			get
			{
				if(_default == null)
					System.Threading.Interlocked.CompareExchange(ref _default, new CommandExecutor(), null);

				return _default;
			}
			set
			{
				_default = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 公共属性
		public CommandTreeNode Root => _root;
		public IDictionary<string, object> States => _states;
		public ICommandExpressionParser Parser
		{
			get => _parser;
			set => _parser = value ?? throw new ArgumentNullException();
		}

		public virtual ICommandOutlet Output
		{
			get => _output;
			set => _output = value ?? NullCommandOutlet.Instance;
		}

		public virtual TextWriter Error
		{
			get => _error;
			set => _error = value ?? TextWriter.Null;
		}
		#endregion

		#region 查找方法
		public virtual CommandTreeNode Find(string path) => _root.Find(path);
		#endregion

		#region 执行方法
		public object Execute(string expression, object argument = null)
		{
			if(string.IsNullOrWhiteSpace(expression))
				throw new ArgumentNullException(nameof(expression));

			CommandExecutorContext context = null;

			try
			{
				//创建命令执行器上下文对象
				context = this.CreateExecutorContext(expression, argument);

				if(context == null)
					throw new InvalidOperationException("Create executor context failed.");
			}
			catch(Exception ex)
			{
				//激发“Error”事件
				if(!this.OnFailed(context, ex))
					throw;

				return null;
			}

			//创建事件参数对象
			var executingArgs = new CommandExecutorExecutingEventArgs(context);

			//激发“Executing”事件
			this.OnExecuting(executingArgs);

			if(executingArgs.Cancel)
				return executingArgs.Result;

			var complateInvoked = false;
			IEnumerable<CommandCompletionContext> completes = null;

			try
			{
				//调用执行请求
				context.Result = this.OnExecute(context, out completes);
			}
			catch(Exception ex)
			{
				complateInvoked = true;

				//执行命令完成通知
				this.OnCompletes(completes, ex);

				//激发“Error”事件
				if(!this.OnFailed(context, ex))
					throw;
			}

			//执行命令完成通知
			if(!complateInvoked)
				this.OnCompletes(completes, null);

			//创建事件参数对象
			var executedArgs = new CommandExecutorExecutedEventArgs(context);

			//激发“Executed”事件
			this.OnExecuted(executedArgs);

			//返回最终的执行结果
			return executedArgs.Result;
		}
		#endregion

		#region 执行实现
		protected virtual object OnExecute(CommandExecutorContext session, out IEnumerable<CommandCompletionContext> completes)
		{
			var queue = new Queue<Tuple<CommandExpression, CommandTreeNode>>();
			var expression = session.Expression;

			//设置输出参数默认值
			completes = System.Linq.Enumerable.Empty<CommandCompletionContext>();

			while(expression != null)
			{
				//查找指定路径的命令节点
				var node = this.Find(expression.FullPath);

				//如果指定的路径在命令树中是不存在的则抛出异常
				if(node == null)
					throw new CommandNotFoundException(expression.FullPath);

				//将命令表达式的选项集绑定到当前命令上
				if(node.Command != null)
					expression.Options.Bind(node.Command);

				//将找到的命令表达式和对应的节点加入队列中
				queue.Enqueue(new Tuple<CommandExpression, CommandTreeNode>(expression, node));

				//设置下一个待搜索的命令表达式
				expression = expression.Next;
			}

			//如果队列为空则返回空
			if(queue.Count < 1)
				return null;

			//创建输出参数的列表对象
			completes = new List<CommandCompletionContext>();

			//初始化第一个输入参数
			var parameter = session.Parameter;

			while(queue.Count > 0)
			{
				var entry = queue.Dequeue();

				//创建命令执行上下文
				var context = this.CreateCommandContext(session, entry.Item1, entry.Item2, parameter);

				//如果命令上下文为空或命令空则直接返回
				if(context == null || context.Command == null)
					return null;

				//执行当前命令
				parameter = this.OnExecuteCommand(context);

				//判断命令是否需要完成通知，如果是则加入到清理列表中
				if(context.Command is ICommandCompletion completion)
					((ICollection<CommandCompletionContext>)completes).Add(new CommandCompletionContext(context, parameter));
			}

			//返回最后一个命令的执行结果
			return parameter;
		}

		protected virtual object OnExecuteCommand(CommandContext context)
		{
			var command = context?.Command;

			if(command == null)
				throw new InvalidOperationException("Missing command.");

			return command.Execute(context);
		}
		#endregion

		#region 保护方法
		protected virtual CommandExecutorContext CreateExecutorContext(string commandText, object parameter)
		{
			//解析当前命令文本
			var expression = this.OnParse(commandText);

			if(expression == null)
				throw new InvalidOperationException($"Invalid command expression text: {commandText}.");

			return new CommandExecutorContext(this, expression, parameter);
		}

		protected virtual CommandContext CreateCommandContext(CommandExecutorContext session, CommandExpression expression, CommandTreeNode node, object parameter)
		{
			return node == null || node.Command == null ? null : new CommandContext(session, expression, node, parameter);
		}

		protected virtual CommandExpression OnParse(string text)
		{
			return _parser.Parse(text);
		}
		#endregion

		#region 激发事件
		protected bool OnFailed(CommandExecutorContext context, Exception ex)
		{
			var args = new CommandExecutorFailureEventArgs(context, ex);

			//激发“Failed”事件
			this.OnFailed(args);

			//输出异常信息
			if(!args.ExceptionHandled && args.Exception != null)
				this.Error.WriteLine(args.Exception);

			return args.ExceptionHandled;
		}

		protected virtual void OnFailed(CommandExecutorFailureEventArgs args) => this.Failed?.Invoke(this, args);
		protected virtual void OnExecuting(CommandExecutorExecutingEventArgs args) => this.Executing?.Invoke(this, args);
		protected virtual void OnExecuted(CommandExecutorExecutedEventArgs args) => this.Executed?.Invoke(this, args);
		#endregion

		#region 私有方法
		private void OnCompletes(IEnumerable<CommandCompletionContext> contexts, Exception exception)
		{
			if(contexts == null)
				return;

			foreach(var context in contexts)
			{
				//设置会话完成通知上下文的异常属性
				context.Exception = exception;

				//回调命令执行器会话完成通知
				((ICommandCompletion)context.Command).OnCompleted(context);
			}
		}
		#endregion

		#region 嵌套子类
		private class NullCommandOutlet : ICommandOutlet
		{
			#region 单例字段
			public static readonly ICommandOutlet Instance = new NullCommandOutlet();
			#endregion

			#region 公共属性
			public Encoding Encoding
			{
				get => Encoding.UTF8;
				set { }
			}
			public TextWriter Writer => TextWriter.Null;
			#endregion

			#region 公共方法
			public void Write(object value) { }
			public void Write(string text) { }
			public void Write(CommandOutletColor color, object value) { }
			public void Write(CommandOutletColor color, string text) { }
			public void Write(string format, params object[] args) { }
			public void Write(CommandOutletContent content) { }
			public void Write(CommandOutletColor color, CommandOutletContent content) { }
			public void Write(CommandOutletColor color, string format, params object[] args) { }
			public void WriteLine() { }
			public void WriteLine(object value) { }
			public void WriteLine(string text) { }
			public void WriteLine(CommandOutletColor color, object value) { }
			public void WriteLine(CommandOutletColor color, string text) { }
			public void WriteLine(string format, params object[] args) { }
			public void WriteLine(CommandOutletContent content) { }
			public void WriteLine(CommandOutletColor color, CommandOutletContent content) { }
			public void WriteLine(CommandOutletColor color, string format, params object[] args) { }
			#endregion
		}

		private class CommandErrorWriter : TextWriter
		{
			#region 单例字段
			public static readonly TextWriter Instance = new CommandErrorWriter();
			#endregion

			#region 实例字段
			private readonly Zongsoft.Diagnostics.Logger _logger = Zongsoft.Diagnostics.Logger.GetLogger<CommandExecutor>();
			#endregion

			#region 公共属性
			public override Encoding Encoding => Encoding.UTF8;
			#endregion

			#region 重写方法
			public override void Write(object value)
			{
				if(value == null)
					return;

				switch(value)
				{
					case string text:
						_logger.Error(text);
						break;
					case StringBuilder buffer:
						_logger.Error(buffer.ToString());
						break;
					default:
						_logger.Error("An error occurred.", value);
						break;
				}
			}

			public override void Write(string value) => _logger.Error(value);
			public override void Write(string format, params object[] args) => _logger.Error(string.Format(format, args));
			public override Task WriteAsync(string value) => Task.Run(() => _logger.Error(value));
			public override void WriteLine(object value)
			{
				if(value == null)
					return;

				switch(value)
				{
					case string text:
						_logger.Error(text + Environment.NewLine);
						break;
					case StringBuilder buffer:
						_logger.Error(buffer.ToString() + Environment.NewLine);
						break;
					default:
						_logger.Error("An error occurred.", value);
						break;
				}
			}

			public override void WriteLine(string value) => _logger.Error(value + this.NewLine);
			public override void WriteLine(string format, params object[] args) => _logger.Error(string.Format(format, args) + this.NewLine);
			public override Task WriteLineAsync(string value) => Task.Run(() => _logger.Error(value + this.NewLine));
			#endregion
		}
		#endregion
	}
}