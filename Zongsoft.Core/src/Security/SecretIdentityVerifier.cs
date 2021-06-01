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

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security
{
	[Service(typeof(IIdentityVerifier), typeof(IIdentityVerifier<string>))]
	public class SecretIdentityVerifier : IIdentityVerifier<string>
	{
		#region 构造函数
		public SecretIdentityVerifier() { }
		#endregion

		#region 公共属性
		public string Name => "Secret";

		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		[ServiceDependency]
		public ISecretor Secretor { get; set; }
		#endregion

		#region 校验方法
		OperationResult IIdentityVerifier.Verify(string token, object data, out object ticket)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			return this.Verify(token, (string)(ticket = SecretIdentityUtility.GetTicket(data)));
		}

		public OperationResult Verify(string token, string data)
		{
			var index = token.IndexOf(':');

			if(index <= 0 || index >= token.Length - 1)
				return OperationResult.Fail(SecurityReasons.InvalidArgument, $"Illegal identity verification token format.");

			var key = token.Substring(0, index);
			var secret = token.Substring(index + 1);

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

			return string.IsNullOrEmpty(extra) || string.IsNullOrEmpty(data) || string.Equals(extra, data, StringComparison.OrdinalIgnoreCase) ?
				OperationResult.Success() :
				OperationResult.Fail(SecurityReasons.VerifyFaild, $"Identity verification data is inconsistent.");
		}
		#endregion
	}
}