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
using System.Collections.Generic;

namespace Zongsoft.Scheduling
{
	/// <summary>
	/// 表示调度器的接口。
	/// </summary>
	public interface IScheduler : Zongsoft.Services.IWorker
	{
		#region 事件声明
		/// <summary>表示调度项已经过期的事件。</summary>
		event EventHandler<ExpiredEventArgs> Expired;

		/// <summary>表示一个处理器执行完成的事件。</summary>
		/// <remarks>
		/// 	<para>可通过<seealso cref="HandledEventArgs.Exception"/>属性来确认最近一次处理是否成功。</para>
		/// 	<para>可通过<seealso cref="IHandlerContext.Failure"/>属性来获取重试情况的信息。</para>
		/// </remarks>
		event EventHandler<HandledEventArgs> Handled;

		/// <summary>表示任务被触发执行完成的事件。</summary>
		/// <remarks>即使任务处理执行中的所有处理器都调用失败，该事件也会发生。</remarks>
		event EventHandler<OccurredEventArgs> Occurred;

		/// <summary>表示任务被触发执行之前的事件。</summary>
		event EventHandler<OccurringEventArgs> Occurring;

		/// <summary>表示一个处理器调度完成的事件。</summary>
		event EventHandler<ScheduledEventArgs> Scheduled;
		#endregion

		#region 属性声明
		/// <summary>获取一个值，指示调度项的记录数。</summary>
		int Count { get; }

		/// <summary>获取一个值，指示最近一次调度的时间。</summary>
		DateTime? LastTime { get; }

		/// <summary>获取一个值，指示下一次调度的时间。</summary>
		DateTime? NextTime { get; }

		/// <summary>获取或设置调度失败的重试器。</summary>
		IRetriever Retriever { get; set; }

		/// <summary>获取调度器中的调度触发器集。</summary>
		IReadOnlyCollection<ITrigger> Triggers { get; }

		/// <summary>获取一个值，指示当前调度器是否处于工作中。</summary>
		bool IsScheduling { get; }

		/// <summary>获取一个值，指示当前调度器是否含有附加数据。</summary>
		bool HasStates { get; }

		/// <summary>获取当前调度器的附加数据字典。</summary>
		IDictionary<string, object> States { get; }
		#endregion

		#region 方法声明
		/// <summary>
		/// 获取指定触发器中关联的处理器。
		/// </summary>
		/// <param name="trigger">指定要获取的触发器。</param>
		/// <returns>返回指定触发器中关联的处理器集。</returns>
		HandlerToken[] GetHandlers(ITrigger trigger);

		/// <summary>
		/// 排程操作，将指定的处理器与触发器绑定。
		/// </summary>
		/// <param name="handler">指定要绑定的处理器。</param>
		/// <param name="trigger">指定要调度的触发器。</param>
		/// <returns>返回排程成功的调度项编号，零表示排程失败。</returns>
		long Schedule(IHandler handler, ITrigger trigger);

		/// <summary>
		/// 重新排程，将指定的处理器绑定到新的触发器并自动将其关联的原触发器解绑。
		/// </summary>
		/// <param name="scheduleId">指定要重排的调度项编号。</param>
		/// <param name="trigger">指定要调度的新触发器。</param>
		/// <returns>如果重排成功则返回真(True)，否则返回假(False)。</returns>
		bool Reschedule(long scheduleId, ITrigger trigger);

		/// <summary>
		/// 清空所有排程，即将调度器中的所有绑定关系解除。
		/// </summary>
		void Unschedule();

		/// <summary>
		/// 解除指定触发器的所有排程。
		/// </summary>
		/// <param name="trigger">指定要解除的触发器。</param>
		/// <returns>如果解除成功则返回真(False)，否则返回假(False)。</returns>
		bool Unschedule(ITrigger trigger);

		/// <summary>
		/// 解除指定处理器的所有排程。
		/// </summary>
		/// <param name="scheduleId">指定要解除的调度项编号。</param>
		/// <returns>如果解除成功则返回真(True)，否则返回假(False)。</returns>
		bool Unschedule(long scheduleId);
		#endregion
	}
}
