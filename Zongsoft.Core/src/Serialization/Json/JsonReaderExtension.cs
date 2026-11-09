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
