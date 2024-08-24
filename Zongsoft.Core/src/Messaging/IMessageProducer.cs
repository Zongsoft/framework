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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息生产者的接口。
	/// </summary>
	public interface IMessageProducer
	{
		/// <summary>生产消息。</summary>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="encoding">指定的消息文本的字符编码方案。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="tags">指定要生产的消息标签。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="encoding">指定的消息文本的字符编码方案。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="tags">指定要生产的消息标签。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default);

		/// <summary>生产消息。</summary>
		/// <param name="topic">指定要生产的消息主题。</param>
		/// <param name="tags">指定要生产的消息标签。</param>
		/// <param name="data">指定要生产的消息数据。</param>
		/// <param name="encoding">指定的消息文本的字符编码方案。</param>
		/// <param name="options">指定要生产的消息选项。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>如果消息生产成功则返回对应结果标识的异步任务，否则返回空的异步任务。</returns>
		ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<char> data, Encoding encoding, MessageEnqueueOptions options = null, CancellationToken cancellation = default);
	}
}
