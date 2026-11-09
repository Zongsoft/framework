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
using System.Collections.Concurrent;

namespace Zongsoft.Configuration;

public class ConfigurationRecognizerProvider : IConfigurationRecognizerProvider
{
	#region 单例字段
	public static readonly ConfigurationRecognizerProvider Default = new ConfigurationRecognizerProvider();
	#endregion

	#region 私有字段
	private readonly ConcurrentDictionary<Type, IConfigurationRecognizer> _recognizers;
	#endregion

	#region 构造函数
	protected ConfigurationRecognizerProvider()
	{
		_recognizers = new ConcurrentDictionary<Type, IConfigurationRecognizer>();
	}
	#endregion

	#region 公共方法
	public IConfigurationRecognizer GetRecognize(Type type)
	{
		return _recognizers.GetOrAdd(type, key => this.CreateRecognizer(key));
	}
	#endregion

	#region 虚拟方法
	protected virtual IConfigurationRecognizer CreateRecognizer(Type type)
	{
		return new ConfigurationRecognizer(GetUnrecognizedProperty(type));
	}
	#endregion

	#region 静态方法
	internal static PropertyInfo GetUnrecognizedProperty(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		var attribute = type.GetConfigurationAttribute();

		if(attribute == null || string.IsNullOrEmpty(attribute.UnrecognizedProperty))
			return default;

		var unrecognizedProperty = type.GetProperty(attribute.UnrecognizedProperty) ??
			throw new ArgumentException(string.Format(Zongsoft.Properties.Resources.Error_PropertyNotExists_Message, type, attribute.UnrecognizedProperty));

		if(!unrecognizedProperty.CanRead)
			throw new InvalidOperationException(string.Format(Zongsoft.Properties.Resources.Error_PropertyCannotRead_Message, type, attribute.UnrecognizedProperty));

		return unrecognizedProperty;
	}
	#endregion
}
