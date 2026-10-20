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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Caching;

/// <summary>缓冲高频输入，并按周期、数量或显式请求串行执行批量处理。</summary>
/// <typeparam name="T">缓冲的数据类型。</typeparam>
/// <remarks>
///		<para>回调通过枚举取走数据；取走不表示持久化成功。缓冲器不重试、不去重、不写日志，释放不排空数据。</para>
///		<para>调用方应避免在回调及故障通知中等待本缓冲器的刷新、异步释放或满载投递，以免形成循环等待。</para>
///		<para>在高频写数据的场景中（譬如日志记录、物联网的数据采集等），可使用本缓冲器来降低写入频率。该类底层采用 <seealso cref="System.Threading.Channels"/> 技术进行异步生成与消费高频数据，具有优秀的高并发性能。</para>
/// </remarks>
public partial class Spooler<T> : IEnumerable<T>, IAsyncDisposable
{
	#region 事件声明
	/// <summary>定时刷新运行失败时发生；该次定时运行结束，仍可显式刷新或由满载写入触发刷新。</summary>
	/// <remarks>修改事件参数不改变运行结果。订阅者异常中断后续通知，并由异步释放传播。</remarks>
	public event EventHandler<Diagnostics.FailureEventArgs> Failed;
	#endregion

	#region 成员字段
	private readonly int _limit;
	private readonly Common.Timer _timer;
	private readonly SemaphoreSlim _flushing; //串行化刷新，关闭时等待持有者退出
	private readonly IDisposable _diagnostics;
	private readonly TaskCompletionSource _disposal;

	private Channel<T> _channel; //置空表示已经开始关闭
	private Func<IEnumerable<T>, CancellationToken, ValueTask> _flusher;
	#endregion

	#region 构造函数
	/// <summary>使用指定回调初始化缓冲器，并启动周期刷新。</summary>
	/// <param name="flusher">通过枚举取走数据的处理回调。</param>
	/// <param name="period">周期刷新间隔。</param>
	/// <param name="limit">缓冲及每个枚举器的数量上限；小于或等于零表示不限数量。</param>
	/// <param name="clock">周期刷新使用的时钟；为空时使用系统时钟。</param>
	public Spooler(Func<IEnumerable<T>, CancellationToken, ValueTask> flusher, TimeSpan period, int limit = 0, TimeProvider clock = null) :
		this(flusher == null ? throw new ArgumentNullException(nameof(flusher)) : period, limit, clock) => _flusher = flusher;

