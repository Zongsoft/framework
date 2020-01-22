/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Reflection;
using System.Linq;
using System.Xml;

namespace Zongsoft.Options.Configuration
{
	internal static class OptionConfigurationUtility
	{
		public static OptionConfigurationElement GetGlobalElement(string elementName)
		{
			if(!OptionConfiguration.Declarations.Contains(elementName))
				return null;

			var declaration = OptionConfiguration.Declarations[elementName];
			return Activator.CreateInstance(declaration.Type) as OptionConfigurationElement;
		}

		public static OptionConfigurationProperty GetKeyProperty(OptionConfigurationElement element)
		{
			if(element == null)
				return null;

			return element.Properties.FirstOrDefault(property => property.IsKey);
		}

		public static OptionConfigurationProperty GetDefaultCollectionProperty(OptionConfigurationPropertyCollection properties)
		{
			if(properties == null || properties.Count < 1)
				return null;

			return properties.FirstOrDefault(property => property.IsDefaultCollection);
		}

		public static string GetValueString(object value, System.ComponentModel.TypeConverter converter)
		{
			if(value == null)
				return string.Empty;

			if(value is string)
				return (string)value;

			if(converter != null)
				return converter.ConvertToString(value);
			else
				return Zongsoft.Common.Convert.ConvertValue<string>(value);
		}
	}
}
