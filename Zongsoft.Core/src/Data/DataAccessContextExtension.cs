﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Data;

public static class DataAccessContextExtension
{
	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataInsertContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);
	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataUpsertContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);
	public static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this DataUpdateContextBase context) => GetDataDictionaries<T>((IDataMutateContextBase)context);

	private static IEnumerable<IDataDictionary<T>> GetDataDictionaries<T>(this IDataMutateContextBase context)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		if(context.Count < 1)
			return Array.Empty<IDataDictionary<T>>();

		if(context.IsMultiple)
			return DataDictionary.GetDictionaries<T>((System.Collections.IEnumerable)context.Data);

		return new IDataDictionary<T>[] { DataDictionary.GetDictionary<T>(context.Data) };
	}

	public static bool Validate(this IDataMutateContextBase context, DataAccessMethod method, Metadata.IDataEntityProperty property, out object value)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		if(!context.Options.ValidatorSuppressed)
		{
			var validator = context.Validator;

			if(validator != null)
			{
				switch(method)
				{
					case DataAccessMethod.Insert:
						return validator.OnInsert(context, property, out value);
					case DataAccessMethod.Update:
						return validator.OnUpdate(context, property, out value);
				}
			}
		}

		value = null;
		return false;
	}

	public static bool Validate(this DataInsertContextBase context, Metadata.IDataEntityProperty property, out object value)
	{
		return Validate(context, DataAccessMethod.Insert, property, out value);
	}

	public static bool Validate(this DataUpdateContextBase context, Metadata.IDataEntityProperty property, out object value)
	{
		return Validate(context, DataAccessMethod.Update, property, out value);
	}
}
