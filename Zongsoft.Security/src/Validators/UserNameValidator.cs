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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Validators;

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
		if(!(char.IsLetter(data[0]) || data[0] == '_' || data[0] == '$'))
		{
			failure?.Invoke($"The '{data}' name contains illegal characters.");
			return false;
		}

		//检查名字(用户名或角色名)的其余字符的合法性
		for(int i = 1; i < data.Length; i++)
		{
			//名字的中间字符必须是字母、数字或下划线
			if(!char.IsLetterOrDigit(data[i]) && data[i] != '_')
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

	#region 服务匹配
	bool IMatchable.Match(object parameter) => this.Match(parameter as string);
	public bool Match(string parameter) => string.Equals(parameter, "Name", StringComparison.OrdinalIgnoreCase) |
		string.Equals(parameter, "UserName", StringComparison.OrdinalIgnoreCase) |
		string.Equals(parameter, "User.Name", StringComparison.OrdinalIgnoreCase);
	#endregion
}
