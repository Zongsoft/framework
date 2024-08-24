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
using System.Linq;
using System.Security.Claims;

namespace Zongsoft.Security
{
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

			//如果指定的安全主体中对应的身份模型已缓存则直接返回
			if(_cache.TryGetValue(GetCacheKey(principal.CredentialId, scheme), out var identityModel) && identityModel != null)
				return (TIdentityModel)identityModel;

			//确认当前的安全身份标识
			var identity = principal.GetIdentity(scheme);

			//将当前安全身份标识转换为身份模型
			var model = transform == null ? identity.AsModel<TIdentityModel>() : transform(identity);

			//如果安全主体的有效期大于零则将身份模型缓存起来
			if(principal.Validity > TimeSpan.Zero)
				_cache.SetValue(GetCacheKey(principal.CredentialId, scheme), model, principal.Validity.TotalHours > 24 ? TimeSpan.FromHours(24) : principal.Validity);

			return model;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static string GetCacheKey(string credentialId, string scheme) => string.IsNullOrEmpty(scheme) ? credentialId : $"{credentialId}:{scheme}";
		#endregion
	}
}