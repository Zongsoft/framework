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
using Zongsoft.Collections;

namespace Zongsoft.Security.Membership.Common
{
	public class RoleNameValidator : IValidator<string>, IMatchable<string>
	{
		#region 验证方法
		public bool Validate(string parameter, Action<string> failure = null)
		{
			if(string.IsNullOrEmpty(parameter))
			{
				failure?.Invoke("The name is null or empty.");
				return false;
			}

			//名字(用户名或角色名)的长度必须不少于4个字符
			if(parameter.Length < 4)
			{
				failure?.Invoke($"The '{parameter}' name length must be greater than 3.");
				return false;
			}

			//名字(用户名或角色名)的首字符必须是字母、下划线、美元符
			if(!(Char.IsLetter(parameter[0]) || parameter[0] == '_' || parameter[0] == '$'))
			{
				failure?.Invoke($"The '{parameter}' name contains illegal characters.");
				return false;
			}

			//检查名字(用户名或角色名)的其余字符的合法性
			for(int i = 1; i < parameter.Length; i++)
			{
				//名字的中间字符必须是字母、数字或下划线
				if(!Char.IsLetterOrDigit(parameter[i]) && parameter[i] != '_')
				{
					failure?.Invoke($"The '{parameter}' name contains illegal characters.");
					return false;
				}
			}

			//通过所有检测，返回成功
			return true;
		}

		public Task<bool> ValidateAsync(string data, Action<string> failure = null, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Validate(data, failure));
		}
		#endregion

		#region 匹配方法
		public bool Match(string parameter)
		{
			return string.Equals(parameter, "Name", StringComparison.OrdinalIgnoreCase) |
			       string.Equals(parameter, "RoleName", StringComparison.OrdinalIgnoreCase) |
			       string.Equals(parameter, "Role.Name", StringComparison.OrdinalIgnoreCase);
		}

		bool IMatchable.Match(object parameter)
		{
			return this.Match(parameter as string);
		}
		#endregion
	}
}
