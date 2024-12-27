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

namespace Zongsoft.Serialization
{
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
}
