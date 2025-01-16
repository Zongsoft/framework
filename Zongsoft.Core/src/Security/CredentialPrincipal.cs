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
using System.IO;
using System.Threading;
using System.Security.Claims;

using Microsoft.Extensions.Primitives;

namespace Zongsoft.Security
{
	public class CredentialPrincipal : ClaimsPrincipal, IEquatable<CredentialPrincipal>, IEquatable<ClaimsPrincipal>, IDisposable
	{
		#region 静态字段
		/// <summary>凭证验证的方案名。</summary>
		public static readonly string Scheme = "Credential";
		#endregion

		#region 构造函数
		public CredentialPrincipal(ClaimsIdentity identity, string scenario, TimeSpan? validity = null) : base(identity)
		{
			var (credentialId, renewalToken) = GenerateIdentifier();
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario?.Trim().ToLowerInvariant();

			if(validity.HasValue && validity.Value > TimeSpan.Zero)
				this.Validity = validity.Value;
		}

		public CredentialPrincipal(ClaimsIdentity identity, string credentialId, string renewalToken, string scenario, TimeSpan? validity = null) : base(identity)
		{
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario?.Trim().ToLowerInvariant();

			if(validity.HasValue && validity.Value > TimeSpan.Zero)
				this.Validity = validity.Value;
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

				if(parts.Length > 3 && TimeSpan.TryParse(parts[3], out var validity))
					this.Validity = validity;
			}
		}

		private CredentialPrincipal(ClaimsPrincipal principal, string credentialId, string renewalToken, string scenario, TimeSpan validity) : base(principal)
		{
			this.CredentialId = credentialId;
			this.RenewalToken = renewalToken;
			this.Scenario = scenario;
			this.Validity = validity;
		}
		#endregion

		#region 公共属性
		/// <summary>获取凭证编号。</summary>
		public string CredentialId { get; }

		/// <summary>获取续约令牌。</summary>
		public string RenewalToken { get; }

		/// <summary>获取场景名称。</summary>
		public string Scenario { get; }

		/// <summary>获取或设置有效期时长。</summary>
		public TimeSpan Validity { get; set; }
		#endregion

		#region 公共方法
		public override CredentialPrincipal Clone()
		{
			var (credentialId, renewalToken) = GenerateIdentifier();
			return new CredentialPrincipal(this, credentialId, renewalToken, this.Scenario, this.Validity);
		}

		public CredentialPrincipal Clone(string credentialId, string renewalToken)
		{
			return new CredentialPrincipal(this, credentialId, renewalToken, this.Scenario, this.Validity);
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
		public bool Equals(CredentialPrincipal other) => other is not null && this.CredentialId == other.CredentialId;
		public bool Equals(ClaimsPrincipal other) => other is CredentialPrincipal principal && this.Equals(principal);
		public override bool Equals(object obj) => obj is CredentialPrincipal other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.CredentialId);
		public override string ToString() => $"{this.CredentialId}@{this.Scenario}({this.Validity})";

		protected override ClaimsIdentity CreateClaimsIdentity(BinaryReader reader) => new CredentialIdentity(reader);
		protected override void WriteTo(BinaryWriter writer, byte[] data)
		{
			if(data == null || data.Length == 0)
				data = System.Text.Encoding.UTF8.GetBytes($"{this.CredentialId}|{this.RenewalToken}|{this.Scenario}|{this.Validity}");

			base.WriteTo(writer, data);
		}
		#endregion

		#region 观察方法
		private CancellationTokenSource _cancellation = new();
		public IChangeToken Watch()
		{
			var cancellation = _cancellation;
			return cancellation == null ? ChangedToken.Instance : new CancellationChangeToken(cancellation.Token);
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var cancellation = Interlocked.Exchange(ref _cancellation, null);

			if(cancellation != null)
			{
				cancellation.Cancel();
				cancellation.Dispose();
			}
		}
		#endregion

		#region 私有方法
		private static (string credentialId, string renewalToken) GenerateIdentifier() =>
		(
			$"{(ulong)(DateTime.UtcNow - Common.Timestamp.Millennium.Epoch).TotalSeconds}{Common.Randomizer.GenerateString(8)}",
			$"{(ulong)(DateTime.UtcNow - Common.Timestamp.Millennium.Epoch).TotalDays}{Environment.TickCount64:X}{Common.Randomizer.GenerateString(8)}"
		);
		#endregion

		#region 嵌套子类
		private sealed class ChangedToken : IChangeToken
		{
			public static readonly ChangedToken Instance = new();

			public bool HasChanged => true;
			public bool ActiveChangeCallbacks => true;
			public IDisposable RegisterChangeCallback(Action<object> callback, object state) => Nothing.Instance;

			private sealed class Nothing : IDisposable
			{
				public static readonly Nothing Instance = new();
				public void Dispose() { }
			}
		}
		#endregion
	}
}
