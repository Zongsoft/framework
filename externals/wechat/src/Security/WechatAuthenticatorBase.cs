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
	public abstract class WechatAuthenticatorBase : IAuthenticator, IAuthenticator<WechatAuthenticatorBase.Ticket, WechatAuthenticatorBase.Token>
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
		async ValueTask<OperationResult> IAuthenticator.VerifyAsync(string key, object data, string scenario, CancellationToken cancellation) => await this.VerifyAsync(key, Ticket.GetTicket(data), scenario, cancellation);
		public async ValueTask<OperationResult<Token>> VerifyAsync(string key, Ticket ticket, string scenario, CancellationToken cancellation = default)
		{
			if(ticket.IsEmpty)
				return OperationResult.Fail("InvalidTicket");

			var index = key.IndexOf(':');

			if(index <= 0)
			{
				switch(ticket.Scheme?.ToLowerInvariant())
				{
					case "channel":
						var options = Utility.GetOptions<Options.ChannelOptions>($"/Externals/Wechat/Channels/{key}");

						if(options == null || string.IsNullOrEmpty(options.Name))
							return OperationResult.Fail("ChannelNotFound", $"The specified '{key}' channel does not exist.");

						var channel = new Channel(new Account(options.Name, options.Secret));
						return await GetChannelToken(channel, key, ticket.Token, cancellation);
					default:
						return OperationResult.Fail("InvalidTicket", $"The specified ‘{ticket.Scheme}’ ticket scheme is not recognized.");
				}
			}
			else
			{
				var provider = this.ServiceProvider.ResolveRequired<IAccountProvider>();
				var account = provider.GetAccount(key);

				if(account.IsEmpty)
					return OperationResult.Fail("AccountNotFound", $"The specified '{key}' account does not exist.");

				switch(ticket.Scheme?.ToLowerInvariant())
				{
					case "channel":
						var channel = new Channel(account);
						return await GetChannelToken(channel, key, ticket.Token, cancellation);
					default:
						return OperationResult.Fail("InvalidTicket", $"The specified ‘{ticket.Scheme}’ ticket scheme is not recognized.");
				}
			}
		}
		#endregion

		#region 身份签发
		ValueTask<ClaimsIdentity> IAuthenticator.IssueAsync(object token, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation) => this.IssueAsync((Token)token, scenario, parameters);
		public ValueTask<ClaimsIdentity> IssueAsync(Token token, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(token.Identifier))
				return ValueTask.FromResult<ClaimsIdentity>(null);

			return this.OnIssueAsync(token, scenario, parameters, cancellation);
		}

		protected abstract ValueTask<ClaimsIdentity> OnIssueAsync(Token token, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation);
		#endregion

		#region 私有方法
		private static async ValueTask<OperationResult<Token>> GetChannelToken(Channel channel, string key, string code, CancellationToken cancellation = default)
		{
			var result = await channel.Authentication.AuthenticateAsync(code, cancellation);

			if(result.Failed)
				return result.Failure;

			var identifier = result.Value.Identifier;
			var info = await channel.Users.GetInfoAsync(identifier);

			if(info.Succeed)
				return string.IsNullOrEmpty(info.Value.UnionId) ?
					OperationResult.Success(new Token(channel.Account, key, identifier, info.Value.Nickname, info.Value.Avatar, info.Value.Description)) :
					OperationResult.Success(new Token(channel.Account, key, identifier, info.Value.UnionId, info.Value.Nickname, info.Value.Avatar, info.Value.Description));

			return OperationResult.Success(new Token(channel.Account, key, identifier));
		}
		#endregion

		#region 嵌套结构
		public struct Ticket
		{
			public Ticket(string scheme, string token)
			{
				this.Scheme = scheme;
				this.Token = token;
			}

			public string Scheme;
			public string Token;

			public bool IsEmpty { get => string.IsNullOrWhiteSpace(this.Token); }

			public static Ticket GetTicket(object data)
			{
				var text = data as string;

				if(text == null)
				{
					if(data is byte[] bytes)
						text = System.Text.Encoding.UTF8.GetString(bytes);
					else if(data is System.IO.Stream stream)
					{
						using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
						text = reader.ReadToEnd();
					}
				}

				if(string.IsNullOrEmpty(text))
					throw new InvalidOperationException($"The identity verification data type '{data.GetType().FullName}' is not supported.");

				var index = text.IndexOf(':');

				if(index > 0 && index < text.Length - 1)
					return new Ticket(text.Substring(0, index), text.Substring(index + 1));

				return new Ticket(null, text);
			}
		}

		public struct Token
		{
			public Token(in Account account, string key, string identifier)
			{
				this.Account = account;
				this.Key = key;
				this.Identifier = identifier;
				this.UnionId = null;
				this.Nickname = null;
				this.Avatar = null;
				this.Description = null;
			}

			public Token(in Account account, string key, string identifier, string nickname, string avatar, string description = null)
			{
				this.Account = account;
				this.Key = key;
				this.Identifier = identifier;
				this.UnionId = null;
				this.Nickname = nickname;
				this.Avatar = avatar;
				this.Description = description;
			}

			public Token(in Account account, string key, string identifier, string unionId, string nickname, string avatar, string description = null)
			{
				this.Account = account;
				this.Key = key;
				this.Identifier = identifier;
				this.UnionId = unionId;
				this.Nickname = nickname;
				this.Avatar = avatar;
				this.Description = description;
			}

			public readonly string Key;
			public readonly Account Account;
			public readonly string Identifier;
			public readonly string UnionId;
			public readonly string Nickname;
			public readonly string Avatar;
			public readonly string Description;
		}
		#endregion
	}
}
