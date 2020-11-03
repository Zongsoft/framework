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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Scheduling
{
	public class SchedulerProvider : ISchedulerProvider, ICollection<IScheduler>
	{
		#region 单例字段
		public static readonly SchedulerProvider Default = new SchedulerProvider();
		#endregion

		#region 成员字段
		private readonly ConcurrentDictionary<string, IScheduler> _schedulers;
		#endregion

		#region 构造函数
		public SchedulerProvider()
		{
			_schedulers = new ConcurrentDictionary<string, IScheduler>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count => _schedulers.Count;
		bool ICollection<IScheduler>.IsReadOnly => false;
		#endregion

		#region 公共方法
		public bool Register(IScheduler scheduler)
		{
			if(scheduler == null)
				throw new ArgumentNullException(nameof(scheduler));

			return _schedulers.TryAdd(scheduler.Name, scheduler);
		}

		public bool Unregister(string name)
		{
			return _schedulers.TryRemove(name, out _);
		}

		public void Clear()
		{
			_schedulers.Clear();
		}

		public bool Contains(string name)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			return _schedulers.ContainsKey(name);
		}

		public IScheduler GetScheduler(string name)
		{
			if(name != null && _schedulers.TryGetValue(name, out var scheduler))
				return scheduler;

			return null;
		}

		bool ICollection<IScheduler>.Contains(IScheduler item)
		{
			if(item == null)
				return false;

			return _schedulers.ContainsKey(item.Name);
		}

		void ICollection<IScheduler>.Add(IScheduler item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			_schedulers.TryAdd(item.Name, item);
		}

		bool ICollection<IScheduler>.Remove(IScheduler item)
		{
			return _schedulers.TryRemove(item.Name, out _);
		}

		void ICollection<IScheduler>.CopyTo(IScheduler[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));
			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			int index = arrayIndex;

			foreach(var schedule in _schedulers.Values)
			{
				array[index] = schedule;
			}
		}
		#endregion

		#region 遍历迭代
		public IEnumerator<IScheduler> GetEnumerator()
		{
			foreach(var scheduler in _schedulers.Values)
				yield return scheduler;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach(var scheduler in _schedulers.Values)
				yield return scheduler;
		}
		#endregion
	}
}
