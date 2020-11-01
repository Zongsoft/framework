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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Scheduling
{
	public abstract class Scheduler<TKey, TData> : Scheduler where TKey : struct, IEquatable<TKey> where TData : class
	{
		#region 成员字段
		private readonly ConcurrentDictionary<TKey, ISchedule<TKey, TData>> _schedules;
		#endregion

		#region 构造函数
		public Scheduler()
		{
			_schedules = new ConcurrentDictionary<TKey, ISchedule<TKey, TData>>();
		}
		#endregion

		#region 公共属性
		public ICollection<ISchedule<TKey, TData>> Schedules { get => _schedules.Values; }
		#endregion

		#region 公共方法
		public long Schedule(TKey key)
		{
			var schedule = this.GetSchedules(new[] { key }).FirstOrDefault();

			if(schedule == null)
			{
				this.Unschedule(key);
				return 0;
			}

			if(_schedules.TryAdd(key, schedule))
				return this.Schedule(this.GetHandler(schedule), this.GetTrigger(schedule), schedule.Data);

			return this.Reschedule(schedule.ScheduleId, this.GetTrigger(schedule), schedule.Data) ? schedule.ScheduleId : 0;
		}

		public bool Unschedule(TKey key)
		{
			if(_schedules.TryGetValue(key, out var schedule))
				return this.Unschedule(schedule.ScheduleId);

			return false;
		}
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			//初始化调度记录
			this.Initialize();

			//调用基类同名方法
			base.OnStart(args);
		}

		protected override void OnStop(string[] args)
		{
			//清空所有调度数据
			this.Unschedule();

			//调用基类同名方法
			base.OnStop(args);
		}
		#endregion

		#region 初始化器
		protected virtual void Initialize()
		{
			var schedules = this.GetSchedules(null);

			if(schedules == null)
				return;

			foreach(var schedule in schedules)
			{
				if(schedule != null && _schedules.TryAdd(schedule.Key, schedule))
					this.Schedule(this.GetHandler(schedule), this.GetTrigger(schedule));
			}
		}
		#endregion

		#region 抽象方法
		protected abstract ITrigger GetTrigger(ISchedule<TKey, TData> schedule);
		protected abstract IHandler GetHandler(ISchedule<TKey, TData> schedule);
		protected abstract IEnumerable<ISchedule<TKey, TData>> GetSchedules(IEnumerable<TKey> keys);
		#endregion
	}
}
