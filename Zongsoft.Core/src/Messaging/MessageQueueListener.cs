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
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Collections;
using Zongsoft.Communication;

namespace Zongsoft.Messaging
{
	[DisplayName("${Text.MessageQueueListener.Title}")]
	[Description("${Text.MessageQueueListener.Description}")]
	public class MessageQueueListener<T> : ListenerBase<T>
	{
		#region 成员字段
		private IMessageQueue<T> _queue;
		private MessageQueueReceiver _receiver;
		#endregion

		#region 构造函数
		public MessageQueueListener(string name) : base(name) { }
		public MessageQueueListener(IMessageQueue<T> queue) : base("MessageQueueListener")
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));

			if(queue.Name != null && queue.Name.Length > 0)
				base.Name = queue.Name;
		}
		#endregion

		#region 公共属性
		[TypeConverter(typeof(QueueConverter))]
		public IMessageQueue<T> Queue
		{
			get => _queue;
			set
			{
				if(this.State == Services.WorkerState.Running)
					throw new InvalidOperationException();

				_queue = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			if(_receiver == null)
				_receiver = new MessageQueueReceiver(this);

			_receiver.Start();
		}

		protected override void OnStop(string[] args)
		{
			var receiver = Interlocked.Exchange(ref _receiver, null);

			if(receiver != null)
				receiver.Dispose();
		}
		#endregion

		#region 嵌套子类
		private class MessageQueueReceiver : IDisposable
		{
			#region 私有变量
			private readonly MessageQueueListener<T> _listener;
			private CancellationTokenSource _cancellation;
			#endregion

			#region 构造函数
			public MessageQueueReceiver(MessageQueueListener<T> listener)
			{
				_listener = listener ?? throw new ArgumentNullException(nameof(listener));
				_cancellation = new CancellationTokenSource();
			}
			#endregion

			#region 收取消息
			public void Start()
			{
				var cancellation = _cancellation;

				if(cancellation == null || cancellation.IsCancellationRequested)
					return;

				Task.Factory.StartNew(this.Receive, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			private void Receive()
			{
				var queue = _listener.Queue;
				var cancellation = _cancellation;
				var options = new MessageDequeueSettings(TimeSpan.FromSeconds(10));

				while(!cancellation.IsCancellationRequested)
				{
					T message;
					Exception exception = null;

					try
					{
						//以同步方式从消息队列中获取一条消息
						message = queue.Dequeue(options);
					}
					catch(Exception ex)
					{
						message = default;
						exception = ex;

						//错误日志
						Zongsoft.Diagnostics.Logger.Error(ex);
					}

					//如果消息获取失败则休息一小会
					if(exception != null)
						Thread.Sleep(500);
					else //以异步方式激发消息接收事件
						_listener.OnHandleAsync(message, cancellation.Token);
				}
			}
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

				if(cancellation != null)
					cancellation.Cancel();
			}
			#endregion
		}
		#endregion
	}

	internal class QueueConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string name)
			{
				var queueProvider = (IQueueProvider)Services.ApplicationContext.Current?.Services?.GetService(typeof(IQueueProvider));

				if(queueProvider != null)
					return queueProvider.GetQueue(name) as IMessageQueue;
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
