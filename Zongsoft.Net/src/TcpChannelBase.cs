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
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Communication;

namespace Zongsoft.Net;

/// <summary>由单一接收者和单一写入者协调的 TCP 通道。</summary>
/// <typeparam name="T">线协议报文类型。</typeparam>
/// <remarks><see cref="ChannelBase.CloseAsync" /> 关闭传输，<see cref="WaitAsync" /> 等待处理器、发送和缓冲清理。处理器内可以关闭通道，不得等待自身的完成或释放。</remarks>
public abstract partial class TcpChannelBase<T> : ChannelBase, ISender<T>, ITcpMonitoredConnection
{
	#region 成员字段
	private readonly TcpTransport _transport;
	private readonly TcpConnectionOptions _options;
	private readonly PacketizerBase<T> _packetizer;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly Sender _sender;
	private readonly Receiver _receiver;
	private Task _cancelling = Task.CompletedTask;
	private Exception _failure;
	private int _started;
	private int _closeReason;
	private int _errorReported;
	private bool _shutdownTimedOut;
	#endregion

	#region 构造函数
	private protected TcpChannelBase(TcpTransport transport, PacketizerBase<T> packetizer, TcpConnectionOptions options)
	{
		_transport = transport;
		_packetizer = packetizer;
		_options = options;
		_sender = new(this);
		_receiver = new(this);

		this.Identifier = TcpTransport.GetIdentity();
	}
	#endregion

	#region 公共属性
	/// <summary>获取稳定的连接标识，不以远端地址作为连接身份。</summary>
	public long Identifier { get; }
	/// <summary>获取本地实际端点。</summary>
	public EndPoint LocalEndPoint => _transport.LocalEndPoint;
	/// <summary>获取远端端点。</summary>
	public EndPoint RemoteEndPoint => _transport.RemoteEndPoint;
	/// <summary>获取已完成 Socket 发送的累计字节数，不表示远端业务确认。</summary>
	public long TotalBytesSent => _transport.BytesSent;
	/// <summary>获取从 Socket 接收的累计字节数。</summary>
	public long TotalBytesReceived => _transport.BytesReceived;
	/// <summary>获取成功编码并完成 Flush 的报文数。</summary>
	public long TotalFramesSent => _sender.Frames;
	/// <summary>获取成功解码的报文数。</summary>
	public long TotalFramesReceived => _receiver.Frames;
	/// <summary>获取已接纳但尚未完成的发送操作数。</summary>
	public int PendingSendCount => _sender.Count;
	/// <summary>获取预留及尚未完成 Socket 发送的线协议字节数。</summary>
	public long PendingSendBytes => _sender.Bytes;
	/// <summary>获取可选诊断计数；未启用 <see cref="TcpConnectionOptions.EnableDiagnostics" /> 时全部为零。</summary>
	public TcpChannelStatistics Statistics => new(
		_receiver.Reads,
		_receiver.BudgetExhaustions,
		_receiver.MaximumFrames,
		_sender.Waits);
	#endregion

	#region 私有属性
	/// <summary>获取连接结束的首个原因，供生命周期通知创建快照。</summary>
	private TcpCloseReason CloseReason => (TcpCloseReason)Volatile.Read(ref _closeReason);
	/// <summary>获取首个收发、处理或清理故障；尚无故障时为 <see langword="null" />。</summary>
	private Exception Failure => Volatile.Read(ref _failure);
	#endregion

	#region 等待完成
	/// <summary>等待处理器、已接纳发送、缓冲清理及生命周期通知全部结束，不主动关闭连接。</summary>
	/// <param name="cancellation">仅取消当前调用方的等待，不影响通道及其他等待者。</param>
	/// <returns>共享完成结果；通道发生故障时传播首个异常，完成后仍可再次等待。</returns>
	/// <remarks>处理器不得等待自身退场；同步事件回调不得阻塞等待包含自身的操作。</remarks>
	public Task WaitAsync(CancellationToken cancellation = default) => _completion.Task.WaitAsync(cancellation);
	#endregion

	#region 接收流程
	/// <summary>获取一个值，指示连接仍可接纳新操作。</summary>
	internal bool IsActive => _receiver.IsActive;

