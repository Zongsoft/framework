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
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

[System.Reflection.DefaultMember(nameof(Privileger))]
[System.ComponentModel.DefaultProperty(nameof(Privileger))]
public abstract class AuthorizerBase : IAuthorizer, IMatchable, IMatchable<ClaimsPrincipal>
{
	#region 私有字段
	private readonly MemoryCache _cache;
	#endregion

	#region 构造函数
	protected AuthorizerBase(string name)
	{
		this.Name = name ?? string.Empty;
		this.Privileger = new();
		_cache = new MemoryCache();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public Privileger Privileger { get; }
	public abstract IPrivilegeEvaluator Evaluator { get; }
	#endregion

	#region 公共方法
	public virtual async ValueTask<bool> AuthorizeAsync(ClaimsIdentity user, string privilege, Parameters parameters, CancellationToken cancellation = default)
	{
		if(user == null)
			return false;

		if(privilege == null)
			return false;

		var privileges = await _cache.GetOrCreateAsync(user.Identify(),
			key => (GetPrivilegesAsync((Identifier)key, cancellation), TimeSpan.FromMinutes(60)));

		return privileges.Contains(privilege);

		async Task<HashSet<string>> GetPrivilegesAsync(Identifier identifier, CancellationToken cancellation)
		{
			var privileges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var results = this.Evaluator.EvaluateAsync(identifier, parameters, cancellation);
			await foreach(var result in results)
				privileges.Add(result.Privilege);

			return privileges;
		}
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
