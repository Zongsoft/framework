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
	/// 提供消息队列轮询功能的类。
	/// </summary>
	public abstract class MessageQueuePollerBase<TQueue> : IMessageQueuePoller where TQueue : class, IMessageQueue
	{
		#region 私有变量
		private TQueue _queue;
		private CancellationTokenSource _cancellation;
		#endregion

		#region 构造函数
		/// <summary>构建消息队列轮询器。</summary>
		/// <param name="queue">待轮询的队列。</param>
		protected MessageQueuePollerBase(TQueue queue)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
		}
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public bool IsPolling
		{
			get
			{
				var cancellation = _cancellation;
				return cancellation != null && !cancellation.IsCancellationRequested;
			}
		}

		IMessageQueue IMessageQueuePoller.Queue { get => _queue; }

		/// <summary>获取或设置轮询的队列。</summary>
		[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
		public TQueue Queue
		{
			get => _queue;
			set => _queue = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		public void Start() => this.Start(null, 1000);

		/// <summary>开始队列轮询。</summary>
		/// <param name="options">轮询的出队选项。</param>
		/// <param name="interval">轮询失败的等待间隔（单位：毫秒）。</param>
		public void Start(MessageDequeueOptions options, int interval = 1000)
		{
			if(_queue == null)
				throw new ObjectDisposedException(this.GetType().Name);

			if(this.IsPolling)
				return;

			_cancellation = new CancellationTokenSource();
			Task.Factory.StartNew(this.Poll, new PollArgument(options, interval), _cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		/// <summary>停止队列轮询。</summary>
		public void Stop()
		{
			var cancellation = Interlocked.Exchange(ref _cancellation, null);

			if(cancellation != null && !cancellation.IsCancellationRequested)
				cancellation.Cancel(false);
		}
		#endregion

		#region 轮询方法
		private void Poll(object argument)
		{
			var cancellation = _cancellation;
			var settings = (PollArgument)argument;

			while(!cancellation.IsCancellationRequested)
			{
				Message message;
				Exception exception = null;

				try
				{
					//以同步方式从消息队列中获取一条消息
					message = this.Receive(settings.Options, cancellation.Token);
				}
				catch(Exception ex)
				{
					message = default;
					exception = ex;

					//错误日志
					Zongsoft.Diagnostics.Logger.Error(ex);
				}

				//如果消息获取失败则休息一小会
				if(exception != null || message.IsEmpty)
					Thread.Sleep(settings.Interval);
				else
					Handle(message, _cancellation.Token);
			}

			void Handle(in Message message, CancellationToken cancellation)
			{
				if(cancellation.IsCancellationRequested)
					return;

				try
				{
					Task.Factory.StartNew(async argument =>
					{
						var message = (Message)argument;
						await this.OnHandleAsync(message, cancellation);
					}, message, cancellation);
				}
				catch(Exception ex)
				{
					Zongsoft.Diagnostics.Logger.Error(ex, message);
				}
			}
		}

		protected abstract Message Receive(MessageDequeueOptions options, CancellationToken cancellation);
		#endregion

		#region 处理方法
		protected abstract ValueTask OnHandleAsync(Message message, CancellationToken cancellation);
		#endregion

		#region 释放资源
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var cancellation = Interlocked.Exchange(ref _cancellation, null);

			if(cancellation != null && !cancellation.IsCancellationRequested)
				cancellation.Cancel(false);

			var queue = Interlocked.Exchange(ref _queue, null);

			if(queue != null && queue is IDisposable disposable)
				disposable.Dispose();
		}
		#endregion

		#region 轮询参数
		private class PollArgument
		{
			public PollArgument(MessageDequeueOptions options, int interval = 1000)
			{
				this.Options = options ?? MessageDequeueOptions.Default;
				this.Interval = Math.Max(interval, 100);
			}

			public readonly MessageDequeueOptions Options;
			public readonly int Interval;
		}
		#endregion
	}
}
