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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Terminals;

partial class Terminal
{
	partial class ConsoleTerminal
	{
		#region 标准输入
		private sealed class StandardReader : TextReader
		{
			private readonly object _sync = new();
			private Task<string> _pending;
			private TaskCompletionSource<string> _request;
			private Thread _thread;
			private int _reading;

			public override int Peek() => throw new NotSupportedException(Properties.Resources.OperationException_Unsupported_Message);
			public override int Read() => throw new NotSupportedException(Properties.Resources.OperationException_Unsupported_Message);
			public override string ReadLine() => throw new NotSupportedException(Properties.Resources.OperationException_Unsupported_Message);
			public override Task<string> ReadLineAsync() => this.ReadLineAsync(CancellationToken.None).AsTask();
			public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if(Interlocked.CompareExchange(ref _reading, 1, 0) != 0)
					throw new InvalidOperationException(Properties.Resources.Terminal_InputReading_Message);

				return this.ReadAsync(cancellationToken);
			}

			private async ValueTask<string> ReadAsync(CancellationToken cancellation)
			{
				try
				{
					_pending ??= this.RequestRead();

					var result = await _pending.WaitAsync(cancellation).ConfigureAwait(false);
					if(result != null)
						_pending = null;

					return result;
				}
				finally { Volatile.Write(ref _reading, 0); }
			}

			private Task<string> RequestRead()
			{
				var request = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

				lock(_sync)
				{
					_request = request;

					if(_thread == null)
						StartReader();
					else
						Monitor.Pulse(_sync);
				}

				//等待者取消后仍观察读取故障，后续调用继续取得同一结果。
				_ = request.Task.ContinueWith(static task => { _ = task.Exception; }, CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

				return request.Task;

				void StartReader()
				{
					_thread = new Thread(this.ReadInput) { IsBackground = true, Name = "Zongsoft.Terminal.Input" };

					try { _thread.Start(); }
					catch
					{
						_thread = null;
						_request = null;
						throw;
					}
				}
			}

			private void ReadInput()
			{
				while(true)
				{
					TaskCompletionSource<string> request;

					lock(_sync)
					{
						while(_request == null)
							Monitor.Wait(_sync);

						request = _request;
						_request = null;
					}

					try
					{
						var result = System.Console.In.ReadLine();
						request.TrySetResult(result);

						if(result == null)
							return;
					}
					catch(Exception exception)
					{
						request.TrySetException(exception);
						return;
					}
				}
			}
		}
		#endregion
	}
}
