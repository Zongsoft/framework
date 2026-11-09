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
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Security.Configuration;

/// <summary>表示授权管理的配置选项。</summary>
public class AuthorizationOptions
{
	#region 公共属性
	/// <summary>获取一个可以进行授权管理的角色集。</summary>
	[TypeConverter(typeof(RolesConverter))]
	public ISet<string> Roles { get; set; }
	#endregion

	#region 嵌套子类
	private class RolesConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) ? true : base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text)
			{
				if(string.IsNullOrEmpty(text))
					return null;

				return new HashSet<string>(Common.StringExtension.Slice(text, ',', ';', '|'));
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType == typeof(string))
			{
				if(value == null)
					return null;

				if(value is IEnumerable<string> strings)
					return string.Join(',', strings);
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
	#endregion
}