	/// <summary>为派生类初始化缓冲器，并启动周期刷新。</summary>
	/// <param name="period">周期刷新间隔。</param>
	/// <param name="limit">缓冲及每个枚举器的数量上限；小于或等于零表示不限数量。</param>
	/// <param name="clock">周期刷新使用的时钟；为空时使用系统时钟。</param>
	protected Spooler(TimeSpan period, int limit = 0, TimeProvider clock = null)
	{
		_limit = Math.Max(limit, 0);
		_flushing = new(1, 1);
		_disposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_channel = _limit > 0 ? Channel.CreateBounded<T>(_limit) : Channel.CreateUnbounded<T>();

		_timer = new Common.Timer(period, this.OnTickAsync, clock: clock);
		_timer.Failed += this.OnFailed;
		_diagnostics = Spooler<object>.Metrics.Register(_channel.Reader);

		try { _timer.Start(); }
		catch
		{
			_diagnostics.Dispose();
			throw;
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取尚未从缓冲区取出的数据数量，不包含已经取出但仍在处理的数据。</summary>
	public int Count => this.GetChannel().Reader.Count;
	/// <summary>获取缓冲区是否为空；为空不表示处理回调已经完成。</summary>
	public bool IsEmpty => this.Count == 0;
	/// <summary>获取缓冲容量及单次枚举的数量上限；零表示无数量限制。缓冲区满后的下一次写入触发刷新。</summary>
	public int Limit => _limit;
	/// <summary>获取或设置周期刷新间隔；该间隔不保证单条数据的最大处理延迟。</summary>
	public TimeSpan Period
	{
		get
		{
			ObjectDisposedException.ThrowIf(Volatile.Read(ref _channel) == null, this);
			return _timer.Period;
		}
		set
		{
			ObjectDisposedException.ThrowIf(Volatile.Read(ref _channel) == null, this);
			_timer.Period = value;
		}
	}
	#endregion

	#region 公共方法
	/// <summary>丢弃尚未取出的数据，不影响正在执行的处理回调。</summary>
	public void Clear()
	{
		var reader = this.GetChannel().Reader;
		while(reader.TryRead(out _));
	}

	/// <summary>接收数据；缓冲区满时由本次操作刷新已有数据，再尝试接收。</summary>
	/// <param name="value">待接收的数据。</param>
	/// <param name="cancellation">取消尚未完成的投递及其触发的刷新；已接收的数据不撤回。</param>
	/// <returns>表示数据已经接收的操作，不表示该条数据已经处理。</returns>
	public ValueTask PutAsync(T value, CancellationToken cancellation = default)
	{
		var channel = Volatile.Read(ref _channel);

		if(channel == null)
			return ValueTask.FromException(new ObjectDisposedException(this.GetType().FullName));
		if(cancellation.IsCancellationRequested)
			return ValueTask.FromCanceled(cancellation);

		if(!channel.Writer.TryWrite(value))
			return this.PutCoreAsync(value, cancellation);

		Spooler<object>.Metrics.Accept();
		return ValueTask.CompletedTask;
	}

	[System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder))]
	private async ValueTask PutCoreAsync(T value, CancellationToken cancellation)
	{
		var started = Spooler<object>.Metrics.StartWaiting();

		try
		{
			while(true)
			{
				cancellation.ThrowIfCancellationRequested();
				var consumed = await this.FlushCoreAsync(cancellation, true).ConfigureAwait(false);

				cancellation.ThrowIfCancellationRequested();
				var channel = this.GetChannel();

				if(channel.Writer.TryWrite(value))
				{
					Spooler<object>.Metrics.Accept();
					return;
				}

				//回调未消费时等待容量；完成通道返回 false 时必须退出，不能空转。
				if(!consumed && !await channel.Writer.WaitToWriteAsync(cancellation).ConfigureAwait(false))
					throw new ObjectDisposedException(this.GetType().FullName);
			}
		}
		finally { Spooler<object>.Metrics.StopWaiting(started); }
	}

	/// <summary>等待正在执行的刷新，然后对仍在缓冲区的数据执行一轮刷新。</summary>
	/// <param name="cancellation">取消本次等待及由本次操作执行的回调，不取消其它操作的回调。</param>
	/// <returns>表示本轮回调已经完成的操作；部分枚举可以留下数据，持续输入可能被本轮枚举取到。</returns>
	public async ValueTask FlushAsync(CancellationToken cancellation = default) => await this.FlushCoreAsync(cancellation).ConfigureAwait(false);

