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

using Zongsoft.Services;

namespace Zongsoft.Diagnostics;

public abstract class DiagnostorBase : IDiagnostor, IDisposable
{
	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 成员字段
	private WorkerWrapper _worker;
	private volatile int _disposing;
	#endregion

	#region 构造函数
	protected DiagnostorBase(string name) => this.Name = name ?? string.Empty;
	protected DiagnostorBase(string name, IDiagnostorFiltering meters, IDiagnostorFiltering traces)
	{
		this.Name = name ?? string.Empty;
		this.Meters = meters;
		this.Traces = traces;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public IDiagnostorFiltering Meters { get; set; }
	public IDiagnostorFiltering Traces { get; set; }
	public bool IsDisposed => _disposing == DISPOSED;
	public IWorker Worker => _worker ??= new WorkerWrapper(this);
	#endregion

	#region 公共方法
	public abstract void Open();
	public abstract void Close();
	#endregion

	#region 处置方法
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_disposing = DISPOSED;
		}
	}

	protected virtual void Dispose(bool disposing) => this.Close();
	#endregion

	#region 嵌套子类
	private sealed class WorkerWrapper(DiagnostorBase diagnostor) : WorkerBase()
	{
		private DiagnostorBase _diagnostor = diagnostor;

		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			var diagnostor = _diagnostor ?? throw new ObjectDisposedException(nameof(WorkerWrapper));

			if(!diagnostor.IsDisposed)
				diagnostor.Open();

			return Task.CompletedTask;
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
		{
			var diagnostor = _diagnostor ?? throw new ObjectDisposedException(nameof(WorkerWrapper));

			if(!diagnostor.IsDisposed)
				diagnostor.Close();

			return Task.CompletedTask;
		}

		protected override void Dispose(bool disposing) => _diagnostor = null;
	}
	#endregion
}