	/// <summary>取得唯一接收权，协调接收结束、发送排空和资源清理。</summary>
	internal void Start()
	{
		if(Interlocked.CompareExchange(ref _started, 1, 0) != 0)
			throw new InvalidOperationException();

		_ = ReceiveAsync();

		async Task ReceiveAsync()
		{
			try
			{
				if(await _receiver.RunAsync().ConfigureAwait(false))
					this.RecordFailure(TcpCloseReason.Remote, null);

				if(!this.IsClosing)
					await _sender.DrainAsync().ConfigureAwait(false);
			}
			catch(OperationCanceledException) when(_lifetime.IsCancellationRequested) { }
			catch(Exception exception) { this.RecordFailure(GetCloseReason(exception), exception); }
			finally
			{
				try { await _transport.Input.CompleteAsync(this.Failure).ConfigureAwait(false); }
				catch(Exception exception) { this.RecordFailure(TcpCloseReason.TransportError, exception); }

				await this.FinishAsync().ConfigureAwait(false);
			}
		}
	}

	/// <summary>处理当前报文；完成前不得释放或在其他任务中继续使用其借用内存。</summary>
	/// <param name="context">接收通道和报文。</param>
	/// <param name="cancellation">连接关闭令牌。</param>
	/// <returns>本次报文的业务处理操作。</returns>
	protected abstract ValueTask OnReceiveAsync(TcpReceiveContext<T> context, CancellationToken cancellation);
	#endregion

	#region 超时检查
	/// <summary>获取构造时固定的连接设置快照。</summary>
	TcpConnectionOptions ITcpMonitoredConnection.Settings => _options;

	/// <summary>由共享维护任务终止连接并保存故障。</summary>
	/// <param name="reason">连接关闭原因。</param>
	/// <param name="exception">首个故障；正常关闭时为 <see langword="null" />。</param>
	void ITcpMonitoredConnection.Abort(TcpCloseReason reason, Exception exception) => this.Abort(reason, exception);

	/// <summary>协调收发期限检查，并按首个超时原因关闭连接。</summary>
	/// <param name="now">由配置的 <see cref="TimeProvider" /> 提供的单调时间戳。</param>
	/// <returns>相对于记录期限的检测延迟；未超时返回 <see cref="TimeSpan.Zero" />。</returns>
	TimeSpan ITcpMonitoredConnection.CheckTimeout(long now)
	{
		if(this.IsClosing)
			return TimeSpan.Zero;

		var reason = _receiver.CheckTimeout(now, out var delay);
		if(reason == TcpCloseReason.None && _sender.CheckTimeout(now, out delay))
			reason = TcpCloseReason.SendTimeout;

		if(reason == TcpCloseReason.None)
			return TimeSpan.Zero;

		delay += _options.TimeProvider.GetElapsedTime(now);
		this.Abort(reason, new TimeoutException(Properties.Resources.Network_Timeout_Message));
		return delay;
	}
	#endregion

	#region 事件通知
	/// <summary>固定开始关闭的原因，并转发所属连接管理对象的通知。</summary>
	protected sealed override void OnClosing()
	{
		Interlocked.CompareExchange(ref _closeReason, (int)TcpCloseReason.Local, 0);

		try { base.OnClosing(); }
		finally
		{
			this.OnClosing(new(this, this.CloseReason, this.Failure));
			this.ReportError();
		}
	}

	/// <summary>通知所属连接管理对象开始关闭。</summary>
	/// <param name="args">开始关闭时的状态快照。</param>
	private protected abstract void OnClosing(TcpChannelEventArgs<T> args);
	/// <summary>通知所属连接管理对象已经完整退场。</summary>
	/// <param name="args">移除登记后的最终状态快照。</param>
	private protected abstract void OnClosed(TcpChannelEventArgs<T> args);
	/// <summary>通知所属连接管理对象首个结束故障。</summary>
	/// <param name="args">包含首个异常的状态快照。</param>
	private protected abstract void OnError(TcpChannelEventArgs<T> args);

	/// <summary>在开始关闭或最终清理时报告首个故障，每条通道只通知一次。</summary>
	private void ReportError()
	{
		var failure = this.Failure;
		if(failure != null && Interlocked.CompareExchange(ref _errorReported, 1, 0) == 0)
			this.OnError(new(this, this.CloseReason, failure));
	}
	#endregion

	#region 错误分类
	/// <summary>将报文错误和其他收发异常映射为通道结束原因。</summary>
	/// <param name="exception">分帧器或传输报告的异常。</param>
	/// <returns>协议错误或传输错误。</returns>
	private static TcpCloseReason GetCloseReason(Exception exception) => exception is
		InvalidDataException or TcpException { ErrorCode: TcpError.InvalidFrame or TcpError.IncompleteFrame } ?
		TcpCloseReason.ProtocolError : TcpCloseReason.TransportError;
	#endregion

