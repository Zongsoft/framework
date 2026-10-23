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
using System.Collections.Generic;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat;

public partial class Authority : IAuthority, IEquatable<IAuthority>, IEquatable<Authority>
{
	#region 构造函数
	public Authority(string name, string code, string apiV2Key = null, string apiV3Key = null, SigningCredential signing = null, Verifier verification = null, AccountCollection accounts = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(string.IsNullOrEmpty(code))
			throw new ArgumentNullException(nameof(code));

		ValidateKey(apiV2Key, nameof(apiV2Key));
		ValidateKey(apiV3Key, nameof(apiV3Key));

		if(signing != null && !string.Equals(signing.MerchantId, code, StringComparison.Ordinal))
			throw new ArgumentException(null, nameof(signing));

		this.Name = name;
		this.Code = code;
		this.ApiV2Key = apiV2Key;
		this.ApiV3Key = apiV3Key;
		this.Signing = signing;
		this.Verification = verification?.Copy();
		this.Accounts = accounts ?? new AccountCollection(null);

		static void ValidateKey(string key, string parameter)
		{
			if(key != null && (key.Length != 32 || System.Text.Encoding.UTF8.GetByteCount(key) != 32))
				throw new ArgumentException(null, parameter);
		}
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Code { get; }
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string ApiV2Key { get; }
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string ApiV3Key { get; }
	public SigningCredential Signing { get; }
	public Verifier Verification { get; }
	public AccountCollection Accounts { get; }
	#endregion

	#region 重写方法
	public bool Equals(Authority other) => other != null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(this.Code, other.Code);
	public bool Equals(IAuthority other) => other != null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(this.Code, other.Code);
	public override bool Equals(object obj) => obj is IAuthority other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Code);
	public override string ToString() => string.IsNullOrEmpty(this.Name) || string.Equals(this.Code, this.Name) ? this.Code : $"{this.Code}({this.Name})";
	#endregion
}
