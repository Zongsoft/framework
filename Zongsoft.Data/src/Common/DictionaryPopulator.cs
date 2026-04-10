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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common;

public class DictionaryPopulator : IDataPopulator
{
	#region 成员字段
	private readonly Func<int, IDictionary> _creator;
	private readonly IDataEntityProperty[] _properties;
	#endregion

	#region 构造函数
	internal protected DictionaryPopulator(Type type, IDataEntityProperty[] properties)
	{
		_properties = properties ?? throw new ArgumentNullException(nameof(properties));
		_creator = this.GetCreator(type);
	}
	#endregion

	#region 公共方法
	public T Populate<T>(IDataRecord record)
	{
		if(record.FieldCount != _properties.Length)
			throw new DataException("The record of populate has failed.");

		//创建一个对应的实体字典
		var dictionary = _creator(record.FieldCount);

		for(var i = 0; i < record.FieldCount; i++)
		{
			if(record.IsDBNull(i))
				continue;

			var property = _properties[i];
			if(property == null)
				continue;

			dictionary[property.Name] = GetValue(record, property, i, false);
		}

		return dictionary.Count > 0 ? (T)dictionary : default;

		static object GetValue(IDataRecord record, IDataEntityProperty property, int ordinal, bool nullable) =>
			property.IsSimplex(out var simplex) ? record.GetValue(ordinal, simplex.Type.DbType, nullable) : null;
	}
	#endregion

	#region 虚拟方法
	protected virtual Func<int, IDictionary> GetCreator(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		if(type.IsInterface)
		{
			if(Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IDictionary<,>), type))
				return capacity => new Dictionary<string, object>(capacity, StringComparer.OrdinalIgnoreCase);
			else
				return capacity => new Hashtable(capacity, StringComparer.OrdinalIgnoreCase);
		}

		if(type.IsAbstract)
			throw new InvalidOperationException($"The specified '{type.FullName}' type is an abstract class that the dictionary populator cannot to populate.");

		if(!typeof(IDictionary).IsAssignableFrom(type))
			throw new InvalidOperationException($"The specified '{type.FullName}' type does not implement the {nameof(IDictionary)} interface that the dictionary populator cannot to populate.");

		return capacity => (IDictionary)System.Activator.CreateInstance(type);
	}
	#endregion
}
