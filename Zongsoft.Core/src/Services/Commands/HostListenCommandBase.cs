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
using System.Threading;

namespace Zongsoft.Services.Commands
{
	public abstract class HostListenCommandBase<THost> : CommandBase<CommandContext> where THost : class
	{
		#region 私有变量
		private CommandContext _context;
		private AutoResetEvent _semaphore;
		#endregion

		#region 构造函数
		protected HostListenCommandBase() : base("Listen")
		{
			//创建信号量，默认为堵塞状态
			_semaphore = new AutoResetEvent(false);
		}

		protected HostListenCommandBase(string name) : base(name)
		{
			//创建信号量，默认为堵塞状态
			_semaphore = new AutoResetEvent(false);
		}
		#endregion

		#region 保护属性
		protected CommandContext Context { get => _context; }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			//获取当前命令执行器对应的终端
			var terminal = (context.Executor as Zongsoft.Terminals.TerminalCommandExecutor)?.Terminal;

			//如果当前命令执行器不是终端命令执行器则抛出不支持的异常
			if(terminal == null)
				throw new NotSupportedException("The listen command must be run in terminal executor.");

			//查找依赖的宿主对象
			var host = this.Find(context);

			//如果宿主查找失败，则抛出异常
			if(host == null)
				throw new CommandException("Missing required host of depends on.");

			//保持当前命令执行上下文
			_context = context;

			//挂载当前终端的中断事件
			terminal.Aborting += this.Terminal_Aborting;

			//调用侦听开始方法
			this.OnListening(context, host);

			//等待信号量
			_semaphore.WaitOne();

			//注销当前终端的中断事件
			terminal.Aborting -= this.Terminal_Aborting;

			//调用侦听结束方法
			this.OnListened(context, host);

			//将当前命令执行上下文置空
			_context = null;

			//返回执行成功的工作者
			return host;
		}
		#endregion

		#region 查找方法
		protected virtual THost Find(CommandContext context)
		{
			var found = context.CommandNode.Find(node => node.Command is HostCommandBase<THost> command && command.Host != null, true);
			return found == null ? null : ((HostCommandBase<THost>)found.Command).Host as THost;
		}
		#endregion

		#region 抽象方法
		protected abstract void OnListening(CommandContext context, THost host);
		protected abstract void OnListened(CommandContext context, THost host);
		#endregion

		#region 事件处理
		private void Terminal_Aborting(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//阻止命令执行器被关闭
			e.Cancel = true;

			//释放信号量
			_semaphore.Set();
		}
		#endregion
	}
}
