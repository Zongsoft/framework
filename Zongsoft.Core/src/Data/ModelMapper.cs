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

namespace Zongsoft.Data;

public static class ModelMapper
{
	public static T Map<T>(this IModel source, T result = default)
	{
		if(source == null || !source.HasChanges())
			return result;

		if(typeof(T).IsAbstract)
		{
			if(result == null)
				result = Model.Build<T>();

			if(result is IModel model)
			{
				foreach(var entry in source.GetChanges())
				{
					model.TrySetValue(entry.Key, entry.Value);
				}

				return result;
			}
		}

		if(result == null)
			result = Activator.CreateInstance<T>();

		foreach(var entry in source.GetChanges())
		{
			Reflection.Reflector.TrySetValue(ref result, entry.Key, entry.Value);
		}

		return result;
	}

	public static T Map<T>(this IDataDictionary source, T result = default)
	{
		if(source == null || source.IsEmpty)
			return result;

		if(typeof(T).IsAbstract)
		{
			if(result == null)
				result = Model.Build<T>();

			if(result is IModel model)
			{
				foreach(var entry in (IEnumerable<KeyValuePair<string, object>>)source)
				{
					model.TrySetValue(entry.Key, entry.Value);
				}

				return result;
			}
		}

		if(result == null)
			result = Activator.CreateInstance<T>();

		foreach(var entry in (IEnumerable<KeyValuePair<string, object>>)source)
		{
			Reflection.Reflector.TrySetValue(ref result, entry.Key, entry.Value);
		}

		return result;
	}
}
