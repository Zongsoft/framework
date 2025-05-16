﻿/*
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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Components;

public partial class CommandExecutor : ICommandExecutor
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
	private readonly CommandNode _root;
	private ICommandExpressionParser _parser;
	private ICommandInvoker _invoker;
	private ICommandOutlet _output;
	private TextWriter _error;
	#endregion

	#region 构造函数
	public CommandExecutor(ICommandExpressionParser parser = null) : this(null, parser) { }
	public CommandExecutor(ICommandInvoker invoker, ICommandExpressionParser parser = null)
	{
		_root = new CommandNode();
		_invoker = invoker ?? CommandInvoker.Default;
		_parser = parser ?? CommandExpressionParser.Instance;
		_output = NullCommandOutlet.Instance;
		_error = CommandErrorWriter.Instance;
		this.States = new();
		this.Aliaser = new CommandNode.Aliaser(_root);
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
	public CommandNode Root => _root;
	public CommandNode.Aliaser Aliaser { get; }
	public Collections.Parameters States { get; }

	public ICommandInvoker Invoker
	{
		get => _invoker;
		set => _invoker = value ?? CommandInvoker.Default;
	}

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
	public virtual CommandNode Find(string path) => _root.Find(path);
	#endregion

	#region 执行方法
	public async ValueTask<object> ExecuteAsync(string expression, object value = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(expression))
			throw new ArgumentNullException(nameof(expression));

		//创建命令执行上下文
		var context = this.CreateContext(expression, value);

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
			(var result, completes) = await this.OnExecuteAsync(context, cancellation);

			//更新上下文的结果
			context.Result = result;
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
	protected virtual async ValueTask<(object result, IEnumerable<CommandCompletionContext> completes)> OnExecuteAsync(CommandExecutorContext session, CancellationToken cancellation)
	{
		var queue = new Queue<Tuple<CommandExpression, CommandNode>>();
		var expression = session.Expression;

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
			queue.Enqueue(new Tuple<CommandExpression, CommandNode>(expression, node));

			//设置下一个待搜索的命令表达式
			expression = expression.Next;
		}

		//如果队列为空则返回空
		if(queue.Count < 1)
			return default;

		//创建输出参数的列表对象
		var completes = new List<CommandCompletionContext>();

		//初始化第一个输入参数
		var value = session.Value;

		while(queue.Count > 0)
		{
			var entry = queue.Dequeue();

			//创建命令执行上下文
			var context = this.CreateContext(session, entry.Item1, entry.Item2, value);

			//如果命令上下文为空或命令空则直接返回
			if(context == null || context.Command == null)
				return default;

			//执行当前命令
			value = await this.OnExecuteAsync(context, cancellation);

			//判断命令是否需要完成通知，如果是则加入到清理列表中
			if(context.Command is ICommandCompletion completion)
				((ICollection<CommandCompletionContext>)completes).Add(new CommandCompletionContext(context, value));
		}

		//返回最后一个命令的执行结果
		return (value, completes);
	}

	protected virtual ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) => this.Invoker.InvokeAsync(context, cancellation);
	#endregion

	#region 虚拟方法
	protected virtual CommandExecutorContext CreateContext(string commandText, object value)
	{
		//解析当前命令文本
		var expression = this.OnParse(commandText) ?? throw new InvalidOperationException($"Invalid command expression text: {commandText}.");
		return new CommandExecutorContext(this, expression, value);
	}

	protected virtual CommandContext CreateContext(CommandExecutorContext context, CommandExpression expression, CommandNode node, object value) =>
		node == null || node.Command == null ? null : new CommandContext(context, expression, node, value);

	protected virtual CommandExpression OnParse(string text) => _parser.Parse(text);
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
	private class CommandInvoker : ICommandInvoker
	{
		public static readonly ICommandInvoker Default = new CommandInvoker();

		public ValueTask<object> InvokeAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			return context.Command != null ? context.Command.ExecuteAsync(context, cancellation) : throw new InvalidOperationException("Missing executable commands.");
		}
	}

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
		public void Write(char character) { }
		public void Write(CommandOutletColor color, object value) { }
		public void Write(CommandOutletColor color, string text) { }
		public void Write(CommandOutletColor color, char character) { }
		public void Write(string format, params object[] args) { }
		public void Write(CommandOutletContent content) { }
		public void Write(CommandOutletColor color, CommandOutletContent content) { }
		public void Write(CommandOutletColor color, string format, params object[] args) { }
		public void WriteLine() { }
		public void WriteLine(object value) { }
		public void WriteLine(string text) { }
		public void WriteLine(char character) { }
		public void WriteLine(CommandOutletColor color, object value) { }
		public void WriteLine(CommandOutletColor color, string text) { }
		public void WriteLine(CommandOutletColor color, char character) { }
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