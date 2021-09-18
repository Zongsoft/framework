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

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息订阅者的接口。
	/// </summary>
	/// <typeparam name="TMessage">订阅的消息类型。</typeparam>
	public interface IMessageSubscriber<TMessage>
	{
		#region 属性定义
		/// <summary>获取订阅的消息队列名称。</summary>
		string Name { get; }
		#endregion

		#region 方法定义
		/// <summary>取消当前的订阅。</summary>
		void Unsubscribe();

		/// <summary>取消当前的订阅。</summary>
		Task UnsubscribeAsync();

		/// <summary>
		/// 处理订阅的消息。
		/// </summary>
		/// <param name="message">待处理的消息。</param>
		bool Handle(TMessage message);

		/// <summary>
		/// 处理订阅的消息。
		/// </summary>
		/// <param name="message">待处理的消息。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		Task<bool> HandleAsync(TMessage message, CancellationToken cancellation = default);
		#endregion
	}
}
