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

public struct BitVector32(int data) : IEquatable<BitVector32>
{
	#region 成员字段
	private int _data = data;
	#endregion

	#region 公共属性
	public readonly int Data => _data;

	public bool this[int bit]
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
				var oldData = _data;
				int newData;

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
	public static implicit operator int(BitVector32 vector) => vector._data;
	public static implicit operator BitVector32(int data) => new(data);
	#endregion

	#region 符号重写
	public static bool operator ==(BitVector32 left, BitVector32 right) => left._data == right._data;
	public static bool operator !=(BitVector32 left, BitVector32 right) => left._data != right._data;
	#endregion

	#region 重写方法
	public readonly bool Equals(BitVector32 other) => _data == other._data;
	public override readonly bool Equals(object obj) => obj is BitVector32 other && this.Equals(other);
	public override readonly int GetHashCode() => _data.GetHashCode();
	public override readonly string ToString() => _data.ToString();
	#endregion
}
