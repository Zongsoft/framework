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

namespace Zongsoft.Scheduling
{
	/// <summary>
	/// 提供调度失败的重试机制的接口。
	/// </summary>
	public interface IRetriever
	{
		#region 事件定义
		/// <summary>表示重试丢弃的事件。</summary>
		event EventHandler<HandledEventArgs> Discarded;

		/// <summary>表示重试失败的事件。</summary>
		event EventHandler<HandledEventArgs> Failed;

		/// <summary>表示重试成功的事件。</summary>
		event EventHandler<HandledEventArgs> Succeed;
		#endregion

		#region 方法定义
		/// <summary>启动重试。</summary>
		void Run();

		/// <summary>停止重试。</summary>
		/// <param name="clean">指定是否清空积压的重试队列，如果为真则清空重试队列，否则不清理。</param>
		void Stop(bool clean);

		/// <summary>将指定的调度处理加入到重试队列。</summary>
		/// <param name="handler">指定要重试的处理器。</param>
		/// <param name="context">指定要重试的处理上下文对象。</param>
		/// <param name="exception">指定导致要重试的异常。</param>
		/// <remarks>
		///		<para>该方法会自动触发启动操作。</para>
		/// </remarks>
		void Retry(IHandler handler, IHandlerContext context, Exception exception = null);
		#endregion
	}
}
