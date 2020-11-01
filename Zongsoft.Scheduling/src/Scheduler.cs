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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;

namespace Zongsoft.Scheduling
{
	/// <summary>
	/// 提供定时任务调度功能的调度器。
	/// </summary>
	public class Scheduler : WorkerBase, IScheduler
	{
		#region 事件定义
		public event EventHandler<ExpiredEventArgs> Expired;
		public event EventHandler<HandledEventArgs> Handled;
		public event EventHandler<OccurredEventArgs> Occurred;
		public event EventHandler<OccurringEventArgs> Occurring;
		public event EventHandler<ScheduledEventArgs> Scheduled;
		#endregion

		#region 成员字段
		private long _sequence;
		private DateTime? _lastTime;
		private TaskToken _token;
		private IRetriever _retriever;
		private readonly ConcurrentDictionary<long, ScheduleToken> _schedules;
		private readonly ConcurrentDictionary<ITrigger, Dispatchable> _dispatchables;
		#endregion

		#region 构造函数
		public Scheduler()
		{
			this.CanPauseAndContinue = true;

			_sequence = 1;
			_retriever = new Retriever();
			_schedules = new ConcurrentDictionary<long, ScheduleToken>();
			_dispatchables = new ConcurrentDictionary<ITrigger, Dispatchable>();
		}
		#endregion

		#region 公共属性
		public int Count { get => _schedules.Count; }

		public DateTime? NextTime { get => _token?.Timestamp; }

		public DateTime? LastTime { get => _lastTime; }

		public IRetriever Retriever
		{
			get => _retriever;
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				//如果新值与原有值引用相等则忽略
				if(object.ReferenceEquals(_retriever, value))
					return;

				//更新属性值
				var original = Interlocked.Exchange(ref _retriever, value);

				//通知子类该属性值发生了改变
				this.OnRetrieverChanged(value, original);
			}
		}

		public IReadOnlyCollection<ITrigger> Triggers
		{
			get => _dispatchables.Keys as IReadOnlyCollection<ITrigger> ?? _dispatchables.Keys.ToArray();
		}

		public bool IsScheduling
		{
			get
			{
				var token = _token;
				return token != null && !token.IsCancellationRequested;
			}
		}
		#endregion

