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
	public readonly struct HandlerToken : IEquatable<HandlerToken>
	{
		#region 构造函数
		public HandlerToken(long scheduleId, IHandler handler, object data)
		{
			this.ScheduleId = scheduleId;
			this.Handler = handler;
			this.Data = data;
		}
		#endregion

		#region 公共字段
		public readonly long ScheduleId;
		public readonly IHandler Handler;
		public readonly object Data;
		#endregion

		#region 重写方法
		public bool Equals(HandlerToken token)
		{
			return this.ScheduleId == token.ScheduleId;
		}

		public override bool Equals(object other)
		{
			return other is HandlerToken token && this.ScheduleId == token.ScheduleId;
		}

		public override int GetHashCode()
		{
			return this.ScheduleId.GetHashCode();
		}

		public override string ToString()
		{
			return this.ScheduleId + ":" + this.Handler.ToString() + Environment.NewLine + this.Data;
		}
		#endregion
	}
}
