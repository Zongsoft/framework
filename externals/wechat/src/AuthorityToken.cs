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
using System.ComponentModel;
using System.Globalization;

namespace Zongsoft.Externals.Wechat;

/// <summary>表示机构标识的结构。</summary>
[TypeConverter(typeof(TypeConverter))]
public struct AuthorityToken : IEquatable<AuthorityToken>
{
	#region 构造函数
	public AuthorityToken(string identifier, string applet = null)
	{
		this.Identifier = identifier;
		this.Applet = applet;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置机构标识(商户号)。</summary>
	public string Identifier { get; set; }

	/// <summary>获取或设置应用标识(应用号)。</summary>
	public string Applet { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(AuthorityToken other) => this.Identifier == other.Identifier && this.Applet == other.Applet;
	public override bool Equals(object obj) => obj is AuthorityToken other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Identifier, this.Applet);
	public override string ToString() => string.IsNullOrEmpty(this.Applet) ? this.Identifier : $"{this.Identifier}:{this.Applet}";

	public static bool operator ==(AuthorityToken left, AuthorityToken right) => left.Equals(right);
	public static bool operator !=(AuthorityToken left, AuthorityToken right) => !(left == right);
	#endregion

	#region 静态方法
	public static AuthorityToken Parse(string text)
	{
		if(string.IsNullOrEmpty(text))
			throw new ArgumentNullException(nameof(text));

		if(TryParse(text, out var key))
			return key;

		throw new ArgumentException(Properties.Resources.Wechat_AuthorityTokenInvalid_Message);
	}

	public static bool TryParse(string text, out AuthorityToken value)
	{
		value = default;

		if(string.IsNullOrEmpty(text))
			return false;

		var index = text.IndexOfAny([':', '=']);

		if(index < 0)
		{
			value = new AuthorityToken(text);
			return true;
		}

		if(index >= text.Length - 1)
		{
			value = new AuthorityToken(text.Substring(0, index));
			return true;
		}

		if(index > 0 && index < text.Length - 1)
		{
			value = new AuthorityToken(text.Substring(0, index), text.Substring(index + 1));
			return true;
		}

		return false;
	}
	#endregion

	#region 类型转换
	private class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text && AuthorityToken.TryParse(text, out var result))
				return result;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is AuthorityToken token && destinationType == typeof(string))
				return string.IsNullOrEmpty(token.Applet) ? token.Identifier : $"{token.Identifier}:{token.Applet}";

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
	#endregion
}
