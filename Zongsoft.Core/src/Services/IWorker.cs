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

namespace Zongsoft.Services
{
	/// <summary>
	/// 关于工作器的接口。
	/// </summary>
	/// <remarks>
	///		<para>对于实现者的约定：应支持 <see cref="Start(string[])"/>、<see cref="Stop(string[])"/>、<see cref="Pause"/>、<see cref="Resume"/> 这四个工作方法的线程重入隔离性。</para>
	/// </remarks>
	public interface IWorker
	{
		#region 事件定义
		/// <summary>表示状态发生了改变。</summary>
		event EventHandler<WorkerStateChangedEventArgs> StateChanged;
		#endregion

		#region 属性定义
		/// <summary>获取当前工作器的名称。</summary>
		string Name { get; }

		/// <summary>获取当前工作器的状态。</summary>
		WorkerState State { get; }

		/// <summary>获取或设置一个值，指示工作器是否可用。</summary>
		bool Enabled { get; set; }

		/// <summary>获取工作器是否允许暂停和继续。</summary>
		bool CanPauseAndContinue { get; }
		#endregion

		#region 方法定义
		/// <summary>启动工作器。</summary>
		/// <param name="args">启动的参数。</param>
		void Start(params string[] args);

		/// <summary>启动工作器。</summary>
		/// <param name="args">启动的参数。</param>
		/// <param name="cancellation">异步操作取消标记。</param>
		Task StartAsync(string[] args, CancellationToken cancellation = default);

		/// <summary>停止工作器。</summary>
		/// <param name="args">停止的参数。</param>
		void Stop(params string[] args);

		/// <summary>停止工作器。</summary>
		/// <param name="args">停止的参数。</param>
		/// <param name="cancellation">异步操作取消标记。</param>
		Task StopAsync(string[] args, CancellationToken cancellation = default);

		/// <summary>暂停工作器。</summary>
		void Pause();

		/// <summary>暂停工作器。</summary>
		/// <param name="cancellation">异步操作取消标记。</param>
		Task PauseAsync(CancellationToken cancellation = default);

		/// <summary>恢复工作器，继续运行。</summary>
		void Resume();

		/// <summary>恢复工作器，继续运行。</summary>
		/// <param name="cancellation">异步操作取消标记。</param>
		Task ResumeAsync(CancellationToken cancellation = default);
		#endregion
	}
}
