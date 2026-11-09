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
using System.Collections.Generic;

namespace Zongsoft.Common;

public static class ArrayExtension
{
	#region 私有变量
	private static readonly Dictionary<Type, Array> _empties = new();
	#endregion

	/// <summary>获取一个值，指示数组是否为空(<c>null</c>)或长度为零。</summary>
	/// <param name="array">指定要判断的数组。</param>
	/// <returns>如果数组为空或空则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool IsEmpty(this Array array) => array == null || array.Length == 0;

	/// <summary>获取指定类型的空数组（即元素数量为零的数组）。</summary>
	/// <param name="type">指定的数组元素类型。</param>
	/// <returns>返回指定元素类型的空数组。</returns>
	public static Array Empty(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		if(_empties.TryGetValue(type, out var value))
			return value;

		lock(_empties)
		{
			if(_empties.TryGetValue(type, out value))
				return value;

			var array = Array.CreateInstance(type, 0);
			return _empties.TryAdd(type, array) ? array : _empties[type];
		}
	}
}
