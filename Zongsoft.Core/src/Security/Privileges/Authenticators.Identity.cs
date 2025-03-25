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

partial class Authentication
{
	public abstract class IdentityAuthenticatorBase : IAuthenticator<IdentityAuthenticatorBase.Requirement, IdentityAuthenticatorBase.Ticket>
	{
		#region 成员字段
		private IAttempter _attempter;
		#endregion

		#region 构造函数
		protected IdentityAuthenticatorBase(IServiceProvider services, IAttempter attempter = null) : this(null, services, attempter) { }
		protected IdentityAuthenticatorBase(string name, IServiceProvider services, IAttempter attempter = null)
		{
			this.Services = services ?? throw new ArgumentNullException(nameof(services));
			this.Name = name ?? string.Empty;
			this.Attempter = _attempter;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IAttempter Attempter
		{
			get => _attempter ?? Authentication.Attempter;
			set => _attempter = value;
		}
		#endregion

		#region 保护属性
		protected IServiceProvider Services { get; }
		protected abstract IDataAccess Accessor { get; }
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
			var attempterKey = $"{this.GetType().Name}:{requirement.Identity}@{requirement.Namespace}";

			//确认验证失败是否超出限制数，如果超出则返回账号被禁用
			if(attempter != null && !attempter.Verify(attempterKey))
				throw new AuthenticationException(SecurityReasons.AccountSuspended);

			//获取当前用户的密钥信息
			var cipher = await Authentication.Servicer.Users.Passworder.GetAsync(requirement.Identity, requirement.Namespace, cancellation);

			//如果帐户不存在则验证失败
			if(cipher == null)
				throw new AuthenticationException(SecurityReasons.InvalidIdentity);

			//执行密码验证，如果成功则返回验证成功的票证
			if(await Authentication.Servicer.Users.Passworder.VerifyAsync(requirement.Password, cipher, cancellation))
			{
				//通知验证尝试成功，即清空验证失败记录
				attempter?.Done(attempterKey);

				//返回验证成功的票证
				return this.CreateTicket(cipher.Identifier, requirement);
			}

			//通知验证尝试失败
			attempter?.Fail(attempterKey);
			//抛出验证失败异常
			throw new AuthenticationException(SecurityReasons.InvalidPassword);
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
		protected virtual Ticket CreateTicket(Identifier identifier, Requirement requirement) => new(identifier, requirement.Identity, requirement.Namespace);
		protected virtual ValueTask<IUser> GetUserAsync(Ticket ticket, CancellationToken cancellation) => Authentication.Servicer.Users.GetAsync(ticket.Identifier, cancellation);
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
		#endregion
	}
}