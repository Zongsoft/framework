using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 嵌套类型
	public sealed class Preauthorization
	{
		public Preauthorization(string componentId, string code, DateTimeOffset expiration)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(componentId);
			ArgumentException.ThrowIfNullOrWhiteSpace(code);
			this.ComponentId = componentId;
			this.Code = code;
			this.Expiration = expiration;
		}

		public string ComponentId { get; }
		public string Code { get; }
		public DateTimeOffset Expiration { get; }

		public Uri GetUrl(Uri redirectUri, AccountType type, bool mobile = false)
		{
			ArgumentNullException.ThrowIfNull(redirectUri);

			if(this.Expiration <= DateTimeOffset.UtcNow)
				throw new Common.OperationException("PREAUTHORIZATION_EXPIRED", null);
			ValidateRedirect(redirectUri, type);

			var prefix = mobile ? "https://open.weixin.qq.com/wxaopen/safe/bindcomponent?action=bindcomponent&no_scan=1&" : "https://mp.weixin.qq.com/cgi-bin/componentloginpage?";
			return new Uri($"{prefix}component_appid={Uri.EscapeDataString(this.ComponentId)}&pre_auth_code={Uri.EscapeDataString(this.Code)}&redirect_uri={Uri.EscapeDataString(redirectUri.AbsoluteUri)}&auth_type={(type == AccountType.Channel ? 1 : 2)}{(mobile ? "#wechat_redirect" : "")}");
		}
	}

	public sealed class AuthorizationRequest
	{
		public string State { get; init; }
		public string ComponentId { get; init; }
		public string Owner { get; init; }
		public string Context { get; init; }
		public AccountType AccountType { get; init; }
		public Uri RedirectUri { get; init; }
		public Uri AuthorizationUrl { get; init; }
		public DateTimeOffset Expiration { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string PreauthorizationCode { get; init; }
	}

	public sealed class Authorization
	{
		public string ComponentId { get; init; }
		public string AppId { get; init; }
		public IReadOnlyList<int> Permissions { get; init; } = [];
		public DateTimeOffset? Expiration { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string AccessToken { get; init; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string RefreshToken { get; init; }
	}
	#endregion

	#region 私有方法
	private static void ValidateRedirect(Uri redirectUri, AccountType type)
	{
		ArgumentNullException.ThrowIfNull(redirectUri);

		if(!redirectUri.IsAbsoluteUri || redirectUri.Scheme != "https" || !string.IsNullOrEmpty(redirectUri.UserInfo) || !string.IsNullOrEmpty(redirectUri.Fragment) || redirectUri.AbsoluteUri.Length > 2048)
			throw new ArgumentException(null, nameof(redirectUri));
		if(type is not (AccountType.Channel or AccountType.Applet))
			throw new ArgumentException(null, nameof(type));
	}
	#endregion

}
