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
using System.Security.Claims;

namespace Zongsoft.Security;

[Obsolete($"This class has been deprecated, Please use the {nameof(ClaimsIdentityModeling)} class instead.")]
public static class ClaimsIdentityModel
{
	#region 私有变量
	private static readonly Caching.MemoryCache _cache = new();
	#endregion

	#region 公共方法
	public static TIdentityModel Get<TIdentityModel>(string scheme = null) where TIdentityModel : class =>
		GetCore<TIdentityModel>((Services.ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme);

	public static TIdentityModel Get<TIdentityModel>(string scheme, Func<TIdentityModel, Claim, bool> configure) where TIdentityModel : class =>
		GetCore((Services.ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme, identity => identity.AsModel(configure));

	public static TIdentityModel Get<TIdentityModel>(string scheme, IClaimsIdentityTransformer transformer) where TIdentityModel : class =>
		GetCore((Services.ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme, identity => transformer.Transform(identity) as TIdentityModel);

	public static TIdentityModel Get<TIdentityModel>(CredentialPrincipal principal, string scheme = null) where TIdentityModel : class =>
		GetCore<TIdentityModel>(principal, scheme);

	public static TIdentityModel Get<TIdentityModel>(CredentialPrincipal principal, string scheme, Func<TIdentityModel, Claim, bool> configure) where TIdentityModel : class =>
		GetCore(principal, scheme, identity => identity.AsModel(configure));

	public static TIdentityModel Get<TIdentityModel>(CredentialPrincipal principal, string scheme, IClaimsIdentityTransformer transformer) where TIdentityModel : class =>
		GetCore(principal, scheme, identity => transformer.Transform(identity) as TIdentityModel);

	private static TIdentityModel GetCore<TIdentityModel>(CredentialPrincipal principal, string scheme, Func<ClaimsIdentity, TIdentityModel> transform = null) where TIdentityModel : class
	{
		if(principal == null || principal.CredentialId == null)
			return null;

		return _cache.GetOrCreate<TIdentityModel>(GetCacheKey(principal.CredentialId, scheme), key =>
		{
			//获取指定方案的安全身份
			var identity = principal.GetIdentity(scheme);

			//如果指定方案的身份获取失败则返回空（并阻止缓存）
			if(identity == null)
				return new(null, Zongsoft.Common.Notification.Notified);

			//将指定方案的安全身份转换为身份模型
			var model = transform == null ? identity.AsModel<TIdentityModel>() : transform(identity);

			//缓存身份模型及其失效的变更令牌
			return new(model, principal.Disposed);
		});
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	static string GetCacheKey(string credentialId, string scheme) => string.IsNullOrEmpty(scheme) ? credentialId : $"{credentialId}:{scheme}";
	#endregion
}