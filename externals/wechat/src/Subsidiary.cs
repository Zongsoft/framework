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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Globalization;

namespace Zongsoft.Externals.Wechat
{
	/// <summary>
	/// 表示子商户标识的结构。
	/// </summary>
	[TypeConverter(typeof(TypeConverter))]
	public struct Subsidiary : IEquatable<Subsidiary>
	{
		#region 常量定义
		private const string KEY_MERCHANTID = "sub_mchid";
		private const string KEY_APPID = "sub_appid";
		#endregion

		#region 构造函数
		public Subsidiary(uint merchantId, string appId = null)
		{
			this.MerchantId = merchantId;
			this.AppId = appId;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置子商户编号。</summary>
		public uint MerchantId { get; set; }

		/// <summary>获取或设置子商户应用号。</summary>
		public string AppId { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Subsidiary other) => this.MerchantId == other.MerchantId && this.AppId == other.AppId;
		public override bool Equals(object obj) => obj is Subsidiary other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.MerchantId, this.AppId);
		public override string ToString() => string.IsNullOrEmpty(this.AppId) ? this.MerchantId.ToString() : $"{this.MerchantId}:{this.AppId}";

		public static bool operator ==(Subsidiary left, Subsidiary right) => left.Equals(right);
		public static bool operator !=(Subsidiary left, Subsidiary right) => !(left == right);
		#endregion

		#region 静态方法
		public static Subsidiary Parse(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			if(TryParse(text, out var key))
				return key;

			throw new ArgumentException($"Invalid format of the {typeof(Subsidiary).Name}.");
		}

		public static bool TryParse(string text, out Subsidiary value)
		{
			value = default;

			if(string.IsNullOrEmpty(text))
				return false;

			var index = text.IndexOfAny(new[] { ':', '=' });

			if(index < 0)
			{
				if(uint.TryParse(text, out var id) && id > 0)
				{
					value = new Subsidiary(id);
					return true;
				}

				return false;
			}

			if(index >= text.Length - 1)
			{
				if(uint.TryParse(text.Substring(0, index), out var id) && id > 0)
				{
					value = new Subsidiary(id);
					return true;
				}

				return false;
			}

			if(index > 0 && index < text.Length - 1)
			{
				if(uint.TryParse(text.Substring(0, index), out var id) && id > 0)
				{
					value = new Subsidiary(id, text.Substring(index + 1));
					return true;
				}
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
				if(value is string text && Subsidiary.TryParse(text, out var subsidiary))
					return subsidiary;

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(value is Subsidiary subsidiary && destinationType == typeof(string))
					return string.IsNullOrEmpty(subsidiary.AppId) ? subsidiary.MerchantId.ToString() : $"{subsidiary.MerchantId}:{subsidiary.AppId}";

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}
		#endregion
	}
}
