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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>接收传输通道还原的事件，不再次向远端交换。</summary>
public interface IEventReceiver
{
	/// <summary>将接收到的事件交给本地事件注册表处理。</summary>
	/// <param name="name">事件的完整限定名称。</param>
	/// <param name="argument">还原后的事件实参。</param>
	/// <param name="parameters">事件参数集合。</param>
	/// <param name="cancellation">取消事件处理的令牌。</param>
	/// <returns>本地事件处理任务。</returns>
	ValueTask ReceiveAsync(string name, object argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
