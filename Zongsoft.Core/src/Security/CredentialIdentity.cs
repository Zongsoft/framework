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
using System.IO;
using System.Security.Claims;

namespace Zongsoft.Security
{
	public class CredentialIdentity : ClaimsIdentity
	{
		#region 成员字段
		private readonly string _name;
		#endregion

		#region 构造函数
		public CredentialIdentity(string name, string authenticationType, string issuer, string originalIssuer = null) : base(authenticationType)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();

			if(string.IsNullOrEmpty(issuer))
				issuer = ClaimsIdentity.DefaultIssuer;

			if(string.IsNullOrEmpty(originalIssuer))
				originalIssuer = issuer;

			base.AddClaim(new Claim(ClaimTypes.System, issuer, ClaimValueTypes.String, issuer, originalIssuer, this));
			base.AddClaim(new Claim(base.NameClaimType, _name, ClaimValueTypes.String, issuer, originalIssuer, this));
		}

		public CredentialIdentity(BinaryReader reader) : base(reader)
		{
			_name = base.Name;
		}
		#endregion

		#region 重写属性
		public override string Name => _name;
		public override bool IsAuthenticated => _name != null && _name.Length > 0;
		#endregion
	}
}
