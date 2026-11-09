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
using System.Data;

namespace Zongsoft.Data.Common;

public static class ScalarPopulator
{
	#region 单例字段
	public static readonly IDataPopulator Char = new ValuePopulator<char>();
	public static readonly IDataPopulator Guid = new ValuePopulator<Guid>();
	public static readonly IDataPopulator String = new ValuePopulator<string>();
	public static readonly IDataPopulator Boolean = new ValuePopulator<bool>();
	public static readonly IDataPopulator DateTime = new ValuePopulator<DateTime>();
	public static readonly IDataPopulator Date = new ValuePopulator<DateOnly>();
	public static readonly IDataPopulator Time = new ValuePopulator<TimeOnly>();
	public static readonly IDataPopulator TimeSpan = new ValuePopulator<TimeSpan>();
	public static readonly IDataPopulator DateTimeOffset = new ValuePopulator<DateTimeOffset>();

	public static readonly IDataPopulator Byte = new ValuePopulator<byte>();
	public static readonly IDataPopulator SByte = new ValuePopulator<sbyte>();
	public static readonly IDataPopulator Int16 = new ValuePopulator<short>();
	public static readonly IDataPopulator Int32 = new ValuePopulator<int>();
	public static readonly IDataPopulator Int64 = new ValuePopulator<long>();
	public static readonly IDataPopulator UInt16 = new ValuePopulator<ushort>();
	public static readonly IDataPopulator UInt32 = new ValuePopulator<uint>();
	public static readonly IDataPopulator UInt64 = new ValuePopulator<ulong>();

	public static readonly IDataPopulator Single = new ValuePopulator<float>();
	public static readonly IDataPopulator Double = new ValuePopulator<double>();
	public static readonly IDataPopulator Decimal = new ValuePopulator<decimal>();

	public static readonly IDataPopulator Bytes = new ValuePopulator<byte[]>();
	public static readonly IDataPopulator Chars = new ValuePopulator<char[]>();
	#endregion

	#region 嵌套子类
	private class ValuePopulator<T> : IDataPopulator, IDataPopulator<T>
	{
		TResult IDataPopulator.Populate<TResult>(IDataRecord record) => record.GetValue<TResult>(0);

		public T Populate(IDataRecord record) => record.GetValue<T>(0);
	}
	#endregion
}

public static class NullablePopulator
{
	#region 单例字段
	public static readonly IDataPopulator Char = new ValuePopulator<char>();
	public static readonly IDataPopulator Guid = new ValuePopulator<Guid>();
	public static readonly IDataPopulator String = new ValuePopulator<string>();
	public static readonly IDataPopulator Boolean = new ValuePopulator<bool>();
	public static readonly IDataPopulator DateTime = new ValuePopulator<DateTime>();
	public static readonly IDataPopulator Date = new ValuePopulator<DateOnly>();
	public static readonly IDataPopulator Time = new ValuePopulator<TimeOnly>();
	public static readonly IDataPopulator TimeSpan = new ValuePopulator<TimeSpan>();
	public static readonly IDataPopulator DateTimeOffset = new ValuePopulator<DateTimeOffset>();

	public static readonly IDataPopulator Byte = new ValuePopulator<byte>();
	public static readonly IDataPopulator SByte = new ValuePopulator<sbyte>();
	public static readonly IDataPopulator Int16 = new ValuePopulator<short>();
	public static readonly IDataPopulator Int32 = new ValuePopulator<int>();
	public static readonly IDataPopulator Int64 = new ValuePopulator<long>();
	public static readonly IDataPopulator UInt16 = new ValuePopulator<ushort>();
	public static readonly IDataPopulator UInt32 = new ValuePopulator<uint>();
	public static readonly IDataPopulator UInt64 = new ValuePopulator<ulong>();

	public static readonly IDataPopulator Single = new ValuePopulator<float>();
	public static readonly IDataPopulator Double = new ValuePopulator<double>();
	public static readonly IDataPopulator Decimal = new ValuePopulator<decimal>();

	public static readonly IDataPopulator Bytes = new ValuePopulator<byte[]>();
	public static readonly IDataPopulator Chars = new ValuePopulator<char[]>();
	#endregion

	#region 嵌套子类
	private class ValuePopulator<T> : IDataPopulator, IDataPopulator<T>
	{
		TResult IDataPopulator.Populate<TResult>(IDataRecord record) => record.IsDBNull(0) ? default : record.GetValue<TResult>(0);

		public T Populate(IDataRecord record) => record.IsDBNull(0) ? default : record.GetValue<T>(0);
	}
	#endregion
}
