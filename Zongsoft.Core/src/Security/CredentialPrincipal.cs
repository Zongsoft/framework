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
using System.Collections.Generic;

namespace Zongsoft.Security
{
	public class CredentialPrincipal : ClaimsPrincipal
	{
		#region 构造函数
		public CredentialPrincipal(string credentialId, string renewalToken, string scenario, ClaimsIdentity identity) : base(identity)
		{
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario;
		}

		public CredentialPrincipal(string credentialId, string renewalToken, string scenario, IEnumerable<ClaimsIdentity> identities) : base(identities)
		{
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario;
		}

		public CredentialPrincipal(BinaryReader reader) : base(reader)
		{
			var data = this.CustomSerializationData;

			if(data != null && data.Length > 0)
			{
				var parts = System.Text.Encoding.UTF8.GetString(data).Split('|');

				if(parts.Length > 0)
					this.CredentialId = parts[0];
				if(parts.Length > 1)
					this.RenewalToken = parts[1];
				if(parts.Length > 2)
					this.Scenario = parts[2];

				if(parts.Length > 3 && TimeSpan.TryParse(parts[3], out var expiration))
					this.Expiration = expiration;
			}
		}

		private CredentialPrincipal(ClaimsPrincipal principal, string credentialId, string renewalToken, string scenario, TimeSpan expiration) : base(principal)
		{
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario;
			this.Expiration = expiration;
		}
		#endregion

		#region 公共属性
		/// <summary>获取凭证编号。</summary>
		public string CredentialId { get; }

		/// <summary>获取续约标识。</summary>
		public string RenewalToken { get; }

		/// <summary>获取场景名称。</summary>
		public string Scenario { get; }

		/// <summary>获取或设置过期时长。</summary>
		public TimeSpan Expiration { get; set; }
		#endregion

		#region 公共方法
		public CredentialPrincipal Clone(string credentialId, string renewalToken)
		{
			return new CredentialPrincipal(this, credentialId, renewalToken, this.Scenario, this.Expiration);
		}

		public byte[] Serialize()
		{
			using(var memory = new MemoryStream())
			{
				using(var writer = new BinaryWriter(memory, System.Text.Encoding.UTF8, true))
					this.WriteTo(writer, null);

				return memory.ToArray();
			}
		}

		public static CredentialPrincipal Deserialize(byte[] buffer)
		{
			if(buffer == null || buffer.Length == 0)
				return null;

			using(var memory = new MemoryStream(buffer))
			{
				using(var reader = new BinaryReader(memory))
					return new CredentialPrincipal(reader);
			}
		}

		public static CredentialPrincipal Deserialize(Stream stream)
		{
			if(stream == null)
				return null;

			using(var reader = new BinaryReader(stream))
				return new CredentialPrincipal(reader);
		}
		#endregion

		#region 重写方法
		protected override ClaimsIdentity CreateClaimsIdentity(BinaryReader reader)
		{
			return new CredentialIdentity(reader);
		}

		protected override void WriteTo(BinaryWriter writer, byte[] userData)
		{
			if(userData == null || userData.Length == 0)
				userData = System.Text.Encoding.UTF8.GetBytes(this.CredentialId + "|" + this.RenewalToken + "|" + this.Scenario + "|" + this.Expiration);

			base.WriteTo(writer, userData);
		}
		#endregion
	}
}
