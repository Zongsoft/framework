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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat;

public class Channel
{
	#region 成员字段
	private volatile UserProvider _users;
	private volatile ChannelMessager _messager;
	private volatile ChannelAuthentication _authentication;
	#endregion

	#region 构造函数
	public Channel(Account account)
	{
		if(account.IsEmpty || account.Type != AccountType.Channel)
			throw new ArgumentException(null, nameof(account));

		this.Account = account;
	}
	#endregion

	#region 公共属性
	public Account Account { get; }

	public UserProvider Users
	{
		get
		{
			if(_users == null)
				Interlocked.CompareExchange(ref _users, new UserProvider(this.Account), null);

			return _users;
		}
	}

	public Membership GetMembership(IAuthority authority, string brandId) => new(authority, this.Account, brandId);

	public ChannelMessager Messager
	{
		get
		{
			if(_messager == null)
				Interlocked.CompareExchange(ref _messager, new ChannelMessager(this.Account), null);

			return _messager;
		}
	}

	public ChannelAuthentication Authentication
	{
		get
		{
			if(_authentication == null)
				Interlocked.CompareExchange(ref _authentication, new ChannelAuthentication(this.Account), null);

			return _authentication;
		}
	}
	#endregion

	#region 获取凭证
	public ValueTask<string> GetCredentialAsync(bool refresh, CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, refresh, cancellation);
	#endregion

	#region 计算邮戳
	public async ValueTask<(byte[] data, string nonce, long timestamp, TimeSpan period)> PostmarkAsync(string url, CancellationToken cancellation = default)
	{
		if(!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo))
			throw new ArgumentException(null, nameof(url));

		var fragment = url.IndexOf('#');
		if(fragment >= 0)
			url = url[..fragment];

		var result = await CredentialManager.GetTicketAsync(this.Account, "jsapi", false, cancellation);

		if(string.IsNullOrEmpty(result.ticket))
			return default;

		var nonce = Randomizer.GenerateString(16);
		var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
		var period = result.period;

		var text = $"jsapi_ticket={result.ticket}&noncestr={nonce}&timestamp={timestamp}&url={url}";
		return (System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(text)), nonce, timestamp, period);
	}
	#endregion
}
