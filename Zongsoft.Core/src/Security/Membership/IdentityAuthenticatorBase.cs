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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Serialization;

namespace Zongsoft.Security.Membership
{
	public abstract class IdentityAuthenticatorBase : IAuthenticator<IdentityAuthenticatorBase.Ticket, IIdentityTicket>
	{
		#region 构造函数
		protected IdentityAuthenticatorBase() => this.Name = string.Empty;
		#endregion

		#region 公共属性
		public string Name { get; }

		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		[ServiceDependency("~", IsRequired = true)]
		public IDataAccess DataAccess { get; set; }
		#endregion

		#region 校验方法
		async ValueTask<object> IAuthenticator.VerifyAsync(string key, object data, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			return await this.VerifyAsync(key, GetTicket(data), scenario, parameters, cancellation);
		}

		public ValueTask<IIdentityTicket> VerifyAsync(string key, Ticket data, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrWhiteSpace(data.Identity))
			{
				if(string.IsNullOrEmpty(key))
					throw new AuthenticationException(SecurityReasons.InvalidIdentity, "Missing identity.");

				data.Identity = key;
			}

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则返回账号被禁用
			if(attempter != null && !attempter.Verify(data.Identity, data.Namespace))
				throw new AuthenticationException(SecurityReasons.AccountSuspended);

			//获取当前用户的密码及密码盐
			var userId = this.GetPassword(data.Identity, data.Namespace, out var storedPassword, out var storedPasswordSalt, out var status, out _);

			//如果帐户不存在则返回无效账户
			if(userId == 0)
				throw new AuthenticationException(SecurityReasons.InvalidIdentity);

			//如果账户状态异常则返回账户状态异常
			if(status != UserStatus.Active)
				throw new AuthenticationException(SecurityReasons.AccountDisabled);

			//密码校验失败则返回密码验证失败
			if(!PasswordUtility.VerifyPassword(data.Password, storedPassword, storedPasswordSalt, "SHA1"))
			{
				//通知验证尝试失败
				attempter?.Fail(data.Identity, data.Namespace);

				throw new AuthenticationException(SecurityReasons.InvalidPassword);
			}

			//通知验证尝试成功，即清空验证失败记录
			attempter?.Done(data.Identity, data.Namespace);

			return ValueTask.FromResult<IIdentityTicket>(new Ticket(data.Namespace, data.Identity));
		}
		#endregion

		#region 身份签发
		ValueTask<ClaimsIdentity> IAuthenticator.IssueAsync(object data, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation)
		{
			return this.IssueAsync(data as IIdentityTicket, scenario, parameters, cancellation);
		}

		public ValueTask<ClaimsIdentity> IssueAsync(IIdentityTicket data, string scenario, IDictionary<string, object> parameters, CancellationToken cancellation = default)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			//从数据库中获取指定身份的用户对象
			var user = this.GetUser(data);

			if(user == null)
				return ValueTask.FromResult<ClaimsIdentity>(null);

			return ValueTask.FromResult(this.Identity(user, scenario));
		}
		#endregion

		#region 虚拟方法
		protected abstract uint GetPassword(string identity, string @namespace, out byte[] password, out long passwordSalt, out UserStatus status, out DateTime? statusTimestamp);
		protected abstract IUserModel GetUser(IIdentityTicket ticket);
		protected virtual TimeSpan GetPeriod(string scenario) => TimeSpan.FromHours(4);
		protected virtual ClaimsIdentity Identity(IUserModel user, string scenario) => user.Identity(this.Name, this.Name, this.GetPeriod(scenario));
		#endregion

		#region 私有方法
		private static Ticket GetTicket(object data)
		{
			return data switch
			{
				string text => Serializer.Json.Deserialize<Ticket>(text),
				byte[] array => Serializer.Json.Deserialize<Ticket>(array),
				Stream stream => Serializer.Json.Deserialize<Ticket>(stream),
				Memory<byte> memory => Serializer.Json.Deserialize<Ticket>(memory.Span),
				ReadOnlyMemory<byte> memory => Serializer.Json.Deserialize<Ticket>(memory.Span),
				_ => throw new InvalidOperationException($"The identity verification data type '{data.GetType().FullName}' is not supported."),
			};
		}
		#endregion

		#region 嵌套结构
		public struct Ticket : IIdentityTicket
		{
			public Ticket(string @namespace, string identity, string password = null)
			{
				this.Namespace = @namespace;
				this.Identity = identity;
				this.Password = password;
			}

			public string Namespace { get; set; }
			public string Identity { get; set; }
			public string Password { get; set; }
		}
		#endregion
	}
}