		#region 公共方法
		public HandlerToken[] GetHandlers(ITrigger trigger)
		{
			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));

			if(_dispatchables.TryGetValue(trigger, out var schedule))
				return schedule.Handlers;
			else
				return Array.Empty<HandlerToken>();
		}

		//public ISchedule GetSchedule(long scheduleId)
		//{
		//	if(_schedules.TryGetValue(scheduleId, out var schedule))
		//		return schedule;

		//	return null;
		//}

		public long Schedule(IHandler handler, ITrigger trigger, object data = null)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));

			if(trigger.IsExpired())
				return 0;

			//将该处理器加入到指定的触发器中的调度处理集
			var scheduleId = this.ScheduleCore(handler, trigger, 0, data);

			if(scheduleId > 0 && _schedules.TryAdd(scheduleId, new ScheduleToken(scheduleId, handler, trigger, data)))
				return scheduleId;

			return 0;
		}

		public bool Reschedule(long scheduleId, ITrigger trigger, object data = null)
		{
			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));

			if(trigger.IsExpired())
				return false;

			if(!_schedules.TryGetValue(scheduleId, out var schedule))
				return false;

			if(_dispatchables.TryGetValue(schedule.Trigger, out var older))
			{
				older.RemoveHandler(scheduleId);

				if(older.IsEmpty && _dispatchables.TryRemove(older.Trigger, out older))
				{
					if(older.HasHandlers)
						_dispatchables.TryAdd(older.Trigger, older);
				}
			}

			//将该处理器加入到指定的触发器中的调度处理集
			return this.ScheduleCore(schedule.Handler, schedule.Trigger = trigger, scheduleId, data) > 0;
		}

		public void Unschedule()
		{
			//将待触发的任务标记置空
			var token = Interlocked.Exchange(ref _token, null);

			//如果待触发的任务标记不为空，则将其取消
			if(token != null)
				token.Cancel();

			_dispatchables.Clear();
			_schedules.Clear();
		}

		public bool Unschedule(ITrigger trigger)
		{
			if(trigger == null)
				return false;

			if(_dispatchables.TryRemove(trigger, out var schedule))
			{
				schedule.ClearHandlers();
				return true;
			}

			return false;
		}

		public bool Unschedule(long scheduleId)
		{
			if(_schedules.TryRemove(scheduleId, out var schedule))
			{
				if(_dispatchables.TryGetValue(schedule.Trigger, out var older))
				{
					older.RemoveHandler(scheduleId);

					if(older.IsEmpty && _dispatchables.TryRemove(older.Trigger, out _))
					{
						if(older.HasHandlers)
							_dispatchables.TryAdd(older.Trigger, older);
					}
				}

				return true;
			}

			return false;
		}
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			//扫描调度集
			this.Scan();

			//启动失败重试队列
			_retriever.Run();
		}

		protected override void OnStop(string[] args)
		{
			//将待触发的任务标记置空
			var token = Interlocked.Exchange(ref _token, null);

			//如果待触发的任务标记不为空，则将其取消
			if(token != null)
				token.Cancel();

			//停止失败重试队列并清空所有待重试项
			_retriever.Stop(true);
		}

		protected override void OnPause()
		{
			//将待触发的任务标记置空
			var token = Interlocked.Exchange(ref _token, null);

			//如果待触发的任务标记不为空，则将其取消
			if(token != null)
				token.Cancel();

			//停止失败重试队列
			_retriever.Stop(false);
		}

		protected override void OnResume()
		{
			//扫描调度集
			this.Scan();

			//启动失败重试队列
			_retriever.Run();
		}
		#endregion

		#region 虚拟方法
		protected virtual IHandlerContext CreateContext(ITrigger trigger, HandlerToken token, string eventId, DateTime timestamp, int index)
		{
			return new HandlerContext(this, token.ScheduleId, trigger, eventId, timestamp, index, token.Data);
		}

		protected virtual void OnRetrieverChanged(IRetriever newRetriever, IRetriever oldRetriever)
		{
		}
		#endregion

		#region 扫描方法
		/// <summary>
		/// 重新扫描排程集，并规划最新的调度任务。
		/// </summary>
		/// <remarks>
		///		<para>对调用者的建议：该方法只应在异步启动中调用。</para>
		/// </remarks>
		protected void Scan()
		{
			//如果排程集为空则退出扫描
			if(_dispatchables.IsEmpty)
				return;

			DateTime? earliest = null;
			var schedules = new List<Dispatchable>();
			IList<ITrigger> removables = null;

			//循环遍历排程集，找出其中最早的触发时间点
			foreach(var schedule in _dispatchables.Values)
			{
				//如果当前排程项的处理器集空了，则忽略它
				if(schedule.IsEmpty)
					continue;

				//获取当前排程项的下次触发时间
				var timestamp = schedule.Trigger.GetNextOccurrence();

				//如果下次触发时间为空则表示该触发器已过期
				if(timestamp == null)
				{
					if(removables == null)
						removables = new List<ITrigger>();

					//将过期的触发器加入到待移除队列中
					removables.Add(schedule.Trigger);

					//跳过当前排程项
					continue;
				}

				if(earliest == null || timestamp.Value <= earliest)
				{
					//如果下次触发时间比之前找到的最早项还早，则将之前的排程列表清空
					if(timestamp.Value < earliest)
						schedules.Clear();

					//更新当前最早触发时间点
					earliest = timestamp.Value;

					//将找到的最早排程项加入到列表中
					schedules.Add(schedule);
				}
			}

			//如果待移除队列不为空，则尝试将待移除的排程删除
			if(removables != null)
			{
				foreach(var removable in removables)
				{
					if(_dispatchables.TryRemove(removable, out var schedule))
						this.OnExpired(schedule.Trigger, schedule.Handlers);
				}
			}

			//如果找到最早的触发时间，则将找到的排程项列表加入到调度进程中
			if(earliest.HasValue)
				this.Fire(earliest.Value, schedules);
		}
		#endregion

		#region 激发事件
		protected virtual void OnExpired(ITrigger trigger, HandlerToken[] handlers)
		{
			this.Expired?.Invoke(this, new ExpiredEventArgs(trigger, handlers));
		}

		protected virtual void OnHandled(IHandler handler, IHandlerContext context, Exception exception)
		{
			this.Handled?.Invoke(this, new HandledEventArgs(handler, context, exception));
		}

		protected virtual void OnOccurred(string eventId, int count)
		{
			this.Occurred?.Invoke(this, new OccurredEventArgs(eventId, count));
		}

		protected virtual void OnOccurring(string eventId)
		{
			this.Occurring?.Invoke(this, new OccurringEventArgs(eventId));
		}

		protected virtual void OnScheduled(string eventId, int count, ITrigger[] triggers)
		{
			this.Scheduled?.Invoke(this, new ScheduledEventArgs(eventId, count, triggers));
		}
		#endregion

		#region 私有方法
		private void Refire(Dispatchable schedule)
		{
			//获取当前的任务标记
			var token = _token;

			//如果当前任务标记为空（表示还没有启动排程）或任务标记已经被取消过（表示任务处于暂停或停止状态）
			if(token == null || token.IsCancellationRequested)
				return;

			//获取下次触发的时间点
			var timestamp = schedule.Trigger.GetNextOccurrence();

			//如果下次触发时间不为空（即需要触发）
			if(timestamp.HasValue)
			{
				if(timestamp < token.Timestamp)       //如果新得到的触发时间小于待触发的时间，则尝试调度新的时间点
					this.Fire(timestamp.Value, new[] { schedule });
				else if(timestamp == token.Timestamp) //如果新得到的触发时间等于待触发的时间，则尝试将其加入到待触发任务中
				{
					token.Append(schedule, (id, count, triggers) =>
					{
						//激发“Scheduled”事件
						this.OnScheduled(id, count, triggers);
					});
				}
			}
		}

		private void Fire(DateTime timestamp, IEnumerable<Dispatchable> schedules)
		{
			if(schedules == null)
				return;

			//首先获取待处理的任务凭证
			var pendding = _token;

			//如果待处理的任务凭证有效并且指定要重新触发的时间大于或等于待触发时间则忽略当次调度
			if(pendding != null && timestamp >= pendding.Timestamp)
				return;

			//创建一个新的任务凭证
			var current = new TaskToken(timestamp, schedules);

			//循环确保本次替换的任务凭证没有覆盖到其他线程乱入的
			while(pendding == null || timestamp < pendding.Timestamp)
			{
				//将新的触发凭证设置到全局变量，并确保该设置不会覆盖其他线程的乱入
				var last = Interlocked.CompareExchange(ref _token, current, pendding);

				//如果设置成功则退出该循环
				if(last == pendding)
					break;
				else //设置失败：表示中间有其他线程的乱入，则将乱入的最新值设置为比对的凭证
					pendding = last;
			}

			//注意：再次确认待处理的任务凭证有效并且指定要重新触发的时间大于或等于待触发时间则忽略当次调度
			if(pendding != null && timestamp >= pendding.Timestamp)
			{
				//将刚创建的任务标记销毁
				current.Dispose();

				//退出
				return;
			}

			//如果原有任务凭证不为空，则将原有任务取消掉
			if(pendding != null)
				pendding.Cancel();

			//获取延迟的时长
			var duration = Utility.GetDuration(timestamp);

			Task.Delay(duration).ContinueWith((task, state) =>
			{
				//获取当前的任务调度凭证
				var token = (TaskToken)state;

				//注意：防坑处理！！！
				//任务线程可能没有延迟足够的时长就提前进入，所以必须防止这种提前进入导致的触发器的触发时间计算错误
				if(Utility.Now() < token.Timestamp)
					SpinWait.SpinUntil(() => token.IsCancellationRequested || DateTime.Now.Ticks >= token.Timestamp.Ticks);

				//如果任务已经被取消，则退出
				if(token.IsCancellationRequested)
					return;

				//将最近触发时间点设为此时此刻
				_lastTime = token.Timestamp;

				//注意：必须将待处理任务标记置空（否则会误导Scan方法重新进入Fire方法内的有效性判断）
				_token = null;

				//启动新一轮的调度扫描
				this.Scan();

				//设置处理次数
				int count = 0;

				//激发“Occurring”事件
				this.OnOccurring(token.EventId);

				//遍历待执行的调度项集合（该集合内部确保了线程安全）
				foreach(var schedule in token.Schedules)
				{
					//遍历当前调度项内的所有处理器集合（该集合内部确保了线程安全）
					foreach(var handlerToken in schedule.Handlers)
					{
						//创建处理上下文对象
						var context = this.CreateContext(schedule.Trigger, handlerToken, token.EventId, token.Timestamp, count++);

						Task.Run(() => this.Handle(handlerToken.Handler, context))//异步调用处理器进行处理（该方法内会屏蔽异常，并对执行异常的处理器进行重发处理）
						.ContinueWith(t => this.OnHandled(handlerToken.Handler, context, t.Result)); //异步调用处理器完成后，再激发“Handled”事件
					}
				}

				//激发“Occurred”事件
				this.OnOccurred(token.EventId, count);
			}, current, current.GetToken());

			try
			{
				//激发“Scheduled”事件
				this.OnScheduled(current.EventId, schedules.Sum(p => p.Count), schedules.Select(p => p.Trigger).ToArray());
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.Error(ex);
			}
		}

		private Exception Handle(IHandler handler, IHandlerContext context)
		{
			try
			{
				//调用处理器进行处理
				handler.Handle(context);

				//返回调用成功
				return null;
			}
			catch(Exception ex)
			{
				//将失败的处理器加入到重试队列中
				_retriever.Retry(handler, context, ex);

				//返回调用失败
				return ex;
			}
		}

		private long ScheduleCore(IHandler handler, ITrigger trigger, long scheduleId, object data)
		{
			long Increase(long id) => id > 0 ? id : Interlocked.Increment(ref _sequence);

			//获取指定触发器关联的执行处理器集合
			var dispatchable = _dispatchables.GetOrAdd(trigger,
				(key, state) => new Dispatchable(key, handler, state.thunk(state.id), state.data),
				new { id = scheduleId, thunk = new Func<long, long>(Increase), data });

			if(scheduleId > 0)
				this.Refire(dispatchable);

			//返回调度项编号
			return scheduleId;
		}
		#endregion

		#region 嵌套子类
		private class TaskToken : IDisposable
		{
			#region 公共字段
			public readonly string EventId;
			public readonly DateTime Timestamp;
			#endregion

			#region 私有变量
			private CancellationTokenSource _cancellation;
			private readonly ISet<Dispatchable> _schedules;
			#endregion

			#region 构造函数
			public TaskToken(DateTime timestamp, IEnumerable<Dispatchable> schedules)
			{
				this.Timestamp = timestamp;
				this.EventId = Common.Randomizer.GenerateString();
				_schedules = new HashSet<Dispatchable>(schedules);
				_cancellation = new CancellationTokenSource();
			}
			#endregion

			#region 公共属性
			public bool IsCancellationRequested
			{
				get
				{
					var cancellation = _cancellation;
					return cancellation == null || cancellation.IsCancellationRequested;
				}
			}

			public IEnumerable<Dispatchable> Schedules
			{
				get
				{
					var schedules = _schedules;

					if(schedules == null)
						yield break;

					lock(schedules)
					{
						foreach(var schedule in schedules)
						{
							yield return schedule;
						}
					}
				}
			}
			#endregion

			#region 公共方法
			public bool Append(Dispatchable token, Action<string, int, ITrigger[]> succeed)
			{
				var schedules = _schedules;

				if(schedules == null)
					return false;

				var result = false;
				var count = 0;
				ITrigger[] triggers = null;

				lock(schedules)
				{
					//将指定的调度项加入
					result = schedules.Add(token);

					//如果追加成功，则必须在同步临界区内进行统计
					if(result && succeed != null)
					{
						//计算调度任务中的处理器总数
						count = schedules.Sum(p => p.Count);

						//获取调度任务中的触发器集合
						triggers = schedules.Select(p => p.Trigger).ToArray();
					}
				}

				//如果增加成功并且回调方法不为空，则回调成功方法
				if(result && succeed != null)
					succeed(this.EventId, count, triggers);

				return result;
			}

			public CancellationToken GetToken()
			{
				return _cancellation.Token;
			}

			public void Cancel()
			{
				var cancellation = _cancellation;

				if(cancellation != null)
					cancellation.Cancel();
			}

			public void Dispose()
			{
				var cancellation = Interlocked.Exchange(ref _cancellation, null);

				if(cancellation != null)
				{
					_cancellation.Cancel();
					_cancellation.Dispose();
				}
			}
			#endregion
		}

		private class ScheduleToken
		{
			#region 构造函数
			public ScheduleToken(long scheduleId, IHandler handler, ITrigger trigger, object data)
			{
				this.ScheduleId = scheduleId;
				this.Handler = handler;
				this.Trigger = trigger;
				this.Data = data;
			}
			#endregion

			#region 公共属性
			public long ScheduleId { get; }
			public object Data { get; }
			public IHandler Handler { get; }
			public ITrigger Trigger { get; internal set; }
			#endregion
		}

		private struct Dispatchable : IEquatable<Dispatchable>, IEquatable<ITrigger>
		{
			#region 公共字段
			public readonly ITrigger Trigger;
			#endregion

			#region 私有变量
			private readonly ConcurrentDictionary<long, HandlerToken> _tokens;
			#endregion

			#region 构造函数
			public Dispatchable(ITrigger trigger, IHandler handler, long scheduleId, object data)
			{
				this.Trigger = trigger;
				_tokens = new ConcurrentDictionary<long, HandlerToken>();
				_tokens.TryAdd(scheduleId, new HandlerToken(scheduleId, handler, data));
			}
			#endregion

			#region 公共属性
			public int Count { get => _tokens.Count; }
			public bool HasHandlers { get => _tokens.Count > 0; }
			public bool IsEmpty { get => _tokens.IsEmpty; }
			public HandlerToken[] Handlers { get => _tokens.Values.ToArray(); }
			#endregion

			#region 公共方法
			public bool RemoveHandler(long scheduleId)
			{
				if(scheduleId == 0)
					return false;

				return _tokens.TryRemove(scheduleId, out _);
			}

			public void ClearHandlers()
			{
				_tokens.Clear();
			}
			#endregion

			#region 重写方法
			public bool Equals(ITrigger trigger)
			{
				return this.Trigger.Equals(trigger);
			}

			public bool Equals(Dispatchable other)
			{
				return this.Trigger.Equals(other.Trigger);
			}

			public override bool Equals(object other)
			{
				if(other == null)
					return false;

				return other switch
				{
					ITrigger trigger => this.Trigger.Equals(trigger),
					Dispatchable schedule => this.Trigger.Equals(schedule.Trigger),
					_ => false,
				};
			}

			public override int GetHashCode()
			{
				return this.Trigger.GetHashCode();
			}

			public override string ToString()
			{
				return this.Trigger.ToString() + " (" + _tokens.Count.ToString() + ")";
			}
			#endregion
		}
		#endregion
	}
}
