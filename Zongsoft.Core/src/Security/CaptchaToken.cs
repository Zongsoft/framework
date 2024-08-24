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

namespace Zongsoft.Security
{
	/// <summary>
	/// 表示人机识别标识的结构。
	/// </summary>
	public struct CaptchaToken
	{
		#region 构造函数
		public CaptchaToken(string scheme, object value = null, string extra = null)
		{
			if(string.IsNullOrEmpty(scheme))
				throw new ArgumentNullException(nameof(scheme));

			this.Scheme = scheme;
			this.Value = value;
			this.Extra = extra;
		}
		#endregion

		#region 公共属性
		/// <summary>获取人机识别程序的标识。</summary>
		public string Scheme { get; init; }
		/// <summary>获取或设置识别参数数据。</summary>
		public object Value { get; set; }
		/// <summary>获取或设置附加信息。</summary>
		public string Extra { get; set; }

		/// <summary>获取一个值，指示是否为空结构。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty { get => string.IsNullOrEmpty(this.Scheme); }
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrWhiteSpace(this.Extra) ?
			$"{this.Scheme}:{this.Value}" :
			$"{this.Scheme}:{this.Value}?{this.Extra}";
		#endregion

		#region 静态方法
		public static CaptchaToken Parse(string text) => TryParse(text, out var result) ? result : throw new ArgumentException($"Invalid the CAPTCHA parameter format.");
		public static bool TryParse(string text, out CaptchaToken result)
		{
			result = default;

			if(string.IsNullOrEmpty(text))
				return false;

			var index = text.IndexOf(':');

			if(index == 0)
				return false;

			if(index < 0)
			{
				result = new CaptchaToken(text);
				return true;
			}

			result = new CaptchaToken(text.Substring(0, index));

			if(index < text.Length - 1)
				result.Value = text.Substring(index + 1);

			return true;
		}
		#endregion
	}
}
