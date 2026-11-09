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

public static class JsonElementExtension
{
	public static T[] GetArray<T>(this JsonElement element, Func<JsonElement, T> elementor)
	{
		if(element.ValueKind != JsonValueKind.Array)
			return null;

		var index = 0;
		var array = new T[element.GetArrayLength()];
		var iterator = element.EnumerateArray();

		while(iterator.MoveNext())
		{
			array[index++] = elementor(iterator.Current);
		}

		return array;
	}
}
