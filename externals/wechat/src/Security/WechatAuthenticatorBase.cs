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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Security;
using Zongsoft.Security.Membership;

namespace Zongsoft.Externals.Wechat.Security
{
	public abstract class WechatAuthenticatorBase : IAuthenticator, IAuthenticator<string, WechatAuthenticatorBase.Identity>
	{
		#region 构造函数
		protected WechatAuthenticatorBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public string Name { get => "Wechat"; }
		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 身份校验
		async ValueTask<object> IAuthenticator.VerifyAsync(string key, object data, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation) => await this.VerifyAsync(key, await GetSecretAsync(data, cancellation), scenario, parameters, cancellation);
		public async ValueTask<Identity> VerifyAsync(string key, string secret, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(secret))
				throw new ArgumentNullException(nameof(secret));

			if(!Account.TryParse(key, out var account))
				throw new ArgumentException($"The specified ‘{key}’ key is not recognized.", nameof(key));

			return account.Type switch
			{
				AccountType.Applet => await GetAppletToken(AppletManager.GetApplet(account), secret, parameters, cancellation),
				AccountType.Channel => await GetChannelToken(ChannelManager.GetChannel(account), secret, parameters, cancellation),
				_ => throw new InvalidOperationException(),
			};
		}
		#endregion

		#region 身份签发
		ValueTask<ClaimsIdentity> IAuthenticator.IssueAsync(object identity, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation) => this.IssueAsync((Identity)identity, scenario, parameters);
		public ValueTask<ClaimsIdentity> IssueAsync(Identity identity, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identity.OpenId))
				return ValueTask.FromResult<ClaimsIdentity>(null);

			return this.OnIssueAsync(identity, scenario, parameters, cancellation);
		}

		protected abstract ValueTask<ClaimsIdentity> OnIssueAsync(Identity identity, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation);
		#endregion

		#region 私有方法
		private static async ValueTask<Identity> GetAppletToken(Applet applet, string code, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			var result = await applet.LoginAsync(code, cancellation);
			var info = await applet.Users.GetInfoAsync(result.OpenId, cancellation);

			return string.IsNullOrEmpty(info.UnionId) ?
				new Identity(applet.Account, result.OpenId, info.Nickname, info.Avatar, info.Description, parameters) :
				new Identity(applet.Account, result.OpenId, info.UnionId, info.Nickname, info.Avatar, info.Description);
		}

		private static async ValueTask<Identity> GetChannelToken(Channel channel, string code, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			var credential = await channel.Authentication.AuthenticateAsync(code, cancellation);
			var openId = credential.OpenId;
			var unionId = string.IsNullOrEmpty(credential.UnionId) ? credential.User.OpenId : credential.UnionId;

			return string.IsNullOrEmpty(unionId) ?
				new Identity(channel.Account, openId, credential.User.Nickname, credential.User.Avatar, credential.User.Description, parameters) :
				new Identity(channel.Account, openId, unionId, credential.User.Nickname, credential.User.Avatar, credential.User.Description, parameters);
		}

		private static async ValueTask<string> GetSecretAsync(object data, CancellationToken cancellation = default)
		{
			var text = data as string;

			if(text == null)
			{
				if(data is byte[] bytes)
					text = System.Text.Encoding.UTF8.GetString(bytes);
				else if(data is System.IO.Stream stream)
				{
					using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
					text = await reader.ReadToEndAsync();
				}
				else
					throw new InvalidOperationException($"The identity verification data type '{data.GetType().FullName}' is not supported.");
			}

			return text;
		}
		#endregion

		#region 嵌套结构
		public struct Identity
		{
			public Identity(in Account account, string openId, string unionId, IDictionary<string, object> parameters)
			{
				this.Account = account;
				this.OpenId = openId;
				this.UnionId = unionId;
				this.Nickname = null;
				this.Avatar = null;
				this.Description = null;
				this.Parameters = parameters;
			}

			public Identity(in Account account, string openId, string nickname, string avatar, string description = null, IDictionary<string, object> parameters = null)
			{
				this.Account = account;
				this.OpenId = openId;
				this.UnionId = null;
				this.Nickname = nickname;
				this.Avatar = avatar;
				this.Description = description;
				this.Parameters = parameters;
			}

			public Identity(in Account account, string openId, string unionId, string nickname, string avatar, string description = null, IDictionary<string, object> parameters = null)
			{
				this.Account = account;
				this.OpenId = openId;
				this.UnionId = unionId;
				this.Nickname = nickname;
				this.Avatar = avatar;
				this.Description = description;
				this.Parameters = parameters;
			}

			public readonly Account Account;
			public readonly string OpenId;
			public readonly string UnionId;
			public readonly string Nickname;
			public readonly string Avatar;
			public readonly string Description;
			public readonly IDictionary<string, object> Parameters;
		}
		#endregion
	}
}
