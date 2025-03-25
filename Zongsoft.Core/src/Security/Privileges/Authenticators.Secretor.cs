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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

partial class Authentication
{
	public abstract class SecretorAuthenticatorBase : IAuthenticator<string, string>
	{
		#region 成员字段
		private ISecretor _secretor;
		private IAttempter _attempter;
		#endregion

		#region 构造函数
		protected SecretorAuthenticatorBase(IServiceProvider services, ISecretor secretor = null, IAttempter attempter = null) : this(null, services, secretor, attempter) { }
		protected SecretorAuthenticatorBase(string name, IServiceProvider services, ISecretor secretor = null, IAttempter attempter = null)
		{
			this.Name = string.IsNullOrEmpty(name) ? "Secret" : name;
			this.Services = services ?? throw new ArgumentNullException(nameof(services));
			_secretor = secretor;
			_attempter = attempter;
		}
		#endregion

		#region 公共属性
		public string Name { get; }

		public ISecretor Secretor
		{
			get => _secretor ??= this.Services.Resolve<ISecretor>();
			set => _secretor = value;
		}

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
		async ValueTask<object> IAuthenticator.VerifyAsync(string key, object data, string scenario, Parameters parameters, CancellationToken cancellation)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			return await this.VerifyAsync(key, await GetTicketAsync(data, cancellation), scenario, parameters, cancellation);
		}

		public async ValueTask<string> VerifyAsync(string key, string data, string scenario, Parameters parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(data))
				throw new AuthenticationException(SecurityReasons.InvalidArgument, $"Missing the required authentication token.");

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则返回账号被禁用
			if(attempter != null && !await attempter.CheckAsync(key, cancellation))
				throw new AuthenticationException(SecurityReasons.AccountSuspended);

			var secret = data.Trim();

			if(this.Secretor.Verify(key, secret, out var extra))
			{
				//通知验证尝试成功，即清空验证失败记录
				if(attempter != null)
					await attempter.DoneAsync(key, cancellation);

				//返回成功
				return extra;
			}

			//通知验证尝试失败
			if(attempter != null)
				await attempter.FailAsync(key, cancellation);

			//抛出验证失败的异常
			throw new AuthenticationException(SecurityReasons.VerifyFaild);
		}
		#endregion

		#region 身份签发
		ValueTask<ClaimsIdentity> IAuthenticator.IssueAsync(object identifier, string scenario, Parameters parameters, CancellationToken cancellation)
		{
			return identifier == null ? ValueTask.FromResult<ClaimsIdentity>(null) : this.IssueAsync(identifier.ToString(), scenario, parameters, cancellation);
		}

		public async ValueTask<ClaimsIdentity> IssueAsync(string identifier, string scenario, Parameters parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				throw new ArgumentNullException(nameof(identifier));

			//从数据库中获取指定身份的用户对象
			var user = await this.GetUserAsync(identifier, cancellation);

			if(user == null)
				return null;

			return this.Identity(user, scenario);
		}
		#endregion

		#region 虚拟方法
		protected virtual TimeSpan GetPeriod(string scenario) => TimeSpan.FromHours(2);
		protected virtual ICondition GetCriteria(string identifier) => UserUtility.GetCriteria(identifier);
		protected virtual ClaimsIdentity Identity(IUser user, string scenario) => user.Identity(this.Name, this.Name, this.GetPeriod(scenario));
		protected virtual ValueTask<IUser> GetUserAsync(string identifier, CancellationToken cancellation) => Authentication.Servicer.Users.GetAsync(new Components.Identifier(typeof(IUser), identifier), cancellation);
		#endregion

		#region 私有方法
		private static async ValueTask<string> GetTicketAsync(object data, CancellationToken cancellation = default)
		{
			if(data is string text)
				return text;

			if(data is byte[] bytes)
				return Encoding.UTF8.GetString(bytes);

			if(data is Stream stream)
			{
				using var reader = new StreamReader(stream, Encoding.UTF8);

#if NET7_0_OR_GREATER
			return await reader.ReadToEndAsync(cancellation);
#else
				return await reader.ReadToEndAsync();
#endif
			}

			throw new InvalidOperationException($"The identity verification data type '{data.GetType().FullName}' is not supported.");
		}
		#endregion
	}
}