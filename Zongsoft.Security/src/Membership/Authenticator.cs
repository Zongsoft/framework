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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public class Authenticator : IAuthenticator
	{
		#region 常量定义
		private const string KEY_AUTHENTICATION_SECRET = "Zongsoft.Security.Authentication";
		private const string KEY_AUTHENTICATION_TEMPLATE = "Authentication";
		#endregion

		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 事件声明
		public event EventHandler<AuthenticationContext> Authenticated;
		public event EventHandler<AuthenticationContext> Authenticating;
		#endregion

		#region 构造函数
		public Authenticator()
		{
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get => "Normal";
		}

		public Options.ICredentialOption Option
		{
			get; set;
		}

		[ServiceDependency]
		public Attempter Attempter
		{
			get; set;
		}

		[ServiceDependency]
		public ISecretProvider Secretor
		{
			get; set;
		}

		[ServiceDependency]
		public IDataAccess DataAccess
		{
			get
			{
				return _dataAccess;
			}
			set
			{
				_dataAccess = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 验证方法
		public IUserIdentity Authenticate(string identity, string password, string @namespace, string scene, ref IDictionary<string, object> parameters)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			//创建验证上下文对象
			var context = new AuthenticationContext(this, identity, @namespace, scene, parameters);

			//激发“Authenticating”事件
			this.OnAuthenticating(context);

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则抛出账号被禁用的异常
			if(attempter != null && !attempter.Verify(identity, @namespace))
			{
				//设置当前上下文的异常
				context.Exception = new AuthenticationException(AuthenticationReason.AccountSuspended);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				//抛出异常
				if(context.Exception != null)
					throw context.Exception;

				return context.User;
			}

			//获取当前用户的密码及密码向量
			var userId = this.GetPassword(identity, @namespace, out var storedPassword, out var storedPasswordSalt, out var status, out var statusTimestamp);

			//如果帐户不存在，则抛出异常
			if(userId == 0)
			{
				//设置当前上下文的异常
				context.Exception = new AuthenticationException(AuthenticationReason.InvalidIdentity);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				//抛出异常
				if(context.Exception != null)
					throw context.Exception;

				return null;
			}

			switch(status)
			{
				case UserStatus.Unapproved:
					//因为账户状态异常而抛出验证异常
					context.Exception = new AuthenticationException(AuthenticationReason.AccountUnapproved);

					//激发“Authenticated”事件
					this.OnAuthenticated(context);

					if(context.Exception != null)
						throw context.Exception;

					return context.User;
				case UserStatus.Disabled:
					//因为账户状态异常而抛出验证异常
					context.Exception = new AuthenticationException(AuthenticationReason.AccountDisabled);

					//激发“Authenticated”事件
					this.OnAuthenticated(context);

					if(context.Exception != null)
						throw context.Exception;

					return context.User;
			}

			//如果验证失败，则抛出异常
			if(!PasswordUtility.VerifyPassword(password, storedPassword, storedPasswordSalt, "SHA1"))
			{
				//通知验证尝试失败
				if(attempter != null)
					attempter.Fail(identity, @namespace);

				//密码校验失败则抛出验证异常
				context.Exception = new AuthenticationException(AuthenticationReason.InvalidPassword);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				if(context.Exception != null)
					throw context.Exception;

				return context.User;
			}

			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				attempter.Done(identity, @namespace);

			//获取指定用户编号对应的用户对象
			context.User = this.DataAccess.Select<IUser>(Condition.Equal(nameof(IUser.UserId), userId)).FirstOrDefault();

			//设置凭证有效期的配置策略
			if(this.Option != null)
				context.Parameters["Credential:Option"] = this.Option;

			//激发“Authenticated”事件
			this.OnAuthenticated(context);

			if(context.HasParameters)
				parameters = context.Parameters;

			//返回成功的验证结果
			return context.User;
		}

		public IUserIdentity AuthenticateSecret(string identity, string secret, string @namespace, string scene, ref IDictionary<string, object> parameters)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			//创建验证上下文对象
			var context = new AuthenticationContext(this, identity, @namespace, scene, parameters);

			//激发“Authenticating”事件
			this.OnAuthenticating(context);

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则抛出账号被禁用的异常
			if(attempter != null && !attempter.Verify(identity, @namespace))
			{
				//设置当前上下文的异常
				context.Exception = new AuthenticationException(AuthenticationReason.AccountSuspended);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				//抛出异常
				if(context.Exception != null)
					throw context.Exception;

				return context.User;
			}

			//获取指定标识的用户对象
			var user = this.DataAccess.Select<IUser>(MembershipHelper.GetUserIdentity(identity, out var identityType) & this.GetNamespace(@namespace)).FirstOrDefault();

			//如果帐户不存在，则抛出异常
			if(user == null)
			{
				//设置当前上下文的异常
				context.Exception = new AuthenticationException(AuthenticationReason.InvalidIdentity);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				//抛出异常
				if(context.Exception != null)
					throw context.Exception;

				return null;
			}

			switch(user.Status)
			{
				case UserStatus.Unapproved:
					//因为账户状态异常而抛出验证异常
					context.Exception = new AuthenticationException(AuthenticationReason.AccountUnapproved);

					//激发“Authenticated”事件
					this.OnAuthenticated(context);

					if(context.Exception != null)
						throw context.Exception;

					return context.User;
				case UserStatus.Disabled:
					//因为账户状态异常而抛出验证异常
					context.Exception = new AuthenticationException(AuthenticationReason.AccountDisabled);

					//激发“Authenticated”事件
					this.OnAuthenticated(context);

					if(context.Exception != null)
						throw context.Exception;

					return context.User;
			}

			//如果验证失败，则抛出异常
			if(!this.Secretor.Verify(GetSecretKey(identity, @namespace), secret))
			{
				//通知验证尝试失败
				if(attempter != null)
					attempter.Fail(identity, @namespace);

				//密码校验失败则抛出验证异常
				context.Exception = new AuthenticationException(AuthenticationReason.InvalidPassword);

				//激发“Authenticated”事件
				this.OnAuthenticated(context);

				if(context.Exception != null)
					throw context.Exception;

				return context.User;
			}

			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				attempter.Done(identity, @namespace);

			//更新上下文的用户对象
			context.User = user;

			//设置凭证有效期的配置策略
			if(this.Option != null)
				context.Parameters["Credential:Option"] = this.Option;

			//激发“Authenticated”事件
			this.OnAuthenticated(context);

			if(context.HasParameters)
				parameters = context.Parameters;

			//返回成功的验证结果
			return context.User;
		}
		#endregion

		#region 获取秘密
		public void Secret(string identity, string @namespace = null)
		{
			var secretor = this.Secretor ?? throw new InvalidOperationException($"Missing a required secret provider.");
			var secret = secretor.Generate(GetSecretKey(identity, @namespace));

			switch(MembershipHelper.GetIdentityType(identity))
			{
				case UserIdentityType.Email:
					CommandExecutor.Default.Execute($"email.send -template:{KEY_AUTHENTICATION_TEMPLATE} {identity}", new
					{
						Code = secret,
					});
					break;
				case UserIdentityType.Phone:
					CommandExecutor.Default.Execute($"phone.send -template:{KEY_AUTHENTICATION_TEMPLATE} {identity}", new
					{
						Code = secret,
					});
					break;
				default:
					throw new ArgumentException($"Invalid secret code identity.");
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual uint GetPassword(string identity, string @namespace, out byte[] password, out long passwordSalt, out UserStatus status, out DateTime? statusTimestamp)
		{
			if(string.IsNullOrWhiteSpace(@namespace))
				@namespace = null;

			var entity = this.DataAccess.Select<UserSecret>(MembershipHelper.GetUserIdentity(identity) & Condition.Equal(nameof(IUser.Namespace), @namespace)).FirstOrDefault();

			if(entity.UserId == 0)
			{
				password = null;
				passwordSalt = 0;
				status = UserStatus.Active;
				statusTimestamp = null;
			}
			else
			{
				password = entity.Password;
				passwordSalt = entity.PasswordSalt;
				status = entity.Status;
				statusTimestamp = entity.StatusTimestamp;
			}

			return entity.UserId;
		}
		#endregion

		#region 激发事件
		protected virtual void OnAuthenticated(AuthenticationContext context)
		{
			this.Authenticated?.Invoke(this, context);
		}

		protected virtual void OnAuthenticating(AuthenticationContext context)
		{
			this.Authenticating?.Invoke(this, context);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetSecretKey(string identity, string @namespace)
		{
			return KEY_AUTHENTICATION_SECRET + ":" +
				(
					string.IsNullOrWhiteSpace(@namespace) ?
					identity.Trim() :
					identity.Trim() + "!" + @namespace.Trim()
				).ToLowerInvariant();
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Condition GetNamespace(string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace))
				return Condition.Equal(nameof(IUser.Namespace), null);
			else if(@namespace != "*")
				return Condition.Equal(nameof(IUser.Namespace), @namespace);

			return null;
		}
		#endregion

		#region 嵌套结构
		[Zongsoft.Data.Model("Security.User")]
		private struct UserSecret
		{
			public uint UserId;
			public byte[] Password;
			public long PasswordSalt;
			public UserStatus Status;
			public DateTime? StatusTimestamp;
		}
		#endregion
	}
}
