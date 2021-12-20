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

namespace Zongsoft.Externals.Wechat
{
	public readonly struct Account : IEquatable<Account>
	{
		#region 构造函数
		public Account(string code, string secret = null)
		{
			this.Code = code;
			this.Secret = secret;
		}
		#endregion

		#region 公共属性
		public string Code { get; }
		public string Secret { get; }

		public bool IsEmpty { get => string.IsNullOrEmpty(this.Code); }
		#endregion

		#region 重写方法
		public bool Equals(Account other) => string.Equals(this.Code, other.Code);
		public override bool Equals(object obj) => obj is Account other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Code);
		public override string ToString() => string.IsNullOrEmpty(this.Secret) ? this.Code : $"{this.Code}:{this.Secret}";
		#endregion

		#region 符号重写
		public static bool operator ==(Account left, Account right) => left.Equals(right);
		public static bool operator !=(Account left, Account right) => !(left == right);
		#endregion
	}
}
