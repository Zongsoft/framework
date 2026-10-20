/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Channels;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Zongsoft.Caching;

partial class Spooler<T>
{
	//所有数据类型共用 Spooler<object>.Metrics，避免重复注册仪表并保持队列总量。
	private static class Metrics
	{
		#region 常量定义
		private const string NAME = "Zongsoft.Caching.Spooler";
		#endregion

		#region 静态字段
		private static readonly object _sync = new();
		private static readonly LinkedList<WeakReference<Registration>> _registrations = new();
		private static readonly Meter _meter = new(NAME, typeof(Spooler<>).Assembly.GetName().Version?.ToString());
		private static readonly Counter<long> _accepted = _meter.CreateCounter<long>("zongsoft.caching.spooler.items.accepted", "{item}");
		private static readonly Counter<long> _failures = _meter.CreateCounter<long>("zongsoft.caching.spooler.failures", "{failure}");
		private static readonly Histogram<double> _flushDuration = _meter.CreateHistogram<double>("zongsoft.caching.spooler.flush.duration", "s");
		private static readonly Histogram<double> _waitDuration = _meter.CreateHistogram<double>("zongsoft.caching.spooler.put.wait.duration", "s");
		#endregion

		#region 静态构造
		static Metrics() => _meter.CreateObservableGauge("zongsoft.caching.spooler.items.queued", Observe, "{item}");
		#endregion

		#region 测量方法
		public static void Accept()
		{
			if(_accepted.Enabled)
				Record();

			static void Record()
			{
				try { _accepted.Add(1); }
				catch { } //指标监听器不得改变接收结果。
			}
		}

		public static void Fail(string trigger)
		{
			try { _failures.Add(1, new KeyValuePair<string, object>("zongsoft.spooler.trigger", trigger)); }
			catch { }
		}

		public static long StartFlushing() => _flushDuration.Enabled ? Stopwatch.GetTimestamp() : 0;
		public static long StartWaiting() => _waitDuration.Enabled ? Stopwatch.GetTimestamp() : 0;

		public static void StopFlushing(long started, string trigger, string outcome)
		{
			if(started == 0)
				return;

			try
			{
				_flushDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds,
					new("zongsoft.spooler.trigger", trigger), new("zongsoft.spooler.outcome", outcome));
			}
			catch { }
		}

		public static void StopWaiting(long started)
		{
			if(started == 0)
				return;

			try { _waitDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds); }
			catch { }
		}
		#endregion

		#region 注册采样
		public static IDisposable Register<TItem>(ChannelReader<TItem> reader)
		{
			var registration = new Registration(() => reader.Count);

			lock(_sync)
				registration.Node = _registrations.AddLast(new WeakReference<Registration>(registration));

			return registration;
		}

		private static long Observe()
		{
			long count = 0;

			lock(_sync)
			{
				var node = _registrations.First;

				while(node != null)
				{
					var next = node.Next;

					if(node.Value.TryGetTarget(out var registration))
						count += registration.Count();
					else
						_registrations.Remove(node);

					node = next;
				}
			}

			return count;
		}
		#endregion

		#region 嵌套子类
		//只在创建、采集及关闭时访问注册表；热路径不维护额外队列计数，也不强引用缓冲器。
		private sealed class Registration(Func<int> count) : IDisposable
		{
			public Func<int> Count = count;
			public LinkedListNode<WeakReference<Registration>> Node;

			public void Dispose()
			{
				lock(_sync)
				{
					if(this.Node?.List != null)
						_registrations.Remove(this.Node);

					this.Node = null;
					this.Count = null;
				}
			}
		}
		#endregion
	}
}
