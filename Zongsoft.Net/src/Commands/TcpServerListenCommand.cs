/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Net.Commands
{
	public class TcpServerListenCommand : Zongsoft.Services.Commands.WorkerListenCommandBase<TcpServer>
	{
		#region 私有变量
		private IHandler<ReadOnlySequence<byte>> _handler;
		#endregion

		#region 构造函数
		public TcpServerListenCommand() : this("Listen") { }
		public TcpServerListenCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override void OnListening(CommandContext context, TcpServer worker)
		{
			//通过基类方法打印入口信息
			base.OnListening(context, worker);

			//保存原处理器
			_handler = worker.Handler;

			//挂载侦听处理器
			worker.Handler = new TcpListenHandler(context);
		}

		protected override void OnListened(CommandContext context, TcpServer worker)
		{
			//还原处理器
			worker.Handler = _handler;
		}
		#endregion
	}
}
