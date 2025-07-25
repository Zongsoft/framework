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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Collections;

public static class DictionaryExtension
{
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
	public static bool TryGetValue(this IDictionary dictionary, object key, out object value)
	{
		value = null;

		if(dictionary == null || dictionary.Count < 1)
			return false;

		var existed = dictionary.Contains(key);

		if(existed)
			value = dictionary[key];

		return existed;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
	public static bool TryGetValue(this IDictionary dictionary, object key, Action<object> onGot)
	{
		if(dictionary == null || dictionary.Count < 1)
			return false;

		var existed = dictionary.Contains(key);

		if(existed && onGot != null)
			onGot(dictionary[key]);

		return existed;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
	public static bool TryGetValue<TValue>(this IDictionary dictionary, object key, out TValue value, Func<object, TValue> converter = null)
	{
		value = default;

		if(dictionary == null || dictionary.Count < 1)
			return false;

		var existed = dictionary.Contains(key);

		if(existed)
		{
			if(converter == null)
				value = Zongsoft.Common.Convert.ConvertValue<TValue>(dictionary[key]);
			else
				value = converter(dictionary[key]);
		}

		return existed;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
	public static bool TryGetValue<TValue>(this IDictionary dictionary, object key, Action<object> onGot)
	{
		if(dictionary == null || dictionary.Count < 1)
			return false;

		var existed = dictionary.Contains(key);

		if(existed && onGot != null)
			onGot(Zongsoft.Common.Convert.ConvertValue<TValue>(dictionary[key]));

		return existed;
	}

	public static bool TryGetValue<TKey, TValue>(this IDictionary<TKey, object> dictionary, TKey key, out TValue value, Func<object, TValue> converter = null)
	{
		value = default;

		if(dictionary == null || dictionary.Count < 1)
			return false;

		if(dictionary.TryGetValue(key, out object result))
		{
			if(converter == null)
				value = Zongsoft.Common.Convert.ConvertValue<TValue>(result);
			else
				value = converter(result);

			return true;
		}

		return false;
	}

	public static bool TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> onGot)
	{
		if(dictionary == null || dictionary.Count < 1)
			return false;

		if(dictionary.TryGetValue(key, out TValue value) && onGot != null)
		{
			onGot(value);
			return true;
		}

		return false;
	}

	public static SynchronizedDictionary<TKey, TValue> Synchronize<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	{
		if(dictionary == null)
			return null;

		return dictionary as SynchronizedDictionary<TKey, TValue> ?? new(dictionary);
	}

	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary dictionary, Func<object, TKey> keyConvert = null, Func<object, TValue> valueConvert = null) => ToDictionary(dictionary, null, keyConvert, valueConvert);
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary dictionary, IEqualityComparer<TKey> comparer, Func<object, TKey> keyConvert = null, Func<object, TValue> valueConvert = null)
	{
		if(dictionary == null)
			return null;

		if(keyConvert == null)
			keyConvert = key => Zongsoft.Common.Convert.ConvertValue<TKey>(key);

		if(valueConvert == null)
			valueConvert = value => Zongsoft.Common.Convert.ConvertValue<TValue>(value);

		var result = new Dictionary<TKey, TValue>(dictionary.Count, comparer);
		var iterator = dictionary.GetEnumerator();

		while(iterator.MoveNext())
		{
			result.Add(keyConvert(iterator.Entry.Key), valueConvert(iterator.Entry.Value));
		}

		return result;
	}

	public static IEnumerable<DictionaryEntry> ToDictionary(this IEnumerable source)
	{
		if(source == null)
			yield break;

		if(source is IDictionary || source is IEnumerable<DictionaryEntry>)
		{
			foreach(var item in source)
				yield return (DictionaryEntry)item;
		}
		else
		{
			foreach(var item in source)
			{
				if(item == null)
					continue;

				if(item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
				{
					yield return new DictionaryEntry(
						item.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance).GetValue(item),
						item.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).GetValue(item));
				}
			}
		}
	}
}
