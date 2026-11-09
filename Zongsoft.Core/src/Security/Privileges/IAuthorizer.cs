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
using System.Security.Claims;

using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>提供关于鉴权相关功能的接口。</summary>
public interface IAuthorizer
{
	/// <summary>获取鉴权器名称。</summary>
	string Name { get; }

	/// <summary>获取权限定义器。</summary>
	Privileger Privileger { get; }

	/// <summary>获取权限评估器。</summary>
	IPrivilegeEvaluator Evaluator { get; }

	/// <summary>判断指定用户是否具有指定的授权。</summary>
	/// <param name="user">指定的用户身份。</param>
	/// <param name="privilege">指定的权限标识。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果具有授权则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	/// <remarks>该验证会对指定的用户所属角色逐级向上展开做授权判断，因此只需对本方法一次调用即可得知指定用户的最终授权运算结果。</remarks>
	ValueTask<bool> AuthorizeAsync(ClaimsIdentity user, string privilege, Parameters parameters, CancellationToken cancellation = default);
}
