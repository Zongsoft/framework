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

namespace Zongsoft.Serialization
{
	public interface ITextSerializer : ISerializer
	{
		TextSerializationOptionsBuilder Options { get; }

		string Serialize(object graph, TextSerializationOptions options = null);
		string Serialize(object graph, Type type, TextSerializationOptions options = null);
		ValueTask<string> SerializeAsync(object graph, CancellationToken cancellation = default);
		ValueTask<string> SerializeAsync(object graph, TextSerializationOptions options, CancellationToken cancellation = default);
		ValueTask<string> SerializeAsync(object graph, Type type, CancellationToken cancellation = default);
		ValueTask<string> SerializeAsync(object graph, Type type, TextSerializationOptions options, CancellationToken cancellation = default);

		object Deserialize(string text, TextSerializationOptions options = null);
		object Deserialize(string text, Type type, TextSerializationOptions options = null);
		T Deserialize<T>(string text, TextSerializationOptions options = null);

		ValueTask<object> DeserializeAsync(string text, CancellationToken cancellation = default);
		ValueTask<object> DeserializeAsync(string text, TextSerializationOptions options, CancellationToken cancellation = default);
		ValueTask<object> DeserializeAsync(string text, Type type, CancellationToken cancellation = default);
		ValueTask<object> DeserializeAsync(string text, Type type, TextSerializationOptions options, CancellationToken cancellation = default);
		ValueTask<T> DeserializeAsync<T>(string text, CancellationToken cancellation = default);
		ValueTask<T> DeserializeAsync<T>(string text, TextSerializationOptions options, CancellationToken cancellation = default);
	}
}
