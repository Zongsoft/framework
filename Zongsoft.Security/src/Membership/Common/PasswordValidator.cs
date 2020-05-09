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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Collections;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership.Common
{
	/// <summary>
	/// 提供密码有效性验证的验证器类。
	/// </summary>
	[Service(Modules.Security, typeof(IValidator<string>))]
	public class PasswordValidator : IValidator<string>, IMatchable<string>
	{
		#region 常量定义
		private const int PASSWORD_STRENGTH_DIGIT = 1;
		private const int PASSWORD_STRENGTH_LOWERCASE = 2;
		private const int PASSWORD_STRENGTH_UPPERCASE = 4;
		private const int PASSWORD_STRENGTH_SYMBOL = 8;

		private const int PASSWORD_STRENGTH_LETTER = PASSWORD_STRENGTH_LOWERCASE | PASSWORD_STRENGTH_UPPERCASE;
		private const int PASSWORD_STRENGTH_LETTER_DIGIT = PASSWORD_STRENGTH_DIGIT | PASSWORD_STRENGTH_LOWERCASE | PASSWORD_STRENGTH_UPPERCASE;
		#endregion

		#region 验证方法
		public bool Validate(string data, object parameter, Action<string> failure = null)
		{
			var options = parameter as Configuration.IdentityOptions;

			//如果没有设置密码验证策略，则返回验证成功
			if(options == null || options.PasswordLength < 1)
				return true;

			//如果如果密码长度小于配置要求的长度，则返回验证失败
			if(string.IsNullOrEmpty(data) || data.Length < options.PasswordLength)
			{
				failure?.Invoke($"The password length must be no less than {options.PasswordLength} characters.");
				return false;
			}

			bool isValidate;

			switch(options.PasswordStrength)
			{
				case PasswordStrength.Digits:
					isValidate = this.GetStrength(data) == PASSWORD_STRENGTH_DIGIT;

					if(!isValidate)
					{
						failure?.Invoke("The password must be digits.");
						return false;
					}

					break;
				case PasswordStrength.Lowest:
					isValidate = data != null && data.Length > 0;

					if(!isValidate)
					{
						failure?.Invoke("The password cannot be empty.");
						return false;
					}

					break;
				case PasswordStrength.Normal:
					var strength = this.GetStrength(data);

					isValidate = strength == (PASSWORD_STRENGTH_DIGIT + PASSWORD_STRENGTH_LOWERCASE) ||
					             strength == (PASSWORD_STRENGTH_DIGIT + PASSWORD_STRENGTH_UPPERCASE);

					if(!isValidate)
					{
						failure?.Invoke("The password is not strong enough. It must contain digits and letters.");
						return false;
					}

					break;
				case PasswordStrength.Highest:
					isValidate = this.GetStrength(data) == PASSWORD_STRENGTH_DIGIT +
					                                       PASSWORD_STRENGTH_SYMBOL +
					                                       PASSWORD_STRENGTH_LOWERCASE +
					                                       PASSWORD_STRENGTH_UPPERCASE;

					if(!isValidate)
					{
						failure?.Invoke("The password is not strong enough. It must contain digits, uppercase and lowercase letters, and symbols.");
						return false;
					}

					break;
			}

			//返回密码有效性验证成功
			return true;
		}

		public Task<bool> ValidateAsync(string data, object parameter, Action<string> failure = null, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Validate(data, parameter, failure));
		}
		#endregion

		#region 匹配方法
		public bool Match(string parameter)
		{
			return string.Equals(parameter, "Password", StringComparison.OrdinalIgnoreCase) |
				   string.Equals(parameter, "User.Password", StringComparison.OrdinalIgnoreCase);
		}

		bool IMatchable.Match(object parameter)
		{
			return this.Match(parameter as string);
		}
		#endregion

		#region 私有方法
		private int GetStrength(string data)
		{
			int flag = 0;

			for(int i = 0; i < data.Length; i++)
			{
				var chr = data[i];

				if(chr >= '0' && chr <= '9')
					flag |= PASSWORD_STRENGTH_DIGIT;
				else if(chr >= 'a' && chr <= 'z')
					flag |= PASSWORD_STRENGTH_LOWERCASE;
				else if(chr >= 'A' && chr <= 'Z')
					flag |= PASSWORD_STRENGTH_UPPERCASE;
				else
					flag |= PASSWORD_STRENGTH_SYMBOL;
			}

			return flag;
		}
		#endregion
	}
}
