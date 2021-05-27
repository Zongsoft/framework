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
using System.IO;

using Zongsoft.Caching;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	partial class Authenticator : ICredentialProvider
	{
		#region 事件定义
		public event EventHandler<CredentialRegisterEventArgs> Registered;
		public event EventHandler<CredentialRegisterEventArgs> Registering;
		public event EventHandler<CredentialUnregisterEventArgs> Unregistered;
		public event EventHandler<CredentialUnregisterEventArgs> Unregistering;
		#endregion

		#region 成员字段
		private readonly MemoryCache _memoryCache;
		#endregion

		#region 构造函数
		public Authenticator(IServiceProvider serviceProvider) : base(serviceProvider)
		{
			_memoryCache = new MemoryCache("Zongsoft.Security.Authenticator");

			//挂载内存缓存容器的事件
			_memoryCache.Changed += MemoryCache_Changed;
		}
		#endregion

		#region 公共属性
		[ServiceDependency]
		public IServiceAccessor<ICache> Cache { get; set; }
		#endregion

		#region 公共方法
		public void Register(CredentialPrincipal principal)
		{
			if(principal == null || principal.Identity == null)
				return;

			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");

			//确保同个用户在相同场景下只能存在一个凭证
			if(cache.GetValue(this.GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario)) is string credentialId && credentialId.Length > 0)
			{
				//将同名用户及场景下的原来的凭证删除（即踢下线）
				cache.Remove(this.GetCacheKeyOfCredential(credentialId));

				//将本地内存缓存中的凭证对象删除
				_memoryCache.Remove(credentialId);
			}

			//确保指定的凭证主体的过期时间是有效的
			if(principal.Expiration == TimeSpan.Zero)
				principal.Expiration = this.Options.GetPeriod(principal.Scenario);

			//激发“Registering”事件
			this.OnRegistering(principal);

			//将当前用户身份保存到物理存储层中
			cache.SetValue(this.GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Expiration);

			//设置当前用户及场景所对应的唯一凭证号为新注册的凭证号
			cache.SetValue(this.GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.CredentialId, principal.Expiration);

			//将凭证对象保存到本地内存缓存中
			_memoryCache.SetValue(principal.CredentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			//激发“Registered”事件
			this.OnRegistered(principal, false);
		}

		public void Unregister(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return;

			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");

			//激发“Unregistering”事件
			this.OnUnregistering(credentialId);

			//将凭证资料从缓存容器中删除
			cache.Remove(this.GetCacheKeyOfCredential(credentialId));

			//将当前用户及场景对应的凭证号记录删除
			if(_memoryCache.TryGetValue<CredentialToken>(credentialId, out var token))
				cache.Remove(this.GetCacheKeyOfUser(token.Principal.Identity.GetIdentifier(), token.Principal.Scenario));

			//从本地内存缓存中把指定编号的凭证对象删除
			_memoryCache.Remove(credentialId);

			//激发“Unregistered”事件
			this.OnUnregistered(credentialId, false);
		}

		public CredentialPrincipal Renew(string credentialId, string token)
		{
			if(string.IsNullOrEmpty(credentialId))
				throw new ArgumentNullException(nameof(credentialId));

			var principal = this.GetPrincipal(credentialId);

			if(principal == null || token != principal.RenewalToken)
				return null;

			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");

			//激发“Unregistered”事件
			this.OnUnregistered(credentialId, true);

			//创建一个新的凭证对象
			principal = principal.Clone(Authentication.GenerateId(out token), token);

			//将当前用户身份保存到物理存储层中
			cache.SetValue(this.GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Expiration);

			//将当前用户及场景对应的凭证号更改为新创建的凭证号
			cache.SetValue(this.GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.CredentialId, principal.Expiration);

			//将新建的凭证保存到本地内存缓存中
			_memoryCache.SetValue(principal.CredentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			//将原来的凭证从物理存储层中删除
			cache.Remove(this.GetCacheKeyOfCredential(credentialId));

			//将原来的凭证从本地内存缓存中删除
			_memoryCache.Remove(credentialId);

			//激发“Registered”事件
			this.OnRegistered(principal, true);

			//返回续约后的新凭证对象
			return principal;
		}

		public CredentialPrincipal GetPrincipal(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return null;

			//如果本地缓存获取成功则直接返回
			if(_memoryCache.GetValue(credentialId) is CredentialToken token)
			{
				if(token.Active())
					this.Refresh(credentialId, token);

				return token.Principal;
			}

			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");
			var buffer = cache.GetValue<byte[]>(this.GetCacheKeyOfCredential(credentialId));

			if(buffer == null || buffer.Length == 0)
				return null;

			var principal = CredentialPrincipal.Deserialize(buffer);

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			cache.SetExpiry(this.GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.Expiration);

			//顺延当前凭证缓存项的过期时长
			cache.SetExpiry(this.GetCacheKeyOfCredential(credentialId), principal.Expiration);

			//将获取到的凭证保存到本地内存缓存中
			_memoryCache.SetValue(credentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			return principal;
		}

		public CredentialPrincipal GetPrincipal(string identity, string scene)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");
			var credentialId = cache.GetValue<string>(this.GetCacheKeyOfUser(identity, scene));

			if(string.IsNullOrEmpty(credentialId))
				return null;

			return this.GetPrincipal(credentialId);
		}
		#endregion

		#region 事件处理
		private void MemoryCache_Changed(object sender, CacheChangedEventArgs e)
		{
			if(e.Reason != CacheChangedReason.Expired)
				return;

			this.Refresh(e.Key, e.OldValue as CredentialToken);
		}
		#endregion

		#region 激发事件
		protected virtual void OnRegistered(CredentialPrincipal principal, bool renewal)
		{
			this.Registered?.Invoke(this, new CredentialRegisterEventArgs(principal, renewal));
		}

		protected virtual void OnRegistering(CredentialPrincipal principal)
		{
			this.Registering?.Invoke(this, new CredentialRegisterEventArgs(principal));
		}

		protected virtual void OnUnregistered(string credentialId, bool renewal)
		{
			this.Unregistered?.Invoke(this, new CredentialUnregisterEventArgs(credentialId, renewal));
		}

		protected virtual void OnUnregistering(string credentialId)
		{
			this.Unregistering?.Invoke(this, new CredentialUnregisterEventArgs(credentialId));
		}
		#endregion

		#region 私有方法
		private void Refresh(string credentialId, CredentialToken token)
		{
			var cache = this.Cache.Value ?? throw new InvalidOperationException($"Missing the required cache.");

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			cache.SetExpiry(this.GetCacheKeyOfUser(token.Principal.Identity.GetIdentifier(), token.Principal.Scenario), token.Principal.Expiration);

			//顺延当前凭证缓存项的过期时长
			cache.SetExpiry(this.GetCacheKeyOfCredential(credentialId), token.Principal.Expiration);

			//重置本地缓存的时间信息
			token.Reset();
		}

		private string GetCacheKeyOfUser(string identity, string scene)
		{
			return "Zongsoft.Security:" +
				(
					string.IsNullOrWhiteSpace(scene) ?
					identity :
					identity + "!" + scene.Trim().ToLowerInvariant()
				);
		}

		private string GetCacheKeyOfCredential(string credentialId)
		{
			if(string.IsNullOrWhiteSpace(credentialId))
				throw new ArgumentNullException(nameof(credentialId));

			return "Zongsoft.Security.Credential:" + credentialId.Trim().ToUpperInvariant();
		}
		#endregion

		#region 嵌套结构
		private class CredentialToken
		{
			#region 成员字段
			private DateTime _issuedTime;
			private DateTime _activeTime;
			#endregion

			#region 构造函数
			public CredentialToken(CredentialPrincipal principal)
			{
				this.Principal = principal ?? throw new ArgumentNullException(nameof(principal));
				_issuedTime = _activeTime = DateTime.Now;
			}
			#endregion

			#region 公共属性
			public CredentialPrincipal Principal
			{
				get;
			}

			public DateTime IssuedTime
			{
				get => _issuedTime;
			}

			public DateTime ActiveTime
			{
				get => _activeTime;
			}
			#endregion

			#region 公共方法
			public bool Active()
			{
				_activeTime = DateTime.Now;
				return (_activeTime - _issuedTime).TotalHours > 1;
			}

			public void Reset()
			{
				_issuedTime = _activeTime = DateTime.Now;
			}
			#endregion
		}
		#endregion
	}
}
