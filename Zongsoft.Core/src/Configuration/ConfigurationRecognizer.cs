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
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

public class ConfigurationRecognizer : IConfigurationRecognizer
{
	#region 私有字段
	private readonly PropertyInfo _unrecognizedProperty;
	private readonly Type _dictionaryType;
	#endregion

	#region 构造函数
	public ConfigurationRecognizer(PropertyInfo unrecognizedProperty)
	{
		_unrecognizedProperty = unrecognizedProperty ?? throw new ArgumentNullException(nameof(unrecognizedProperty));
		_dictionaryType = ConfigurationUtility.GetImplementedContract(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

		if(_dictionaryType == null || _dictionaryType.GenericTypeArguments[0] != typeof(string))
			throw new InvalidOperationException(string.Format(Properties.Resources.Error_InvalidUnrecognizedProperty, unrecognizedProperty.Name));
	}
	#endregion

	#region 识别方法
	public bool Recognize(object target, IConfigurationSection configuration, ConfigurationBinderOptions options)
	{
		if(target == null)
			return false;

		var unrecognizedProperty = _unrecognizedProperty;
		var dictionary = Reflection.Reflector.GetValue(unrecognizedProperty, ref target);

		if(dictionary == null)
		{
			if(!unrecognizedProperty.CanWrite)
				throw new ConfigurationException($"The {unrecognizedProperty.Name} unrecognized property value is null and it is read-only.");

			if(unrecognizedProperty.PropertyType.IsAbstract)
			{
				var dictionaryType = ConfigurationUtility.GetImplementedContract(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

				if(dictionaryType == null || dictionaryType.GenericTypeArguments[0] != typeof(string))
					throw new InvalidOperationException(string.Format(Properties.Resources.Error_InvalidUnrecognizedProperty, unrecognizedProperty.Name));

				dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), dictionaryType.GenericTypeArguments[1]), new object[] { StringComparer.OrdinalIgnoreCase });
			}
			else
				dictionary = Activator.CreateInstance(unrecognizedProperty.PropertyType);

			Reflection.Reflector.SetValue(unrecognizedProperty, ref target, dictionary);
		}

		this.SetDictionaryValue(dictionary, configuration);

		return true;
	}
	#endregion

	#region 私有方法
	private void SetDictionaryValue(object dictionary, IConfigurationSection configuration)
	{
		if(configuration.Value == null)
		{
			foreach(var child in configuration.GetChildren())
			{
				this.SetDictionaryValue(dictionary, child);
			}
		}

		if(dictionary != null && Common.Convert.TryConvertValue(configuration.Value, _dictionaryType.GenericTypeArguments[1], () => Common.Convert.GetTypeConverter(_unrecognizedProperty), out var convertedValue))
			Reflection.Reflector.SetValue(ref dictionary, "Item", convertedValue, new object[] { configuration.Key });
	}
	#endregion
}
