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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Net;

public partial class TcpServer<T>
{
	#region 广播发送
	/// <summary>以有界并发广播，分别报告成功、失败和容量拒绝；并发新连接不保证包含在内。</summary>
	/// <param name="package">广播完成前保持稳定的报文。</param>
	/// <param name="cancellation">调用者取消向上传播。</param>
	/// <returns>本地发送统计。</returns>
	/// <exception cref="TcpException">广播容量不足时错误码为 <see cref="TcpError.SendRejected" />；服务未运行时为 <see cref="TcpError.ConnectionClosed" />。</exception>
	public ValueTask<TcpBroadcastResult> BroadcastAsync(T package, CancellationToken cancellation = default) => _broadcaster.SendAsync(package, cancellation);

	/// <summary>将发送委托给 <see cref="BroadcastAsync" />，不返回逐通道统计。</summary>
	/// <param name="package">广播完成前保持稳定的报文。</param>
	/// <param name="cancellation">调用者取消向上传播。</param>
	/// <returns>广播提交操作。</returns>
	async ValueTask ISender<T>.SendAsync(T package, CancellationToken cancellation) => await this.BroadcastAsync(package, cancellation).ConfigureAwait(false);
	#endregion

	#region 嵌套类型
	//广播接纳、并发发送和停止排空共享同一个状态所有者。
	private sealed class Broadcaster(TcpServer<T> server)
	{
		#region 成员字段
		private readonly object _sync = new();
		private TaskCompletionSource _completion;
		private int _count;
		private bool _stopped = true;
		#endregion

		#region 启停方法
		/// <summary>允许接纳新广播；服务运行状态仍由服务端检查。</summary>
		public void Start()
		{
			lock(_sync)
				_stopped = false;
		}

		/// <summary>停止接纳新广播并等待已接纳广播退场。</summary>
		/// <returns>已接纳广播的共享完成任务。</returns>
		public Task Stop()
		{
			lock(_sync)
			{
				_stopped = true;
				if(_count == 0)
					return Task.CompletedTask;

				_completion ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
				return _completion.Task;
			}
		}
		#endregion

		#region 广播方法
		/// <summary>以有界并发广播，分别报告成功、失败和容量拒绝；并发新连接不保证包含在内。</summary>
		/// <param name="package">广播完成前保持稳定的报文。</param>
		/// <param name="cancellation">调用者取消向上传播。</param>
		/// <returns>本地发送统计。</returns>
		public async ValueTask<TcpBroadcastResult> SendAsync(T package, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			lock(_sync)
			{
				if(_stopped || server.State != WorkerState.Running)
					throw new TcpException(TcpError.ConnectionClosed);

				if(_count >= server._options.MaximumConcurrentBroadcasts)
					throw new TcpException(TcpError.SendRejected);

				_count++;
			}

			var succeeded = 0;
			var failed = 0;
			var rejected = 0;

			try
			{
				await Parallel.ForEachAsync(server.Channels, new ParallelOptions { MaxDegreeOfParallelism = server._options.BroadcastConcurrency, CancellationToken = cancellation }, SendAsync).ConfigureAwait(false);
			}
			finally { Complete(); }

			cancellation.ThrowIfCancellationRequested();
			return new(succeeded, failed, rejected);

			async ValueTask SendAsync(TcpServerChannel<T> channel, CancellationToken token)
			{
				using var deadline = new CancellationTokenSource(server._options.SendTimeout, server._options.TimeProvider);
				using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, deadline.Token);

				try
				{
					await channel.SendAsync(package, linked.Token).ConfigureAwait(false);
					Interlocked.Increment(ref succeeded);
				}
				catch(OperationCanceledException) when(token.IsCancellationRequested) { throw; }
				catch(TcpException exception) when(exception.ErrorCode == TcpError.SendRejected) { Interlocked.Increment(ref rejected); }
				catch { Interlocked.Increment(ref failed); }
			}

			void Complete()
			{
				TaskCompletionSource completed = null;

				lock(_sync)
				{
					if(--_count == 0)
					{
						completed = _completion;
						_completion = null;
					}
				}

				completed?.TrySetResult();
			}
		}
		#endregion
	}
	#endregion
}