	[System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
	private async ValueTask<bool> FlushCoreAsync(CancellationToken cancellation, bool full = false, bool periodic = false)
	{
		if(this.GetChannel(periodic) == null)
			return true;

		await _flushing.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			cancellation.ThrowIfCancellationRequested();
			var channel = this.GetChannel(periodic);
			if(channel == null)
				return true;

			var reader = channel.Reader;
			var count = reader.Count;

			//先取得刷新权再判断空队列，确保不会越过正在处理的回调。
			if(count == 0 || (full && count < _limit))
				return true;

			var trigger = (periodic, full) switch
			{
				(true, _) => "periodic",
				(_, true) => "capacity",
				_ => "explicit",
			};

			return await InvokeAsync(this, reader, trigger, cancellation).ConfigureAwait(false);
		}
		finally { _flushing.Release(); }

		[System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<>))]
		static async ValueTask<bool> InvokeAsync(Spooler<T> owner, ChannelReader<T> reader, string trigger, CancellationToken cancellation)
		{
			var batch = new Iterable(reader, owner._limit);
			var started = Spooler<object>.Metrics.StartFlushing();
			var outcome = "success";

			try
			{
				await owner.OnFlushAsync(batch, cancellation).ConfigureAwait(false);
				return batch.Consumed;
			}
			catch(OperationCanceledException exception) when(cancellation.IsCancellationRequested && exception.CancellationToken == cancellation)
			{
				outcome = "canceled";
				throw;
			}
			catch
			{
				outcome = "error";
				Spooler<object>.Metrics.Fail(trigger);
				throw;
			}
			finally { Spooler<object>.Metrics.StopFlushing(started, trigger, outcome); }
		}
	}
	#endregion

	#region 虚拟方法
	/// <summary>处理缓冲数据；枚举时取走数据，每个枚举器最多取出 Limit 项，零表示不限数量。</summary>
	/// <param name="items">消费性序列；重复枚举继续取数，不重放已经取出的数据。</param>
	/// <param name="cancellation">触发本轮刷新的操作令牌。</param>
	/// <returns>表示本轮处理完成的操作；异常不会自动重试已经取出的数据。</returns>
	protected virtual ValueTask OnFlushAsync(IEnumerable<T> items, CancellationToken cancellation) => _flusher?.Invoke(items, cancellation) ?? ValueTask.CompletedTask;
	#endregion

	#region 时钟方法
	private async ValueTask OnTickAsync(object state, CancellationToken cancellation)
	{
		//仅在进入业务回调之前识别正常关闭，回调抛出的异常必须保留。
		await this.FlushCoreAsync(cancellation, periodic: true).ConfigureAwait(false);
	}

	private void OnFailed(object sender, Diagnostics.FailureEventArgs args) => this.Failed?.Invoke(this, args);
	#endregion

	#region 私有方法
	private Channel<T> GetChannel(bool closing = false)
	{
		var channel = Volatile.Read(ref _channel);
		return channel == null && !closing ? throw new ObjectDisposedException(this.GetType().FullName) : channel;
	}
	#endregion

	#region 处置方法
	/// <summary>停止接收并等待在途刷新、计时器清理和故障通知结束；不排空缓冲数据。</summary>
	/// <returns>表示异步释放完成的操作；周期运行或故障通知的异常由该操作传播。</returns>
	/// <remarks>显式刷新异常由原操作传播；本方法只等待其退出。回调和故障通知应由所有者在回调外等待释放。</remarks>
	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsyncCore().ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}

	/// <summary>停止接收并等待缓冲器的在途工作结束；派生类可在等待基类实现后异步释放回调依赖的资源。</summary>
	/// <returns>表示在途工作及计时器清理结束的操作；重复调用共享相同的完成结果。</returns>
	protected virtual ValueTask DisposeAsyncCore()
	{
		var channel = Interlocked.Exchange(ref _channel, null);
		if(channel == null)
			return new(_disposal.Task);

		channel.Writer.TryComplete();
		_timer.Stop();
		_diagnostics.Dispose();

		_ = FinishAsync();
		return new(_disposal.Task);

		async Task FinishAsync()
		{
			try
			{
				await _flushing.WaitAsync().ConfigureAwait(false);
				_flusher = null;
				_flushing.Release(); //保留纯托管信号量，让关闭前已排队的操作依次检查关闭状态

				await _timer.DisposeAsync().ConfigureAwait(false);
				_disposal.TrySetResult();
			}
			catch(Exception exception) { _disposal.TrySetException(exception); }
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<T> GetEnumerator() => new Iterator(Volatile.Read(ref _channel)?.Reader, _limit);
	#endregion
}
