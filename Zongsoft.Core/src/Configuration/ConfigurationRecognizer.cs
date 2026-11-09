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
			throw new InvalidOperationException(string.Format(Properties.Resources.Error_InvalidUnrecognizedProperty_Message, unrecognizedProperty.Name));
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
				throw new ConfigurationException(string.Format(Properties.Resources.Configuration_UnrecognizedPropertyReadOnly_Message, unrecognizedProperty.Name));

			if(unrecognizedProperty.PropertyType.IsAbstract)
			{
				var dictionaryType = ConfigurationUtility.GetImplementedContract(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

				if(dictionaryType == null || dictionaryType.GenericTypeArguments[0] != typeof(string))
					throw new InvalidOperationException(string.Format(Properties.Resources.Error_InvalidUnrecognizedProperty_Message, unrecognizedProperty.Name));

				dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), dictionaryType.GenericTypeArguments[1]), new object[] { StringComparer.OrdinalIgnoreCase });
			}
			else
				dictionary = Activator.CreateInstance(unrecognizedProperty.PropertyType);

			Reflection.Reflector.SetValue(unrecognizedProperty, ref target, dictionary);
		}

		this.SetDictionaryValue(dictionary, configuration, null);

		return true;
	}
	#endregion

	#region 私有方法
	private void SetDictionaryValue(object dictionary, IConfigurationSection configuration, string key)
	{
		if(configuration.Value == null)
		{
			//注意：子节点如果是集合元素则使用当前节点的键作为字典键，因为子节点键名等于其值(即其键名无意义)
			foreach(var child in configuration.GetChildren())
				this.SetDictionaryValue(dictionary, child, child.IsCollection() ? configuration.Key : null);
		}
		else if(dictionary != null && Common.Convert.TryConvertValue(configuration.Value, _dictionaryType.GenericTypeArguments[1], () => Common.Convert.GetTypeConverter(_unrecognizedProperty), out var convertedValue))
			Reflection.Reflector.SetValue(ref dictionary, "Item", convertedValue, [key ?? configuration.Key]);
	}
	#endregion
}
