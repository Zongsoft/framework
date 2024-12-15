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

namespace Zongsoft.Communication
{
	/// <summary>
	/// 表示通道的接口。
	/// </summary>
	public interface IChannel : IAsyncDisposable
	{
		#region 事件定义
		/// <summary>表示通道关闭完成的事件。</summary>
		event EventHandler Closed;
		#endregion

		#region 属性定义
		/// <summary>获取一个值，指示通道是否已关闭。</summary>
		bool IsClosed { get; }

		/// <summary>获取一个值，指示通道是否已释放。</summary>
		bool IsDisposed { get; }
		#endregion

		#region 方法定义
		/// <summary>关闭通道。</summary>
		/// <param name="cancellation">指定的关闭操作的异步标记。</param>
		/// <returns>返回的关闭任务。</returns>
		ValueTask CloseAsync(CancellationToken cancellation = default);
		#endregion
	}
}
