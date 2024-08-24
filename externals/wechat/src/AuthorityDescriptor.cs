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

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat
{
	/// <summary>
	/// 表示机构描述符的类。
	/// </summary>
	public class AuthorityDescriptor : IEquatable<AuthorityDescriptor>
	{
		#region 构造函数
		public AuthorityDescriptor(string name, string code, string secret, ICertificate certificate)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrEmpty(code))
				throw new ArgumentNullException(nameof(code));

			this.Name = name;
			this.Code = code;
			this.Secret = secret;
			this.Certificate = certificate;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Code { get; }
		public string Secret { get; }
		public ICertificate Certificate { get; }
		#endregion

		#region 重写方法
		public bool Equals(AuthorityDescriptor other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(this.Code, other.Code);
		public override bool Equals(object obj) => obj is AuthorityDescriptor other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Code);
		public override string ToString() => $"{this.Name}#{this.Code}";
		#endregion

		#region 符号重写
		public static bool operator ==(AuthorityDescriptor left, AuthorityDescriptor right) => left.Equals(right);
		public static bool operator !=(AuthorityDescriptor left, AuthorityDescriptor right) => !(left == right);
		#endregion
	}
}
