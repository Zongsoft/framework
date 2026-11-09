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

namespace Zongsoft.Common;

public struct BitVector64 : IEquatable<BitVector64>
{
	#region 成员字段
	private long _data;
	#endregion

	#region 构造函数
	public BitVector64(int data) => _data = data;
	public BitVector64(long data) => _data = data;
	#endregion

	#region 公共属性
	public readonly long Data => _data;

	public bool this[long bit]
	{
		get
		{
			var data = _data;
			return (data & bit) == bit;
		}
		set
		{
			while(true)
			{
				long oldData = _data;
				long newData;

				if(value)
					newData = oldData | bit;
				else
					newData = oldData & ~bit;

				var result = Interlocked.CompareExchange(ref _data, newData, oldData);

				if(result == oldData)
					break;
			}
		}
	}
	#endregion

	#region 类型转换
	public static implicit operator long(BitVector64 vector) => vector._data;
	public static implicit operator BitVector64(long data) => new(data);
	public static implicit operator BitVector64(BitVector32 vector) => new(vector.Data);
	#endregion

	#region 符号重写
	public static bool operator ==(BitVector64 left, BitVector64 right) => left._data == right._data;
	public static bool operator !=(BitVector64 left, BitVector64 right) => left._data != right._data;
	#endregion

	#region 重写方法
	public readonly bool Equals(BitVector32 other) => _data == other.Data;
	public readonly bool Equals(BitVector64 other) => _data == other._data;
	public override readonly bool Equals(object obj) => obj is BitVector64 other && this.Equals(other);
	public override readonly int GetHashCode() => _data.GetHashCode();
	public override readonly string ToString() => _data.ToString();
	#endregion
}
