/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
	/// 提供关于通讯侦听的功能的接口。
	/// </summary>
	public interface IListener<T> : Services.IWorker, IDisposable
	{
		/// <summary>获取当前是否处于侦听状态。</summary>
		bool IsListening { get; }

		/// <summary>
		/// 处理请求。
		/// </summary>
		/// <param name="package">处理的请求包。</param>
		void Handle(T package);

		/// <summary>
		/// 处理请求。
		/// </summary>
		/// <param name="package">处理的请求包。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		Task HandleAsync(T package, CancellationToken cancellation = default);
	}
}
