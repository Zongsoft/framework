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
 * This file is part of Zongsoft.Scheduling library.
 *
 * The Zongsoft.Scheduling is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Scheduling is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Scheduling library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Scheduling.Messaging
{
	public abstract class MessageQueueHandlerBase : IExecutionHandler<IExecutionContext>
	{
		#region 构造函数
		protected MessageQueueHandlerBase() { }
		#endregion

		#region 公共方法
		public bool CanHandle(IExecutionContext context)
		{
			return context != null && context.Data != null;
		}

		public void Handle(IExecutionContext context)
		{
			var message = context.Data as MessageBase;

			if(message == null || message.Data == null || message.Data.Length == 0)
				return;

			if(this.OnHandle(message.Data))
				message.Acknowledge();
		}

		public Task HandleAsync(IExecutionContext context)
		{
			this.Handle(context);
			return Task.CompletedTask;
		}
		#endregion

		#region 抽象方法
		protected abstract bool OnHandle(byte[] data);
		#endregion

		#region 显式实现
		bool IExecutionHandler.CanHandle(object context)
		{
			return this.CanHandle(context as IExecutionContext);
		}

		void IExecutionHandler.Handle(object context)
		{
			this.Handle(context as IExecutionContext);
		}

		Task IExecutionHandler.HandleAsync(object context)
		{
			return this.HandleAsync(context as IExecutionContext);
		}
		#endregion
	}
}
