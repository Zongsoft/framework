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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Common;

public class Timer : IDisposable
{
	#region 成员字段
	private PeriodicTimer _timer;
	private CancellationTokenSource _cancellation;
	private Func<object, CancellationToken, ValueTask> _tick;
	#endregion

	#region 构造函数
	public Timer(TimeSpan period, Func<object, CancellationToken, ValueTask> tick = null)
	{
		_tick = tick;
		_timer = new PeriodicTimer(period);
	}
	#endregion

	#region 公共方法
	public void Stop() => _cancellation?.Cancel();
	public void Start(CancellationToken cancellation = default) => this.Start(null, cancellation);
	public async void Start(object state, CancellationToken cancellation = default)
	{
		if(cancellation.IsCancellationRequested)
			return;

		_cancellation = new();

		await Task.Factory.StartNew(async state =>
		{
			try
			{
				while(await _timer.WaitForNextTickAsync(_cancellation.Token))
				{
					await this.OnTickAsync(state, _cancellation.Token);
				}
			}
			catch(OperationCanceledException) { }
			catch(Exception ex) { Zongsoft.Diagnostics.Logger.GetLogger<Timer>().Error(ex); }
		}, state, cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	}
	#endregion

	#region 虚拟方法
	protected virtual ValueTask OnTickAsync(object state, CancellationToken cancellation) => _tick?.Invoke(state, cancellation) ?? default;
	#endregion

	#region 释放方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		var timer = Interlocked.Exchange(ref _timer, null);
		if(timer == null)
			return;

		_cancellation?.Cancel();

		if(disposing)
		{
			_timer.Dispose();
			_cancellation.Dispose();
		}

		_tick = null;
		_cancellation = null;
	}
	#endregion
}
