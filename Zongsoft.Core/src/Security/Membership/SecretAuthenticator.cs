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
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public class SecretAuthenticator : IAuthenticator<string, string>
	{
		#region 公共属性
		public string Name => "Secret";

		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		[ServiceDependency]
		public ISecretor Secretor { get; set; }
		#endregion

		#region 校验方法
		OperationResult IAuthenticator.Verify(string key, object data)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			return this.Verify(key, SecretIdentityUtility.GetTicket(data));
		}

		public OperationResult<string> Verify(string key, string data)
		{
			if(string.IsNullOrEmpty(data))
				return OperationResult.Fail("InvalidToken");

			var index = data.IndexOfAny(new[] { ':', '=' });

			if(index <= 0 || index >= data.Length - 1)
				return OperationResult.Fail(SecurityReasons.InvalidArgument, $"Illegal identity verification token format.");

			var phone = data.Substring(0, index);
			var secret = data.Substring(index + 1);

			//获取验证失败的解决器
			var attempter = this.Attempter;

			//确认验证失败是否超出限制数，如果超出则返回账号被禁用
			if(attempter != null && !attempter.Verify(key))
				return OperationResult.Fail(SecurityReasons.AccountSuspended);

			if(!this.Secretor.Verify(key, secret, out var extra))
			{
				//通知验证尝试失败
				if(attempter != null)
					attempter.Fail(key);

				return OperationResult.Fail(SecurityReasons.VerifyFaild);
			}

			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				attempter.Done(key);

			return string.IsNullOrEmpty(extra) || string.IsNullOrEmpty(phone) || string.Equals(extra, phone, StringComparison.OrdinalIgnoreCase) ?
				OperationResult.Success(phone) :
				OperationResult.Fail(SecurityReasons.VerifyFaild, $"Identity verification data is inconsistent.");
		}
		#endregion

		#region 身份签发
		ClaimsIdentity IAuthenticator.Issue(object token, TimeSpan period, IDictionary<string, object> parameters)
		{
			return token == null ? null : this.Issue(token.ToString(), period, parameters);
		}

		public ClaimsIdentity Issue(string token, TimeSpan period, IDictionary<string, object> parameters)
		{
			if(string.IsNullOrEmpty(token))
				return null;

			var identity = new ClaimsIdentity(this.Name);

			if(token.Contains('@'))
				identity.AddClaim(ClaimTypes.Email, token, ClaimValueTypes.Email, this.Name);
			else
				identity.AddClaim(ClaimTypes.MobilePhone, token, ClaimValueTypes.String, this.Name);

			if(period > TimeSpan.Zero)
				identity.AddClaim(new Claim(ClaimTypes.Expiration, period.ToString(), period.TotalHours > 24 ? ClaimValueTypes.YearMonthDuration : ClaimValueTypes.DaytimeDuration, this.Name, this.Name, identity));

			if(parameters != null && parameters.Count > 0)
			{
				foreach(var parameter in parameters)
				{
					if(parameter.Value != null)
						identity.AddClaim(new Claim(ClaimTypes.UserData + "#" + parameter.Key, parameter.Value.ToString(), ClaimValueTypes.String));
				}
			}

			return identity;
		}
		#endregion
	}
}
