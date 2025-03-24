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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Serialization;

namespace Zongsoft.Security.Privileges;

public abstract class IdentityAuthenticatorBase : IAuthenticator<IdentityAuthenticatorBase.Requirement, IdentityAuthenticatorBase.Ticket>
{
	#region 构造函数
	protected IdentityAuthenticatorBase() => this.Name = string.Empty;
	#endregion

	#region 公共属性
	public string Name { get; }

	[ServiceDependency]
	public IAttempter Attempter { get; set; }
	#endregion

	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 校验方法
	async ValueTask<object> IAuthenticator.VerifyAsync(string key, object requirement, string scenario, Parameters parameters, CancellationToken cancellation)
	{
		if(requirement == null)
			throw new ArgumentNullException(nameof(requirement));

		return await this.VerifyAsync(key, await GetTicketAsync(requirement, cancellation), scenario, parameters, cancellation);
	}

	public async ValueTask<Ticket> VerifyAsync(string key, Requirement requirement, string scenario, Parameters parameters, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(requirement.Identity))
		{
			if(string.IsNullOrEmpty(key))
				throw new AuthenticationException(SecurityReasons.InvalidIdentity, "Missing identity.");

			requirement.Identity = key;
		}

		//获取验证失败的解决器
		var attempter = this.Attempter;

		//确认验证失败是否超出限制数，如果超出则返回账号被禁用
		if(attempter != null && !attempter.Verify(requirement.Identity, requirement.Namespace))
			throw new AuthenticationException(SecurityReasons.AccountSuspended);

		//获取当前用户的密码及相关密钥信息
		var secret = await this.GetSecretAsync(requirement.Identity, requirement.Namespace, cancellation);

		//如果帐户不存在则返回无效账户
		if(secret == null || secret.Identifier.IsEmpty)
			throw new AuthenticationException(SecurityReasons.InvalidIdentity);

		//如果账户状态异常则返回账户状态异常
		//if(status != UserStatus.Active)
		//	throw new AuthenticationException(SecurityReasons.AccountDisabled);

		//密码校验失败则返回密码验证失败
		if(!await this.VerifySecretAsync(requirement, secret, cancellation))
		{
			//通知验证尝试失败
			attempter?.Fail(requirement.Identity, requirement.Namespace);
			throw new AuthenticationException(SecurityReasons.InvalidPassword);
		}

		//通知验证尝试成功，即清空验证失败记录
		attempter?.Done(requirement.Identity, requirement.Namespace);

		//返回验证成功的票证
		return this.CreateTicket(secret.Identifier, requirement);
	}
	#endregion

	#region 身份签发
	ValueTask<ClaimsIdentity> IAuthenticator.IssueAsync(object ticket, string scenario, Parameters parameters, CancellationToken cancellation)
	{
		return this.IssueAsync(ticket is Ticket t ? t : default, scenario, parameters, cancellation);
	}

	public async ValueTask<ClaimsIdentity> IssueAsync(Ticket ticket, string scenario, Parameters parameters, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(ticket.Identity))
			throw new ArgumentNullException(nameof(ticket));

		//从数据库中获取指定身份的用户对象
		var user = await this.GetUserAsync(ticket, cancellation);

		if(user == null)
			return null;

		return this.Identity(user, scenario);
	}
	#endregion

	#region 虚拟方法
	protected abstract ValueTask<Secret> GetSecretAsync(string identity, string @namespace, CancellationToken cancellation);
	protected virtual ValueTask<bool> VerifySecretAsync(Requirement requirement, Secret secret, CancellationToken cancellation)
	{
		return ValueTask.FromResult(PasswordUtility.VerifyPassword(requirement.Password, secret.Password, secret.PasswordSalt, "SHA1"));
	}

	protected virtual Ticket CreateTicket(Identifier identifier, Requirement requirement) => new(identifier, requirement.Identity, requirement.Namespace);
	protected abstract ValueTask<IUser> GetUserAsync(Ticket ticket, CancellationToken cancellation);
	protected virtual TimeSpan GetPeriod(string scenario) => TimeSpan.FromHours(4);
	protected virtual ClaimsIdentity Identity(IUser user, string scenario) => user.Identity(this.Name, this.Name, this.GetPeriod(scenario));
	#endregion

	#region 私有方法
	private static async ValueTask<Requirement> GetTicketAsync(object data, CancellationToken cancellation)
	{
		return data switch
		{
			string text => await Serializer.Json.DeserializeAsync<Requirement>(text, cancellation),
			byte[] array => await Serializer.Json.DeserializeAsync<Requirement>(array, cancellation),
			Stream stream => await Serializer.Json.DeserializeAsync<Requirement>(stream, cancellation),
			Memory<byte> memory => await Serializer.Json.DeserializeAsync<Requirement>(memory.Span, cancellation),
			ReadOnlyMemory<byte> memory => await Serializer.Json.DeserializeAsync<Requirement>(memory.Span, cancellation),
			_ => throw new InvalidOperationException($"The identity verification data type '{data.GetType().FullName}' is not supported."),
		};
	}
	#endregion

	#region 嵌套结构
	public struct Requirement
	{
		public Requirement(string @namespace, string identity, string password = null)
		{
			this.Namespace = @namespace;
			this.Identity = identity;
			this.Password = password;
		}

		public string Namespace { get; set; }
		public string Identity { get; set; }
		public string Password { get; set; }
	}

	public struct Ticket
	{
		public Ticket(Identifier identifier, string identity, string @namespace)
		{
			this.Identifier = identifier;
			this.Identity = identity;
			this.Namespace = @namespace;
		}

		public Identifier Identifier { get; set; }
		public string Identity { get; set; }
		public string Namespace { get; set; }
	}

	protected class Secret
	{
		protected Secret() { }
		protected Secret(Identifier identifier, byte[] password, long passwordSalt)
		{
			this.Identifier = identifier;
			this.Password = password;
			this.PasswordSalt = passwordSalt;
		}

		public virtual Identifier Identifier { get; }
		public byte[] Password { get; set; }
		public long PasswordSalt { get; set; }
	}
	#endregion
}
