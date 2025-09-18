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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Security.Claims;
using System.Collections.Concurrent;

using Zongsoft.Services;

namespace Zongsoft.Security;

public static class ClaimsIdentityModeling
{
	#region 私有变量
	private static readonly Caching.MemoryCache _cache = new();
	#endregion

	#region 公共方法
	public static T GetModel<T>(string scheme = null) => (T)GetModel(scheme);
	public static object GetModel(string scheme = null) =>
		GetModelCore((ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme);
	public static T GetModel<T>(Func<object, T> wrapper) =>
		GetModelCore((ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, null, wrapper);
	public static T GetModel<T>(string scheme, Func<object, T> wrapper) =>
		GetModelCore((ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme, wrapper);

	public static T GetModel<T>(this ClaimsPrincipal principal, string scheme = null) => (T)GetModel(principal, scheme);
	public static object GetModel(this ClaimsPrincipal principal, string scheme = null) =>
		GetModelCore(principal as CredentialPrincipal, scheme);
	public static T GetModel<T>(this ClaimsPrincipal principal, Func<object, T> wrapper) =>
		GetModelCore(principal as CredentialPrincipal, null, wrapper);
	public static T GetModel<T>(this ClaimsPrincipal principal, string scheme, Func<object, T> wrapper) =>
		GetModelCore(principal as CredentialPrincipal, scheme, wrapper);

	public static object Transform(this ClaimsIdentity identity)
	{
		if(identity == null || identity.IsAnonymous())
			return null;

		if(Privileges.Authentication.Transformer is ClaimsPrincipalTransformer principalTransformer)
		{
			foreach(var transformer in principalTransformer.Transformers)
			{
				if(transformer.CanTransform(identity))
					return transformer.Transform(identity);
			}
		}

		var transformers = ApplicationContext.Current.Services.ResolveAll<IClaimsIdentityTransformer>();

		foreach(var transformer in transformers)
		{
			if(transformer.CanTransform(identity))
				return transformer.Transform(identity);
		}

		return null;
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	static string GetCacheKey(string credentialId, string scheme) => string.IsNullOrEmpty(scheme) ? credentialId : $"{credentialId}:{scheme}";

	private static Entry GetModelEntry(this CredentialPrincipal principal, string scheme = null)
	{
		return _cache.GetOrCreate<Entry>(GetCacheKey(principal.CredentialId, scheme), key =>
		{
			//获取指定方案的安全身份
			var identity = principal.GetIdentity(scheme);

			//如果指定方案的身份获取失败则返回空（并阻止缓存）
			if(identity == null)
				return new(null, Zongsoft.Common.Notification.Notified);

			//将指定方案的安全身份转换为身份模型
			var model = identity.Transform();

			//缓存身份模型及其失效的变更令牌
			return model != null ?
				new(new Entry(model), principal.Disposed) :
				new(null, Zongsoft.Common.Notification.Notified);
		});
	}

	private static object GetModelCore(this CredentialPrincipal principal, string scheme = null)
	{
		if(principal == null || principal.CredentialId == null)
			return null;

		var result = GetModelEntry(principal, scheme);
		return result?.Identity;
	}

	private static T GetModelCore<T>(this CredentialPrincipal principal, string scheme, Func<object, T> wrapper)
	{
		if(wrapper == null)
			throw new ArgumentNullException(nameof(wrapper));

		if(principal == null || principal.CredentialId == null)
			return default;

		var result = GetModelEntry(principal, scheme);
		return result == null ? default : result.Get(wrapper);
	}
	#endregion

	#region 嵌套子类
	private sealed class Entry(object identity)
	{
		public readonly object Identity = identity;
		private volatile ConcurrentDictionary<Type, object> _bag;

		public T Get<T>(Func<object, T> creator)
		{
			if(_bag == null)
				Interlocked.CompareExchange(ref _bag, new(), null);

			return (T)_bag.GetOrAdd(typeof(T), key => creator(this.Identity));
		}
	}
	#endregion
}
