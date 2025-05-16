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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Terminals;

partial class Terminal
{
	private class ConsoleExecutor : CommandExecutor, ITerminalExecutor
	{
		#region 事件声明
		public event EventHandler CurrentChanged;
		public event EventHandler<ExitEventArgs> Exit;
		#endregion

		#region 成员字段
		private ITerminal _terminal;
		private CommandNode _current;
		#endregion

		#region 构造函数
		public ConsoleExecutor(ITerminal terminal, ICommandExpressionParser parser = null) : base(parser)
		{
			_terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
			_terminal.Resetted += this.Terminal_Resetted;

			//添加内置命令
			this.Root.Children.Add(new Commands.ExitCommand());
			this.Root.Children.Add(new Commands.ClearCommand());
			this.Root.Children.Add(new Commands.ShellCommand());
		}
		#endregion

		#region 公共属性
		public ITerminal Terminal => _terminal;
		public CommandNode Current
		{
			get => _current;
			private set
			{
				if(object.ReferenceEquals(_current, value))
					return;

				_current = value;

				//激发“CurrentChanged”事件
				this.OnCurrentChanged(EventArgs.Empty);
			}
		}

		public override ICommandOutlet Output
		{
			get => _terminal;
			set => throw new NotSupportedException();
		}

		public override TextWriter Error
		{
			get => _terminal.Error;
			set => _terminal.Error = value ?? TextWriter.Null;
		}
		#endregion

		#region 运行方法
		public int Run(CancellationToken cancellation = default) => this.Run(null, cancellation);
		public int Run(string splash, CancellationToken cancellation = default)
		{
			return this.RunAsync(splash, cancellation).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Task<int> RunAsync(CancellationToken cancellation = default) => this.RunAsync(null, cancellation);
		public async Task<int> RunAsync(string splash, CancellationToken cancellation = default)
		{
			if(this.Root.Children.Count < 1)
				return 0;

			if(string.IsNullOrEmpty(splash))
				splash = SPLASH;

			//打印闪屏信息
			_terminal.WriteLine(splash);

			while(true)
			{
				//重置控制台，准备执行命令
				_terminal.Reset();

				//如果已经取消运行则退出
				if(cancellation.IsCancellationRequested)
					return 0;

				try
				{
					var commandText = _terminal.Input.ReadLine();

					if(!string.IsNullOrWhiteSpace(commandText))
						await this.ExecuteAsync(commandText, null, cancellation);
				}
				catch(OperationCanceledException)
				{
					return 0;
				}
				catch(ExitException ex)
				{
					if(this.RaiseExit(ex.ExitCode))
						return ex.ExitCode;
				}
			}
		}
		#endregion

		#region 查找方法
		public override CommandNode Find(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return null;

			var current = _current;

			if(current == null)
				return base.Find(path);

			//如果查找的路径完全等于当前节点名，则优先返回当前节点
			if(string.Equals(path, current.Name, StringComparison.OrdinalIgnoreCase))
				return current;

			//以当前节点为锚点开始查找
			var node = current.Find(path);

			//如果以当前节点为锚点查找失败，并且查找的路径没有指定特定的锚点，则再从根节点查找一次并返回其查找结果
			if(node == null && path[0] != '.' && path[0] != '/')
				return this.Root.Find(path);

			return node;
		}
		#endregion

		#region 重写方法
		protected override void OnExecuted(CommandExecutorExecutedEventArgs args)
		{
			var last = args.Context.Expression;

			//从执行器的命令表达式中找出最后一个命令表达式
			while(last != null && last.Next != null)
			{
				last = last.Next;
			}

			//查找表达式中最后一个命令节点
			var node = this.Find(last.FullPath);

			//更新当前命令节点，只有命令树节点不是叶子节点并且为空命令节点
			if(node != null && node.Children.Count > 0 && node.Command == null)
				this.Current = node;

			//调用基类同名方法
			base.OnExecuted(args);
		}

		protected override void OnFailed(CommandExecutorFailureEventArgs args)
		{
			//调用基类同名方法
			base.OnFailed(args);

			if(args.Exception is Terminal.ExitException)
				args.Exception = null;
			else
			{
				args.ExceptionHandled = true;
				this.Terminal.WriteLine(CommandOutletColor.Red, Properties.Resources.CommandError_Label + args.Exception.Message);
			}
		}
		#endregion

		#region 激发事件
		private bool RaiseExit(int exitCode)
		{
			var args = new ExitEventArgs(exitCode);

			//激发“Exit”退出事件
			this.OnExit(args);

			return !args.Cancel;
		}

		protected virtual void OnExit(ExitEventArgs args) => this.Exit?.Invoke(this, args);
		protected virtual void OnCurrentChanged(EventArgs args) => this.CurrentChanged?.Invoke(this, args);
		#endregion

		#region 终端重置
		private void Terminal_Resetted(object sender, EventArgs e)
		{
			if(_current == null || _current == this.Root)
				_terminal.Write("$>");
			else
				_terminal.Write(_current.FullPath + ">");
		}
		#endregion
	}
}