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
		protected IdentityAuthenticatorBase() { }
		#endregion

		#region 公共属性
		public string Name { get; }

		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		[ServiceDependency(IsRequired = true)]
		public IServiceAccessor<IDataAccess> DataAccess { get; set; }

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 校验方法
		OperationResult IAuthenticator.Verify(string key, object data)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			return this.Verify(key, GetTicket(data));
		}

		public OperationResult<IIdentityTicket> Verify(string key, Ticket data)
		{
			if(string.IsNullOrWhiteSpace(data.Identity))
			{
				if(string.IsNullOrEmpty(key))
					return OperationResult.Fail(SecurityReasons.InvalidIdentity, "Missing identity.");

				data.Identity = key;
			}

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则返回账号被禁用
			if(attempter != null && !attempter.Verify(data.Identity, data.Namespace))
				return OperationResult.Fail(SecurityReasons.AccountSuspended);

			//获取当前用户的密码及密码盐
			var userId = this.GetPassword(data.Identity, data.Namespace, out var storedPassword, out var storedPasswordSalt, out var status, out _);

			//如果帐户不存在则返回无效账户
			if(userId == 0)
				return OperationResult.Fail(SecurityReasons.InvalidIdentity);

			//如果账户状态异常则返回账户状态异常
			if(status != UserStatus.Active)
				return OperationResult.Fail(SecurityReasons.AccountDisabled);

			//密码校验失败则返回密码验证失败
			if(!PasswordUtility.VerifyPassword(data.Password, storedPassword, storedPasswordSalt, "SHA1"))
			{
				//通知验证尝试失败
				if(attempter != null)
					attempter.Fail(data.Identity, data.Namespace);

				return OperationResult.Fail(SecurityReasons.InvalidPassword);
			}

			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				attempter.Done(data.Identity, data.Namespace);

			return OperationResult.Success<IIdentityTicket>(new Ticket(data.Namespace, data.Identity));
		}
		#endregion

		#region 身份签发
		ClaimsIdentity IAuthenticator.Issue(object data, TimeSpan period, IDictionary<string, object> parameters)
		{
			return this.Issue(data as IIdentityTicket, period, parameters);
		}

		public ClaimsIdentity Issue(IIdentityTicket data, TimeSpan period, IDictionary<string, object> parameters)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			//从数据库中获取指定身份的用户对象
			var user = this.GetUser(data);

			if(user == null)
				return null;

			return this.Identity(user, period);
		}
		#endregion

		#region 虚拟方法
		protected abstract uint GetPassword(string identity, string @namespace, out byte[] password, out long passwordSalt, out UserStatus status, out DateTime? statusTimestamp);
		protected abstract IUser GetUser(IIdentityTicket ticket);
		protected virtual ClaimsIdentity Identity(IUser user, TimeSpan period) => user.Identity(this.Name, this.Name, period);
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
