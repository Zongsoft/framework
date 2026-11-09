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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Serialization;

public interface ITextSerializer : ISerializer
{
	#region 公共属性
	TextSerializationOptionsBuilder Options { get; }
	#endregion

	#region 公共方法
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
	#endregion
}
