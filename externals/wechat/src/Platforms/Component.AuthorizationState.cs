using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 嵌套类型
	public sealed class AuthorizationState
	{
		public string ComponentId { get; init; }
		public string AppId { get; init; }
		public AccountType AccountType { get; init; }
		public string GrantId { get; init; }
		public string Revision { get; init; }
		public bool Enabled { get; init; }
		public IReadOnlyList<int> Permissions { get; init; } = [];
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public AuthorizationCredential Credential { get; init; }
	}

	public sealed class AuthorizationCredential
	{
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string Value { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string RefreshToken { get; init; }
		public DateTimeOffset Expiration { get; init; }
	}

	public sealed class AuthorizerInfo
	{
		public string AppId { get; init; }
		public AccountType AccountType { get; init; }
		public string Name { get; init; }
		public int Status { get; init; }
		public IReadOnlyList<int> Permissions { get; init; } = [];
	}
	#endregion
}
