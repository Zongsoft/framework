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
using System.Collections.Concurrent;

namespace Zongsoft.Configuration
{
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
				throw new ArgumentException(string.Format(Zongsoft.Properties.Resources.Error_PropertyNotExists, type, attribute.UnrecognizedProperty));

			if(!unrecognizedProperty.CanRead)
				throw new InvalidOperationException(string.Format(Zongsoft.Properties.Resources.Error_PropertyCannotRead, type, attribute.UnrecognizedProperty));

			return unrecognizedProperty;
		}
		#endregion
	}
}