	#region 关闭流程
	/// <summary>停止接收新报文，等待当前处理、发送及通知退场；通道故障仍由 <see cref="WaitAsync" /> 和所属对象的错误事件报告。</summary>
	/// <returns>所属对象的停止等待任务；排空超时仍传播给停止调用方。</returns>
	internal async Task ShutdownAsync()
	{
		_receiver.RequestShutdown();

		try { await _completion.Task.ConfigureAwait(false); }
		catch when(!Volatile.Read(ref _shutdownTimedOut))
		{
			//单连接故障已由 Error 通知并保留在完成结果中，不因注销与停止的先后次序改变所属对象的停止结果。
		}
	}

	/// <summary>立即请求关闭传输；当前处理持有的缓冲仍由接收流程归还。</summary>
	/// <param name="reason">连接关闭原因。</param>
	/// <param name="exception">首个故障；正常关闭时为 <see langword="null" />。</param>
	internal void Abort(TcpCloseReason reason, Exception exception)
	{
		this.RecordFailure(reason, exception);
		_ = this.CloseAsync().AsTask();
	}

	/// <summary>原子保存首个结束原因和首个非空故障。</summary>
	/// <param name="reason">连接关闭原因。</param>
	/// <param name="exception">首个故障；正常关闭时为 <see langword="null" />。</param>
	private void RecordFailure(TcpCloseReason reason, Exception exception)
	{
		//半关闭可能已经记录 Remote，仍需保留排空超时对所属对象停止结果的影响。
		if(reason == TcpCloseReason.ShutdownTimeout)
			Volatile.Write(ref _shutdownTimedOut, true);

		Interlocked.CompareExchange(ref _failure, exception, null);
		Interlocked.CompareExchange(ref _closeReason, (int)reason, 0);
	}

	/// <summary>取消收发并关闭 Socket，在取得唯一写入权后完成输出管道。</summary>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>传输关闭任务，不等待业务处理器退场。</returns>
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		_ = _sender.Stop();
		_cancelling = _lifetime.CancelAsync();
		_transport.Input.CancelPendingRead();
		_transport.Output.CancelPendingFlush();
		_transport.Dispose();

		await _sender.CompleteAsync(this.Failure).ConfigureAwait(false);
	}

	/// <summary>等待发送和取消回调退场，释放协调资源并发布最终结果。</summary>
	/// <returns>通道最终清理任务。</returns>
	private async Task FinishAsync()
	{
		try { await this.CloseAsync().ConfigureAwait(false); }
		catch(Exception exception) { this.RecordFailure(TcpCloseReason.TransportError, exception); }

		try
		{
			await _sender.Stop().ConfigureAwait(false);
			await _cancelling.ConfigureAwait(false);
		}
		catch(Exception exception) { this.RecordFailure(TcpCloseReason.TransportError, exception); }

		_lifetime.Dispose();
		_sender.Dispose();

		try { this.OnCompleted(); }
		catch(Exception exception) { this.RecordFailure(TcpCloseReason.TransportError, exception); }

		this.ReportError();
		this.OnClosed(new(this, this.CloseReason, this.Failure));

		if(this.Failure == null)
			_completion.TrySetResult();
		else
		{
			_completion.TrySetException(this.Failure);

			//所属客户端或服务端已记录故障；标记为已观察，同时保留 await 的异常结果。
			_ = _completion.Task.Exception;
		}
	}

	/// <summary>通知连接所有者移除登记并累计统计。</summary>
	private protected virtual void OnCompleted() { }

	/// <summary>等待全部通道任务完成，并清理尚未启动的通道。</summary>
	/// <param name="disposing">是否释放托管资源。</param>
	/// <returns>与 <see cref="WaitAsync" /> 相同的完成结果。</returns>
	protected override async ValueTask DisposeAsync(bool disposing)
	{
		if(Interlocked.CompareExchange(ref _started, -1, 0) == 0)
		{
			try { await _transport.Input.CompleteAsync().ConfigureAwait(false); }
			finally { await this.FinishAsync().ConfigureAwait(false); }
		}

		await this.WaitAsync().ConfigureAwait(false);
	}
	#endregion
}
