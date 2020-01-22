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

using Zongsoft.Options;
using Zongsoft.Options.Configuration;

namespace Zongsoft.Diagnostics.Configuration
{
	public class LoggerHandlerElement : OptionConfigurationElement
	{
		#region 常量定义
		private const string XML_NAME_ATTRIBUTE = "name";
		private const string XML_TYPE_ATTRIBUTE = "type";
		private const string XML_PREDICATION_ELEMENT = "predication";
		private const string XML_PARAMETERS_COLLECTION = "parameters";
		#endregion

		#region 公共属性
		[OptionConfigurationProperty(XML_NAME_ATTRIBUTE, Behavior = OptionConfigurationPropertyBehavior.IsKey)]
		public string Name
		{
			get
			{
				return (string)this[XML_NAME_ATTRIBUTE];
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				this[XML_NAME_ATTRIBUTE] = value;
			}
		}

		[OptionConfigurationProperty(XML_TYPE_ATTRIBUTE, Behavior = OptionConfigurationPropertyBehavior.IsRequired)]
		public string TypeName
		{
			get
			{
				return (string)this[XML_TYPE_ATTRIBUTE];
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				this[XML_TYPE_ATTRIBUTE] = value;
			}
		}

		[OptionConfigurationProperty(XML_PREDICATION_ELEMENT)]
		public LoggerHandlerPredicationElement Predication
		{
			get
			{
				return (LoggerHandlerPredicationElement)this[XML_PREDICATION_ELEMENT];
			}
		}

		[OptionConfigurationProperty(XML_PARAMETERS_COLLECTION, ElementName = "parameter")]
		public SettingElementCollection Parameters
		{
			get
			{
				return (SettingElementCollection)this[XML_PARAMETERS_COLLECTION];
			}
		}

		public bool HasExtendedProperties
		{
			get
			{
				return base.HasUnrecognizedProperties;
			}
		}

		public IDictionary<string, string> ExtendedProperties
		{
			get
			{
				return base.UnrecognizedProperties;
			}
		}
		#endregion

		#region 重写方法
		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			this.UnrecognizedProperties.Add(name, value);
			return true;
		}
		#endregion

		#region 嵌套子类
		public class LoggerHandlerPredicationElement : OptionConfigurationElement
		{
			#region 常量定义
			private const string XML_SOURCE_ATTRIBUTE = "source";
			private const string XML_EXCEPTIONTYPE_ATTRIBUTE = "exceptionType";
			private const string XML_MINLEVEL_ATTRIBUTE = "minLevel";
			private const string XML_MAXLEVEL_ATTRIBUTE = "maxLevel";
			#endregion

			#region 公共属性
			[OptionConfigurationProperty(XML_SOURCE_ATTRIBUTE)]
			public string Source
			{
				get
				{
					return (string)this[XML_SOURCE_ATTRIBUTE];
				}
				set
				{
					this[XML_SOURCE_ATTRIBUTE] = value;
				}
			}

			[OptionConfigurationProperty(XML_EXCEPTIONTYPE_ATTRIBUTE)]
			public Type ExceptionType
			{
				get
				{
					return (Type)this[XML_EXCEPTIONTYPE_ATTRIBUTE];
				}
				set
				{
					this[XML_EXCEPTIONTYPE_ATTRIBUTE] = value;
				}
			}

			[OptionConfigurationProperty(XML_MINLEVEL_ATTRIBUTE)]
			public LogLevel? MinLevel
			{
				get
				{
					return (LogLevel?)this[XML_MINLEVEL_ATTRIBUTE];
				}
				set
				{
					this[XML_MINLEVEL_ATTRIBUTE] = value;
				}
			}

			[OptionConfigurationProperty(XML_MAXLEVEL_ATTRIBUTE)]
			public LogLevel? MaxLevel
			{
				get
				{
					return (LogLevel?)this[XML_MAXLEVEL_ATTRIBUTE];
				}
				set
				{
					this[XML_MAXLEVEL_ATTRIBUTE] = value;
				}
			}
			#endregion
		}
		#endregion
	}
}
