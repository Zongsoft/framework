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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat;

public class Applet
{
	#region 构造函数
	public Applet(Account account)
	{
		if(account.IsEmpty || account.Type != AccountType.Applet)
			throw new ArgumentException(null, nameof(account));

		this.Account = account;
	}
	#endregion

	#region 公共属性
	public Account Account { get; }
	#endregion

	#region 获取凭证
	public ValueTask<string> GetCredentialAsync(bool refresh, CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, refresh, cancellation);
	#endregion

	#region 登录方法
	public async ValueTask<LoginResult> LoginAsync(string token, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(token);

		if(this.Account.Component == null && string.IsNullOrWhiteSpace(this.Account.Secret))
			throw new InvalidOperationException();

		var componentToken = this.Account.Component == null ? null : await this.Account.GetComponentCredentialAsync(18, cancellation);
		var path = componentToken == null ?
			$"sns/jscode2session?appid={Uri.EscapeDataString(this.Account.Code)}&secret={Uri.EscapeDataString(this.Account.Secret)}&js_code={Uri.EscapeDataString(token)}&grant_type=authorization_code" :
			$"sns/component/jscode2session?appid={Uri.EscapeDataString(this.Account.Code)}&grant_type=authorization_code&component_appid={Uri.EscapeDataString(this.Account.ComponentId)}&component_access_token={Uri.EscapeDataString(componentToken)}&js_code={Uri.EscapeDataString(token)}";
		using var client = AccountHttpClient.Create();
		using var response = await client.GetAsync(path, cancellation);
		var result = await response.GetResultAsync<LoginResultWrapper>(cancellation);

		if(string.IsNullOrWhiteSpace(result.OpenId) || string.IsNullOrWhiteSpace(result.Secret) || Convert.FromBase64String(result.Secret).Length != 16)
			throw new FormatException();

		await this.Account.EnsureAuthorizationAsync(18, cancellation);
		return new LoginResult(result.OpenId, result.Secret, string.IsNullOrWhiteSpace(result.UnionId) ? null : result.UnionId);
	}
	#endregion

	#region 手机号码
	public ValueTask<PhoneInfo> GetPhoneNumberAsync(string token, CancellationToken cancellation = default) => this.GetPhoneNumberAsync(token, null, cancellation);
	public async ValueTask<PhoneInfo> GetPhoneNumberAsync(string token, string openId, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(token);
		var credential = await this.Account.GetCredentialAsync(18, false, cancellation);
		using var client = AccountHttpClient.Create();
		using var request = new HttpRequestMessage(HttpMethod.Post, $"wxa/business/getuserphonenumber?access_token={Uri.EscapeDataString(credential)}")
		{
			Content = JsonContent.Create(new { code = token, openid = openId }, options: Json.Options),
		};
		using var response = await client.SendAsync(request, cancellation);
		var result = await response.GetResultAsync<PhoneInfoWrapper>(cancellation);

		if(result.Phone == null || result.Phone.Watermark.AppId != this.Account.Code || result.Phone.Watermark.Timestamp <= 0 ||
			string.IsNullOrWhiteSpace(result.Phone.PhoneNumber) || string.IsNullOrWhiteSpace(result.Phone.PurePhoneNumber) || string.IsNullOrWhiteSpace(result.Phone.CountryCode))
			throw new FormatException();

		await this.Account.EnsureAuthorizationAsync(18, cancellation);
		return new PhoneInfo(result.Phone.PhoneNumber, result.Phone.PurePhoneNumber, result.Phone.CountryCode);
	}
	#endregion

	#region 嵌套结构
	public readonly struct LoginResult
	{
		public LoginResult(string openId, string secret, string unionId)
		{
			this.OpenId = openId;
			this.Secret = secret;
			this.UnionId = unionId;
		}

		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string Secret { get; }
		public string OpenId { get; }
		public string UnionId { get; }
	}

	public readonly record struct PhoneInfo(string PhoneNumber, string PurePhoneNumber, string CountryCode);

	private struct LoginResultWrapper
	{
		[JsonPropertyName("session_key")]
		public string Secret { get; set; }
		[JsonPropertyName("openid")]
		public string OpenId { get; set; }
		[JsonPropertyName("unionid")]
		public string UnionId { get; set; }
	}

	private struct PhoneInfoWrapper
	{
		[JsonPropertyName("phone_info")]
		public Phone Phone { get; set; }
	}

	private sealed class Phone
	{
		public string PhoneNumber { get; set; }
		public string PurePhoneNumber { get; set; }
		public string CountryCode { get; set; }
		public Watermark Watermark { get; set; }
	}

	private struct Watermark
	{
		[JsonPropertyName("appid")]
		public string AppId { get; set; }
		public long Timestamp { get; set; }
	}
	#endregion
}
