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
using System.Text.Json;

namespace Zongsoft.Serialization.Json;

public static class JsonReaderExtension
{
	#region 委托定义
	private delegate T Getter<T>(in Utf8JsonReader reader);
	#endregion

	#region 静态构造
	static JsonReaderExtension()
	{
		JsonGetterTemplate<string>.Get = new Getter<string>((in reader) => reader.GetString());
		JsonGetterTemplate<bool>.Get = new Getter<bool>((in reader) => reader.GetBoolean());
		JsonGetterTemplate<byte>.Get = new Getter<byte>((in reader) => reader.GetByte());
		JsonGetterTemplate<sbyte>.Get = new Getter<sbyte>((in reader) => reader.GetSByte());
		JsonGetterTemplate<short>.Get = new Getter<short>((in reader) => reader.GetInt16());
		JsonGetterTemplate<ushort>.Get = new Getter<ushort>((in reader) => reader.GetUInt16());
		JsonGetterTemplate<int>.Get = new Getter<int>((in reader) => reader.GetInt32());
		JsonGetterTemplate<uint>.Get = new Getter<uint>((in reader) => reader.GetUInt32());
		JsonGetterTemplate<long>.Get = new Getter<long>((in reader) => reader.GetInt64());
		JsonGetterTemplate<ulong>.Get = new Getter<ulong>((in reader) => reader.GetUInt64());
		JsonGetterTemplate<float>.Get = new Getter<float>((in reader) => reader.GetSingle());
		JsonGetterTemplate<double>.Get = new Getter<double>((in reader) => reader.GetDouble());
		JsonGetterTemplate<decimal>.Get = new Getter<decimal>((in reader) => reader.GetDecimal());
		JsonGetterTemplate<DateTime>.Get = new Getter<DateTime>((in reader) => reader.GetDateTime());
		JsonGetterTemplate<DateTimeOffset>.Get = new Getter<DateTimeOffset>((in reader) => reader.GetDateTimeOffset());
		JsonGetterTemplate<Guid>.Get = new Getter<Guid>((in reader) => reader.GetGuid());
	}
	#endregion

	#region 扩展方法
	public static T GetValue<T>(this in Utf8JsonReader reader) => JsonGetterTemplate<T>.Get(reader);
	#endregion

	#region 私有子类
	private static class JsonGetterTemplate<T>
	{
		public static Getter<T> Get;
	}
	#endregion
}
