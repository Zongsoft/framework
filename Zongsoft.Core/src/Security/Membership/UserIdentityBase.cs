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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Security.Claims;

namespace Zongsoft.Security.Membership
{
	public abstract class UserIdentityBase : IUserIdentity, IEquatable<IUserIdentity>
	{
		#region 静态变量
		private static readonly Caching.MemoryCache _cache = new();
		#endregion

		#region 构造函数
		protected UserIdentityBase() { }
		#endregion

		#region 公共属性
		public uint UserId { get; set; }
		public string Name { get; set; }
		public string Nickname { get; set; }
		public string Namespace { get; set; }
		public string Description { get; set; }
		#endregion

		#region 静态方法
		protected static TIdentity Get<TIdentity>(Func<ClaimsIdentity, TIdentity> transform) where TIdentity : class, IUserIdentity => Get((Services.ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, string.Empty, transform);
		protected static TIdentity Get<TIdentity>(string scheme, Func<ClaimsIdentity, TIdentity> transform) where TIdentity : class, IUserIdentity => Get((Services.ApplicationContext.Current?.Principal ?? ClaimsPrincipal.Current) as CredentialPrincipal, scheme, transform);
		protected static TIdentity Get<TIdentity>(CredentialPrincipal principal, Func<ClaimsIdentity, TIdentity> transform) where TIdentity : class, IUserIdentity => Get(principal, string.Empty, transform);
		protected static TIdentity Get<TIdentity>(CredentialPrincipal principal, string scheme, Func<ClaimsIdentity, TIdentity> transform) where TIdentity : class, IUserIdentity
		{
			if(principal == null || principal.CredentialId == null)
				return null;

			//如果指定的安全主体对应的用户身份已缓存则直接返回
			if(_cache.TryGetValue(principal.CredentialId, out var cachedUser) && cachedUser != null)
				return (TIdentity)cachedUser;

			//确认当前的安全身份标识
			var identity = principal.GetIdentity(scheme);

			//将当前安全身份标识转换为用户身份
			var user = transform(identity);

			//如果安全主体的有效期大于零则将用户身份缓存起来
			if(principal.Validity > TimeSpan.Zero)
				_cache.SetValue(principal.CredentialId, user, principal.Validity.TotalHours > 24 ? TimeSpan.FromHours(24) : principal.Validity);

			return user;
		}
		#endregion

		#region 重写方法
		public bool Equals(IUserIdentity user) => user != null && user.UserId == this.UserId;
		public override bool Equals(object obj) => this.Equals(obj as IUserIdentity);
		public override int GetHashCode() => (int)this.UserId;
		public override string ToString() => string.IsNullOrEmpty(this.Namespace) ?
			$"[{this.UserId}]{this.Name}" :
			$"[{this.UserId}]{this.Name}@{this.Namespace}";
		#endregion
	}
}