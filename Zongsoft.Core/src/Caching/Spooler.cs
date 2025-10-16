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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Channels;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Caching;

/// <summary>提供数据缓冲功能的类。</summary>
/// <typeparam name="T">指定的缓冲数据类型。</typeparam>
/// <remarks>
/// 	<para>在高频写数据的场景中（譬如日志记录、物联网的数据采集等），可使用本缓冲器来降低写入频率。该类底层采用 <seealso cref="System.Threading.Channels"/> 技术进行异步生成与消费高频数据，具有优秀的高并发性能。</para>
/// </remarks>
public class Spooler<T> : IEnumerable<T>, IDisposable
{
	#region 成员字段
	private readonly int _limit;
	#endregion

	#region 私有变量
	private Common.Timer _timer;
	private Channel<T> _channel;
	private Func<IEnumerable<T>, CancellationToken, ValueTask> _flusher;
	#endregion

	#region 构造函数
	public Spooler(Func<IEnumerable<T>, CancellationToken, ValueTask> flusher, TimeSpan period, int limit = 0) : this(period, limit) =>
		_flusher = flusher ?? throw new ArgumentNullException(nameof(flusher));
	protected Spooler(TimeSpan period, int limit = 0)
	{
		_limit = Math.Max(limit, 0);
		_timer = new Common.Timer(period, this.OnTickAsync);
		_channel = _limit > 0 ? Channel.CreateBounded<T>(_limit) : Channel.CreateUnbounded<T>();
		_timer.Start();
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前缓冲区的数量。</summary>
	public int Count => this.GetChannel(out var channel) && channel.Reader.CanCount ? channel.Reader.Count : -1;

	/// <summary>获取一个值，指示当前缓冲区是否空了。</summary>
	public bool IsEmpty => this.GetChannel(out var channel) && channel.Reader.CanCount && channel.Reader.Count == 0;

	/// <summary>获取缓冲数量限制，如果缓冲数量超过该属性值则立即触发刷新回调；如果为零则表示忽略该限制。</summary>
	public int Limit => _limit;

	#if NET8_0_OR_GREATER
	/// <summary>获取或设置缓冲的刷新周期，不能低于<c>1</c>毫秒(Millisecond)。</summary>
	public TimeSpan Period
	{
		get => _timer != null ? _timer.Period : throw new ObjectDisposedException(nameof(Spooler<T>));
		set
		{
			var timer = _timer ?? throw new ObjectDisposedException(nameof(Spooler<T>));

			if(timer.Period == value)
				return;

			timer.Period = value;
		}
	}
	#endif
	#endregion

	#region 公共方法
	public void Clear()
	{
		while(this.GetChannel(out var channel) && channel.Reader.TryRead(out _));
	}

	public async ValueTask PutAsync(T value, CancellationToken cancellation = default)
	{
		if(this.GetChannel(out var channel) && channel.Writer.TryWrite(value))
			return;

		await this.OnFlushAsync(new Enumerable(channel.Reader), cancellation);
		await channel.Writer.WaitToWriteAsync(cancellation);
		await channel.Writer.WriteAsync(value, cancellation);
	}

	public async ValueTask FlushAsync(CancellationToken cancellation = default)
	{
		if(this.IsEmpty)
			return;

		var channel = this.GetChannel();
		await channel.Reader.WaitToReadAsync(cancellation);
		await this.OnFlushAsync(new Enumerable(channel.Reader), cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual ValueTask OnFlushAsync(IEnumerable<T> items, CancellationToken cancellation)
	{
		var flusher = _flusher;
		return flusher != null ? flusher.Invoke(items, cancellation) : ValueTask.CompletedTask;
	}
	#endregion

	#region 时钟方法
	private ValueTask OnTickAsync(object state, CancellationToken cancellation) => this.FlushAsync(cancellation);
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private Channel<T> GetChannel() => _channel ?? throw new ObjectDisposedException(nameof(Spooler<T>));
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private bool GetChannel(out Channel<T> channel) => (channel = _channel ?? throw new ObjectDisposedException(nameof(Spooler<T>))) != null;
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		_flusher = null;
		var timer = Interlocked.Exchange(ref _timer, null);
		var channel = Interlocked.Exchange(ref _channel, null);

		if(disposing)
		{
			timer?.Dispose();
			channel?.Writer.TryComplete();
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<T> GetEnumerator() => new Enumerable.Iterator(_channel?.Reader);
	#endregion

	#region 嵌套子类
	private sealed class Enumerable(ChannelReader<T> reader) : IEnumerable<T>
	{
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() => new Iterator(reader);

		internal sealed class Iterator(ChannelReader<T> reader) : IEnumerator<T>
		{
			private T _value;
			private ChannelReader<T> _reader = reader;

			public T Current => _value;
			object IEnumerator.Current => _value;

			public void Dispose() => _reader = null;
			public bool MoveNext() => _reader != null && _reader.TryRead(out _value);
			public void Reset() { }
		}
	}
	#endregion
}
