using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 嵌套类型
	public enum Receipt
	{
		Rejected,
		Accepted,
		Duplicate,
	}

	public enum NotificationKind
	{
		VerificationTicket,
		Authorized,
		Updated,
		Unauthorized,
	}

	public sealed class VerificationTicket
	{
		public string AppId { get; init; }
		public DateTimeOffset CreatedTime { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string Value { get; init; }
	}

	public sealed class Notification
	{
		public string Id { get; init; }
		public string AppId { get; init; }
		public NotificationKind Kind { get; init; }
		public DateTimeOffset CreatedTime { get; init; }
		public string AuthorizerAppId { get; init; }
		public DateTimeOffset? Expiration { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string Value { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string PreauthorizationCode { get; init; }
	}
	#endregion
}
