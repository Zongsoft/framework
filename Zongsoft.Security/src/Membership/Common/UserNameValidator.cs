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

namespace Zongsoft.Security.Membership.Common
{
	[Service(typeof(IValidator<string>))]
	public class UserNameValidator : IValidator<string>, IMatchable
	{
		#region 验证方法
		public bool Validate(string data, object parameter, Action<string> failure = null)
		{
			if(string.IsNullOrEmpty(data))
			{
				failure?.Invoke("The name is null or empty.");
				return false;
			}

			//名字(用户名或角色名)的长度必须不少于4个字符
			if(data.Length < 4)
			{
				failure?.Invoke($"The '{data}' name length must be greater than 3.");
				return false;
			}

			//名字(用户名或角色名)的首字符必须是字母、下划线、美元符
			if(!(Char.IsLetter(data[0]) || data[0] == '_' || data[0] == '$'))
			{
				failure?.Invoke($"The '{data}' name contains illegal characters.");
				return false;
			}

			//检查名字(用户名或角色名)的其余字符的合法性
			for(int i = 1; i < data.Length; i++)
			{
				//名字的中间字符必须是字母、数字或下划线
				if(!Char.IsLetterOrDigit(data[i]) && data[i] != '_')
				{
					failure?.Invoke($"The '{data}' name contains illegal characters.");
					return false;
				}
			}

			//通过所有检测，返回成功
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
			return string.Equals(parameter, "Name", StringComparison.OrdinalIgnoreCase) |
			       string.Equals(parameter, "UserName", StringComparison.OrdinalIgnoreCase) |
			       string.Equals(parameter, "User.Name", StringComparison.OrdinalIgnoreCase);
		}

		bool IMatchable.Match(object parameter)
		{
			return this.Match(parameter as string);
		}
		#endregion
	}
}
