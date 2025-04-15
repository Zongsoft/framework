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

namespace Zongsoft.Communication;

/// <summary>
/// 提供模板信息发送功能的接口。
/// </summary>
public interface ITransmitter
{
	/// <summary>获取发送器名称。</summary>
	string Name { get; }

	/// <summary>获取或发送器描述。</summary>
	TransmitterDescriptor Descriptor { get; }

	/// <summary>发送模板信息到指定的接收者。</summary>
	/// <param name="destination">指定的接收的目的地。</param>
	/// <param name="template">指定的模板标识。</param>
	/// <param name="argument">指定的模板参数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的异步操作任务。</returns>
	ValueTask TransmitAsync(string destination, string template, object argument, CancellationToken cancellation = default) => this.TransmitAsync(destination, null, template, argument, cancellation);

	/// <summary>发送模板信息到指定的接收者。</summary>
	/// <param name="destination">指定的接收的目的地。</param>
	/// <param name="channel">指定的通道标识。</param>
	/// <param name="template">指定的模板标识。</param>
	/// <param name="argument">指定的模板参数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的异步操作任务。</returns>
	ValueTask TransmitAsync(string destination, string channel, string template, object argument, CancellationToken cancellation = default);
}
