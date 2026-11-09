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

namespace Zongsoft.Serialization;

public static class SerializerExtension
{
	#region 反序列化
	public static object Deserialize(this ISerializer serializer, byte[] buffer, SerializationOptions options = null) => Deserialize(serializer, buffer, 0, -1, options);
	public static object Deserialize(this ISerializer serializer, byte[] buffer, Type type, SerializationOptions options = null) => Deserialize(serializer, buffer, 0, -1, type, options);

	public static object Deserialize(this ISerializer serializer, byte[] buffer, int offset, int count, SerializationOptions options = null) => count > 0 ?
		serializer.Deserialize(new ReadOnlySpan<byte>(buffer, offset, count), options) :
		serializer.Deserialize(new ReadOnlySpan<byte>(buffer), options);

	public static object Deserialize(this ISerializer serializer, byte[] buffer, int offset, int count, Type type, SerializationOptions options = null) => count > 0 ?
		serializer.Deserialize(new ReadOnlySpan<byte>(buffer, offset, count), type, options) :
		serializer.Deserialize(new ReadOnlySpan<byte>(buffer), type, options);

	public static T Deserialize<T>(this ISerializer serializer, byte[] buffer, SerializationOptions options = null) => serializer.Deserialize<T>(buffer, 0, -1, options);
	public static T Deserialize<T>(this ISerializer serializer, byte[] buffer, int offset, int count, SerializationOptions options = null) => count > 0 ?
		serializer.Deserialize<T>(new ReadOnlySpan<byte>(buffer, offset, count), options) :
		serializer.Deserialize<T>(new ReadOnlySpan<byte>(buffer), options);
	#endregion
}
