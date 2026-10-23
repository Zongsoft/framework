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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat;

public partial class ChannelAuthentication
{
	public ChannelAuthentication(Account account)
	{
		if(account.IsEmpty || account.Type != AccountType.Channel)
			throw new ArgumentException(null, nameof(account));

		this.Account = account;
	}

	public Account Account { get; }

	public async ValueTask<Credential> AuthenticateAsync(string token, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(token);

		if(this.Account.Component == null && string.IsNullOrWhiteSpace(this.Account.Secret))
			throw new InvalidOperationException();

		var componentToken = this.Account.Component == null ? null : await this.Account.GetComponentCredentialAsync(4, cancellation);
		var started = DateTimeOffset.UtcNow;
		using var client = AccountHttpClient.Create();
		using var response = await client.GetAsync(componentToken == null ?
			$"sns/oauth2/access_token?appid={Uri.EscapeDataString(this.Account.Code)}&secret={Uri.EscapeDataString(this.Account.Secret)}&code={Uri.EscapeDataString(token)}&grant_type=authorization_code" :
			$"sns/oauth2/component/access_token?appid={Uri.EscapeDataString(this.Account.Code)}&code={Uri.EscapeDataString(token)}&grant_type=authorization_code&component_appid={Uri.EscapeDataString(this.Account.ComponentId)}&component_access_token={Uri.EscapeDataString(componentToken)}", cancellation);
		var result = await response.GetResultAsync<AuthenticationResult>(cancellation);
		await this.Account.EnsureAuthorizationAsync(4, cancellation);
		var credential = this.CreateCredential(result, started, default);

		if(HasUserPermission(credential.Permission))
		{
			var user = await this.GetUserInfoAsync(credential, cancellation);
			credential = new Credential(credential.AppId, credential.AccessToken, credential.RenewalToken, credential.OpenId, credential.UnionId ?? user.UnionId, credential.Expiration, credential.Permission, user, credential.ComponentId);
		}

		return credential;
	}

	public async ValueTask<Credential> RefreshAsync(Credential credential, CancellationToken cancellation = default)
	{
		this.EnsureAccount(credential);
		await this.Account.EnsureAuthorizationAsync(4, cancellation);
		ArgumentException.ThrowIfNullOrWhiteSpace(credential.RenewalToken);
		var componentToken = this.Account.Component == null ? null : await this.Account.GetComponentCredentialAsync(4, cancellation);
		var started = DateTimeOffset.UtcNow;
		using var client = AccountHttpClient.Create();
		using var response = await client.GetAsync(componentToken == null ?
			$"sns/oauth2/refresh_token?appid={Uri.EscapeDataString(this.Account.Code)}&grant_type=refresh_token&refresh_token={Uri.EscapeDataString(credential.RenewalToken)}" :
			$"sns/oauth2/component/refresh_token?appid={Uri.EscapeDataString(this.Account.Code)}&grant_type=refresh_token&component_appid={Uri.EscapeDataString(this.Account.ComponentId)}&component_access_token={Uri.EscapeDataString(componentToken)}&refresh_token={Uri.EscapeDataString(credential.RenewalToken)}", cancellation);
		var result = await response.GetResultAsync<AuthenticationResult>(cancellation);
		await this.Account.EnsureAuthorizationAsync(4, cancellation);

		if(result.OpenId != credential.OpenId || !string.IsNullOrEmpty(result.UnionId) && !string.IsNullOrEmpty(credential.UnionId) && result.UnionId != credential.UnionId)
			throw new FormatException();

		return this.CreateCredential(result, started, HasUserPermission(result.Permission) ? credential.User : default, credential.UnionId);
	}

	public async ValueTask ValidateAsync(Credential credential, CancellationToken cancellation = default)
	{
		this.EnsureAccount(credential);
		await this.Account.EnsureAuthorizationAsync(4, cancellation);
		using var client = AccountHttpClient.Create();
		using var response = await client.GetAsync($"sns/auth?access_token={Uri.EscapeDataString(credential.AccessToken)}&openid={Uri.EscapeDataString(credential.OpenId)}", cancellation);
		var result = await response.GetResultAsync<System.Text.Json.JsonElement>(cancellation);

		if(result.ValueKind != System.Text.Json.JsonValueKind.Object || !result.TryGetProperty("errcode", out var code) || !code.TryGetInt32(out var number) || number != 0)
			throw new FormatException();
	}

	public async ValueTask<UserInfo> GetUserInfoAsync(Credential credential, CancellationToken cancellation = default)
	{
		this.EnsureAccount(credential);
		await this.Account.EnsureAuthorizationAsync(4, cancellation);

		if(!HasUserPermission(credential.Permission))
			throw new InvalidOperationException();

		using var client = AccountHttpClient.Create();
		using var response = await client.GetAsync($"sns/userinfo?access_token={Uri.EscapeDataString(credential.AccessToken)}&openid={Uri.EscapeDataString(credential.OpenId)}&lang=zh_CN", cancellation);
		var result = await response.GetResultAsync<UserInfoResult>(cancellation);

		if(result.OpenId != credential.OpenId || !string.IsNullOrEmpty(result.UnionId) && !string.IsNullOrEmpty(credential.UnionId) && result.UnionId != credential.UnionId)
			throw new FormatException();

		return result.ToInfo(credential.OpenId, credential.UnionId);
	}

	private static bool HasUserPermission(string permission) => permission?.Split(',', StringSplitOptions.TrimEntries).Contains("snsapi_userinfo", StringComparer.Ordinal) == true;
	private void EnsureAccount(Credential credential)
	{
		if(credential.AppId != this.Account.Code || credential.ComponentId != this.Account.ComponentId || string.IsNullOrWhiteSpace(credential.AccessToken) || string.IsNullOrWhiteSpace(credential.OpenId))
			throw new ArgumentException(null, nameof(credential));
	}

	private Credential CreateCredential(AuthenticationResult result, DateTimeOffset started, UserInfo user, string unionId = null)
	{
		if(string.IsNullOrWhiteSpace(result.AccessToken) || string.IsNullOrWhiteSpace(result.RenewalToken) || string.IsNullOrWhiteSpace(result.OpenId) || result.Period <= 0 || string.IsNullOrWhiteSpace(result.Permission))
			throw new FormatException();

		return new Credential(this.Account.Code, result.AccessToken, result.RenewalToken, result.OpenId,
			string.IsNullOrWhiteSpace(result.UnionId) ? unionId : result.UnionId, started.AddSeconds(result.Period), result.Permission, user, this.Account.ComponentId);
	}
}
