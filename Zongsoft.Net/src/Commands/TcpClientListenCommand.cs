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

using Zongsoft.Components;
using Zongsoft.Services;
using Zongsoft.Services.Commands;

namespace Zongsoft.Net.Commands
{
	public class TcpClientListenCommand : HostListenCommandBase<TcpClient>
	{
		#region 私有变量
		private IHandler<ReadOnlySequence<byte>> _handler;
		#endregion

		#region 构造函数
		public TcpClientListenCommand() : this("Listen") { }
		public TcpClientListenCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override void OnListening(CommandContext context, TcpClient host)
		{
			//保存原处理器
			_handler = host.Handler;

			//挂载侦听处理器
			host.Handler = new TcpListenHandler(context);
		}

		protected override void OnListened(CommandContext context, TcpClient host)
		{
			//还原处理器
			host.Handler = _handler;
		}
		#endregion
	}
}
