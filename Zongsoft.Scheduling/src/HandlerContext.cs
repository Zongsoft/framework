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
	public class HandlerContext : IHandlerContext
	{
		#region 成员字段
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public HandlerContext(IScheduler scheduler, long scheduleId, ITrigger trigger, string eventId, DateTime timestamp, int index, object data)
		{
			this.Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
			this.Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
			this.ScheduleId = scheduleId;
			this.EventId = eventId;
			this.Timestamp = timestamp;
			this.Index = index;
			this.Data = data;
		}
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public int Index { get; }

		/// <inheritdoc />
		public long ScheduleId { get; }

		/// <inheritdoc />
		public string EventId { get; }

		/// <inheritdoc />
		public DateTime Timestamp { get; }

		/// <inheritdoc />
		public HandlerFailure? Failure { get; set; }

		/// <inheritdoc />
		public IScheduler Scheduler { get; }

		/// <inheritdoc />
		public ITrigger Trigger { get; }

		/// <inheritdoc />
		public object Data { get;  }

		/// <inheritdoc />
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <inheritdoc />
		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}
		#endregion
	}
}
