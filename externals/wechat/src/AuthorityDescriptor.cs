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

namespace Zongsoft.Externals.Wechat;

/// <summary>表示商户配置快照的描述符。</summary>
public class AuthorityDescriptor : Authority, IEquatable<AuthorityDescriptor>
{
	public AuthorityDescriptor(string name, string code, string apiV2Key = null, string apiV3Key = null, SigningCredential signing = null, Verifier verification = null) :
		base(name, code, apiV2Key, apiV3Key, signing, verification)
	{ }

	public bool Equals(AuthorityDescriptor other) => base.Equals(other);
	public override bool Equals(object obj) => obj is AuthorityDescriptor other && this.Equals(other);
	public override int GetHashCode() => base.GetHashCode();
	public override string ToString() => $"{this.Name}#{this.Code}";
	public static bool operator ==(AuthorityDescriptor left, AuthorityDescriptor right) => Equals(left, right);
	public static bool operator !=(AuthorityDescriptor left, AuthorityDescriptor right) => !Equals(left, right);
}
