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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Terminals;

public abstract class TerminalReactiveCommandBase : Components.CommandBase<TerminalCommandContext>
{
	#region 私有变量
	private readonly AutoResetEvent _semaphore;
	#endregion

	#region 构造函数
	protected TerminalReactiveCommandBase(string name) : base(name)
	{
		//创建信号量，默认为堵塞状态
		_semaphore = new AutoResetEvent(false);
	}
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(TerminalCommandContext context, CancellationToken cancellation)
	{
		//获取当前命令执行器对应的终端
		var terminal = context.Terminal ?? throw new NotSupportedException($"The {this.Name} command must be run in terminal environment.");

		//挂载当前终端的中断事件
		terminal.Aborting += this.Terminal_Aborting;

		//定义异常对象
		Exception exception = null;

		try
		{
			//进入被动响应模式
			await this.OnEnterAsync(context, cancellation);
		}
		catch(Exception ex)
		{
			//设置当前异常对象
			exception = ex;

			//释放信号量
			_semaphore.Set();
		}

		//等待信号量
		_semaphore.WaitOne();

		//注销当前终端的中断事件
		terminal.Aborting -= this.Terminal_Aborting;

		//退出被动响应模式
		await this.OnExitAsync(context, exception, cancellation);

		//返回结果
		return context.Result;
	}
	#endregion

	#region 抽象方法
	protected abstract ValueTask OnEnterAsync(TerminalCommandContext context, CancellationToken cancellation);
	protected abstract ValueTask OnExitAsync(TerminalCommandContext context, Exception exception, CancellationToken cancellation);
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
