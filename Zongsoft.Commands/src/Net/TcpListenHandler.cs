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
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Communication.Net.Commands
{
	internal class TcpListenHandler : IHandler<string>
	{
		#region 成员字段
		private readonly CommandContext _context;
		#endregion

		#region 构造函数
		public TcpListenHandler(CommandContext context)
		{
			_context = context;
		}
		#endregion

		#region 公共属性
		public bool CanHandle(string request) => true;
		public bool CanHandle(object request) => true;
		#endregion

		#region 公共方法
		public bool Handle(string request)
		{
			_context.Output.WriteLine(request);
			return true;
		}

		public bool Handle(object request)
		{
			_context.Output.WriteLine(request);
			return true;
		}

		public Task<bool> HandleAsync(string request, CancellationToken cancellation) => Task.FromResult(this.Handle(request));
		public Task<bool> HandleAsync(object request, CancellationToken cancellation = default) => Task.FromResult(this.Handle(request));
		#endregion
	}
}
