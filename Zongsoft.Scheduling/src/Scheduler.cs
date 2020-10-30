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
using System.Collections;
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
		private DateTime? _lastTime;
		private TaskToken _token;
		private ConcurrentDictionary<ITrigger, ScheduleToken> _schedules;
		private HashSet<IHandler> _handlers;
		private IDictionary<string, object> _states;
		private IRetriever _retriever;
		#endregion

		#region 构造函数
		public Scheduler()
		{
			this.CanPauseAndContinue = true;

			_handlers = new HashSet<IHandler>();
			_schedules = new ConcurrentDictionary<ITrigger, ScheduleToken>(TriggerComparer.Instance);
			_retriever = new Retriever();
		}
		#endregion

		#region 公共属性
		public DateTime? NextTime
		{
			get => _token?.Timestamp;
		}

		public DateTime? LastTime
		{
			get => _lastTime;
		}

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
			get => _schedules.Keys as IReadOnlyCollection<ITrigger> ?? _schedules.Keys.ToArray();
		}

		public IReadOnlyCollection<IHandler> Handlers
		{
			get => _handlers;
		}

		public bool IsScheduling
		{
			get
			{
				var token = _token;
				return token != null && !token.IsCancellationRequested;
			}
		}

		public bool HasStates
		{
			get => _states != null && _states.Count > 0;
		}

		public IDictionary<string, object> States
		{
			get
			{
				if(_states == null)
					Interlocked.CompareExchange(ref _states, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _states;
			}
		}
		#endregion

		#region 公共方法
		public IEnumerable<IHandler> GetHandlers(ITrigger trigger)
		{
			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));

			if(_schedules.TryGetValue(trigger, out var schedule))
				return schedule.Handlers;
			else
				return System.Linq.Enumerable.Empty<IHandler>();
		}

		public bool Schedule(IHandler handler, ITrigger trigger)
		{
			return this.Schedule(handler, trigger, null);
		}

		/// <summary>
		/// 排程操作，将指定的处理器与触发器绑定。
		/// </summary>
		/// <param name="handler">指定要绑定的处理器。</param>
		/// <param name="trigger">指定要调度的触发器。</param>
		/// <param name="onTrigger">当触发器触发后的回调方法，暂不支持。</param>
		/// <returns>如果排程成功则返回真(True)，否则返回假(False)。</returns>
		/// <remarks>同一个处理器不能多次绑定到同一个触发器。</remarks>
		public bool Schedule(IHandler handler, ITrigger trigger, Action<IHandlerContext> onTrigger)
		{
			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));

			if(trigger.IsExpired())
				return false;

			if(onTrigger != null) //TODO: 暂时不支持该功能
				throw new NotSupportedException();

			//将处理器增加到处理器集中，如果添加成功（说明该处理器没有被调度过）
			if(_handlers.Add(handler))
			{
				//将该处理器加入到指定的触发器中的调度处理集
				return this.ScheduleCore(handler, trigger);
			}

			return false;
		}

		public bool Reschedule(IHandler handler, ITrigger trigger)
		{
			if(handler == null)
				throw new ArgumentNullException(nameof(handler));
			if(trigger == null)
				throw new ArgumentNullException(nameof(trigger));

			if(trigger.IsExpired())
				return false;

			//将处理器增加到处理器集中，如果添加成功（说明该处理器没有被调度过）
			if(_handlers.Add(handler))
			{
				//将该处理器加入到指定的触发器中的调度处理集
				return this.ScheduleCore(handler, trigger);
			}

			//定义找到的调度项变量
			ScheduleToken? found = null;

			//循环遍历排程集，查找重新排程的触发器
			foreach(var schedule in _schedules.Values)
			{
				//如果当前排程的触发器等于要重新排程的触发器，则更新找到引用
				if(schedule.Trigger.Equals(trigger))
				{
					found = schedule;
				}
				else //否则就尝试将待排程的处理器从原有排程项的处理集中移除掉
				{
					schedule.RemoveHandler(handler);
				}
			}

			if(found.HasValue)
			{
				//将指定的执行处理器加入到找到的调度项的执行集合中，如果加入成功则尝试重新激发
				//该新增方法确保同步完成，不会引发线程重入导致的状态不一致
				if(found.Value.AddHandler(handler))
				{
					//尝试重新触发
					this.Refire(found.Value);

					//返回重新调度成功
					return true;
				}

				//返回重新调度失败
				return false;
			}

			//将该处理器加入到指定的触发器中的调度处理集
			return this.ScheduleCore(handler, trigger);
		}

		public void Unschedule()
		{
			//将待触发的任务标记置空
			var token = Interlocked.Exchange(ref _token, null);

			//如果待触发的任务标记不为空，则将其取消
			if(token != null)
				token.Cancel();

			_handlers.Clear();
			_schedules.Clear();
		}

		public bool Unschedule(IHandler handler)
		{
			if(handler == null)
				return false;

			if(_handlers.Remove(handler))
			{
				foreach(var schedule in _schedules.Values)
				{
					schedule.RemoveHandler(handler);

					if(schedule.Count == 0)
						_schedules.TryRemove(schedule.Trigger, out _);
				}

				return true;
			}

			return false;
		}

		public bool Unschedule(ITrigger trigger)
		{
			if(trigger == null)
				return false;

			if(_schedules.TryRemove(trigger, out var schedule))
			{
				schedule.ClearHandlers();
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

			//清空处理器集
			_handlers.Clear();

			//清空调度项集
			_schedules.Clear();

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
			if(_schedules.IsEmpty)
				return;

			DateTime? earliest = null;
			var schedules = new List<ScheduleToken>();
			IList<ScheduleToken> removables = null;

			//循环遍历排程集，找出其中最早的触发时间点
			foreach(var schedule in _schedules.Values)
			{
				//如果当前排程项的处理器集空了，则忽略它
				if(schedule.Count == 0)
					continue;

				//获取当前排程项的下次触发时间
				var timestamp = schedule.Trigger.GetNextOccurrence();

				//如果下次触发时间为空则表示该触发器已过期
				if(timestamp == null)
				{
					if(removables == null)
						removables = new List<ScheduleToken>();

					//将过期的触发器加入到待移除队列中
					removables.Add(schedule);

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
					if(_schedules.TryRemove(removable.Trigger, out var schedule))
						this.OnExpired(schedule.Trigger, schedule.Handlers.ToArray());
				}
			}

			//如果找到最早的触发时间，则将找到的排程项列表加入到调度进程中
			if(earliest.HasValue)
				this.Fire(earliest.Value, schedules);
		}
		#endregion

		#region 激发事件
		protected virtual void OnExpired(ITrigger trigger, IHandler[] handlers)
		{
			this.Expired?.Invoke(this, new ExpiredEventArgs(trigger, handlers));
		}

		protected virtual void OnHandled(IHandler handler, IHandlerContext context, Exception exception)
		{
			this.Handled?.Invoke(this, new HandledEventArgs(handler, context, exception));
		}

		protected virtual void OnOccurred(string scheduleId, int count)
		{
			this.Occurred?.Invoke(this, new OccurredEventArgs(scheduleId, count));
		}

		protected virtual void OnOccurring(string scheduleId)
		{
			this.Occurring?.Invoke(this, new OccurringEventArgs(scheduleId));
		}

		protected virtual void OnScheduled(string scheduleId, int count, ITrigger[] triggers)
		{
			this.Scheduled?.Invoke(this, new ScheduledEventArgs(scheduleId, count, triggers));
		}
		#endregion

		#region 私有方法
		private void Refire(ScheduleToken schedule)
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

		private void Fire(DateTime timestamp, IEnumerable<ScheduleToken> schedules)
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
				this.OnOccurring(token.Identity);

				//遍历待执行的调度项集合（该集合内部确保了线程安全）
				foreach(var schedule in token.Schedules)
				{
					//遍历当前调度项内的所有处理器集合（该集合内部确保了线程安全）
					foreach(var handler in schedule.Handlers)
					{
						//创建处理上下文对象
						var context = new HandlerContext(this, schedule.Trigger, token.Identity, token.Timestamp, count++);

						Task.Run(() => this.Handle(handler, context))//异步调用处理器进行处理（该方法内会屏蔽异常，并对执行异常的处理器进行重发处理）
						.ContinueWith(t => this.OnHandled(handler, context, t.Result)); //异步调用处理器完成后，再激发“Handled”事件
					}
				}

				//激发“Occurred”事件
				this.OnOccurred(token.Identity, count);
			}, current, current.GetToken());

			try
			{
				//激发“Scheduled”事件
				this.OnScheduled(current.Identity, schedules.Sum(p => p.Count), schedules.Select(p => p.Trigger).ToArray());
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

		private bool ScheduleCore(IHandler handler, ITrigger trigger)
		{
			//获取指定触发器关联的执行处理器集合
			var schedule = _schedules.GetOrAdd(trigger, key => new ScheduleToken(key, new HashSet<IHandler>()));

			//将指定的执行处理器加入到对应的调度项的执行集合中，如果加入成功则尝试重新激发
			//该新增方法确保同步完成，不会引发线程重入导致的状态不一致
			if(schedule.AddHandler(handler))
			{
				//尝试重新调度
				this.Refire(schedule);

				//返回新增调度成功
				return true;
			}

			//返回默认失败
			return false;
		}
		#endregion

		#region 嵌套子类
		private class TaskToken : IDisposable
		{
			#region 公共字段
			public readonly string Identity;
			public readonly DateTime Timestamp;
			#endregion

			#region 私有变量
			private CancellationTokenSource _cancellation;
			private readonly ISet<ScheduleToken> _schedules;
			#endregion

			#region 构造函数
			public TaskToken(DateTime timestamp, IEnumerable<ScheduleToken> schedules)
			{
				this.Timestamp = timestamp;
				this.Identity = Common.Randomizer.GenerateString();
				_schedules = new HashSet<ScheduleToken>(schedules);
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

			public IEnumerable<ScheduleToken> Schedules
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
			public bool Append(ScheduleToken token, Action<string, int, ITrigger[]> succeed)
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
					succeed(this.Identity, count, triggers);

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

		private struct ScheduleToken : IEquatable<ScheduleToken>, IEquatable<ITrigger>
		{
			#region 公共字段
			public readonly ITrigger Trigger;
			#endregion

			#region 私有变量
			private readonly ISet<IHandler> _handlers;
			private readonly AutoResetEvent _semaphore;
			#endregion

			#region 构造函数
			public ScheduleToken(ITrigger trigger, ISet<IHandler> handlers)
			{
				this.Trigger = trigger;
				_handlers = handlers;
				_semaphore = new AutoResetEvent(true);
			}
			#endregion

			#region 公共属性
			/// <summary>
			/// 获取调度项包含的处理器数量。
			/// </summary>
			public int Count
			{
				get => _handlers.Count;
			}

			/// <summary>
			/// 获取调度项包含的处理器集，该集内部以独占方式提供遍历。
			/// </summary>
			public IEnumerable<IHandler> Handlers
			{
				get
				{
					try
					{
						_semaphore.WaitOne();

						foreach(var handler in _handlers)
						{
							yield return handler;
						}
					}
					finally
					{
						_semaphore.Set();
					}
				}
			}
			#endregion

			#region 公共方法
			/// <summary>
			/// 以同步方式将指定的处理器加入到当前调度项中。
			/// </summary>
			/// <param name="handler">指定要添加的同步器。</param>
			/// <returns>添加成功返回真，否则返回假（即表示指定的同步器已经存在于当前调度项中了）。</returns>
			public bool AddHandler(IHandler handler)
			{
				if(handler == null)
					return false;

				try
				{
					_semaphore.WaitOne();

					return _handlers.Add(handler);
				}
				finally
				{
					_semaphore.Set();
				}
			}

			/// <summary>
			/// 以同步方式将指定的处理器从当前调度项中移除。
			/// </summary>
			/// <param name="handler">指定要移除的同步器。</param>
			/// <returns>移除成功返回真，否则返回假（即表示指定的同步器已经不存在于当前调度项中了）。</returns>
			public bool RemoveHandler(IHandler handler)
			{
				if(handler == null)
					return false;

				try
				{
					_semaphore.WaitOne();

					return _handlers.Remove(handler);
				}
				finally
				{
					_semaphore.Set();
				}
			}

			/// <summary>
			/// 以同步方式将当前调度项中的处理器集清空。
			/// </summary>
			public void ClearHandlers()
			{
				try
				{
					_semaphore.WaitOne();

					_handlers.Clear();
				}
				finally
				{
					_semaphore.Set();
				}
			}
			#endregion

			#region 重写方法
			public bool Equals(ITrigger trigger)
			{
				return this.Trigger.Equals(trigger);
			}

			public bool Equals(ScheduleToken other)
			{
				return this.Trigger.Equals(other.Trigger);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != this.GetType())
					return false;

				return base.Equals((ScheduleToken)obj);
			}

			public override int GetHashCode()
			{
				return this.Trigger.GetHashCode();
			}

			public override string ToString()
			{
				return this.Trigger.ToString() + " (" + _handlers.Count + ")";
			}
			#endregion
		}

		private class TriggerComparer : IEqualityComparer<ITrigger>
		{
			#region 单例字段
			public static readonly TriggerComparer Instance = new TriggerComparer();
			#endregion

			#region 私有构造
			private TriggerComparer() { }
			#endregion

			#region 公共方法
			public bool Equals(ITrigger x, ITrigger y)
			{
				if(x == null || y == null)
					return false;

				return x.GetType() == y.GetType() && x.Equals(y);
			}

			public int GetHashCode(ITrigger obj)
			{
				if(obj == null)
					return 0;

				return obj.GetType().GetHashCode() ^ obj.GetHashCode();
			}
			#endregion
		}
		#endregion
	}
}
