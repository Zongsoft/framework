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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Options;
using Zongsoft.Options.Configuration;

namespace Zongsoft.Security.Membership.Options.Configuration
{
	public class AuthorizationOption : OptionConfigurationElement, IAuthorizationOption
	{
		#region 常量定义
		private const string XML_ROLES_ATTRIBUTE = "roles";
		#endregion

		#region 公共属性
		[TypeConverter(typeof(SetConverter))]
		[OptionConfigurationProperty(XML_ROLES_ATTRIBUTE)]
		public ISet<string> Roles
		{
			get
			{
				return (ISet<string>)this[XML_ROLES_ATTRIBUTE];
			}
		}
		#endregion

		#region 嵌套子类
		private class SetConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				return sourceType == typeof(string);
			}

			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			{
				return destinationType == typeof(string);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value != null && value is string text)
				{
					if(string.IsNullOrWhiteSpace(text))
						return null;

					return new HashSet<string>(text.Split(',', ';', '|').Select(p => p.Trim()).Where(p => p.Length > 0), StringComparer.OrdinalIgnoreCase);
				}

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(destinationType == typeof(string))
				{
					if(value != null && value is ISet<string> set && set.Count > 0)
						return string.Join(",", set);
				}

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}
		#endregion
	}
}
