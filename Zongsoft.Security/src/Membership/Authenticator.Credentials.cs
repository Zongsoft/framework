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
using System.Security.Claims;

using Zongsoft.Services;
using Zongsoft.Runtime.Caching;

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

		#region 静态变量
		private static readonly DateTime EPOCH = new DateTime(2000, 1, 1);
		#endregion

		#region 成员字段
		private ICache _cache;
		private readonly MemoryCache _memoryCache;
		#endregion

		#region 构造函数
		public Authenticator()
		{
			_memoryCache = new MemoryCache("Zongsoft.Security.Authenticator");

			//挂载内存缓存容器的事件
			_memoryCache.Changed += MemoryCache_Changed;
		}
		#endregion

		#region 公共属性
		public ICache Cache
		{
			get => _cache ?? (_cache = this.CacheProvider?.GetService(Modules.Security) ?? this.CacheProvider?.GetService(string.Empty));
			set => _cache = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency]
		public IServiceProvider<ICache> CacheProvider
		{
			get; set;
		}
		#endregion

		#region 公共方法
		public CredentialPrincipal Register(ClaimsIdentity identity, string scenario)
		{
			if(identity == null || !identity.IsAuthenticated)
				return null;

			//确保同个用户在相同场景下只能存在一个凭证
			if(this.Cache.GetValue(this.GetCacheKeyOfUser(identity.GetUserId().ToString(), scenario)) is string credentialId && credentialId.Length > 0)
			{
				//将同名用户及场景下的原来的凭证删除（即踢下线）
				this.Cache.Remove(this.GetCacheKeyOfCredential(credentialId));

				//将本地内存缓存中的凭证对象删除
				_memoryCache.Remove(credentialId);
			}

			//生成一个新的凭证编号
			credentialId = GenerateCredentialId();

			//创建一个新的凭证主体对象
			var principal = new CredentialPrincipal(credentialId, GenerateCredentialId(), scenario, this.Options.GetPeriod(scenario), identity);

			//激发“Registering”事件
			this.OnRegistering(principal);

			//将当前用户身份保存到物理存储层中
			this.Cache.SetValue(this.GetCacheKeyOfCredential(credentialId), principal.Serialize(), principal.Expiration);

			//设置当前用户及场景所对应的唯一凭证号为新注册的凭证号
			this.Cache.SetValue(this.GetCacheKeyOfUser(identity.GetUserId().ToString(), scenario), credentialId, principal.Expiration);

			//将凭证对象保存到本地内存缓存中
			_memoryCache.SetValue(credentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			//激发“Registered”事件
			this.OnRegistered(principal, false);

			return principal;
		}

		public void Unregister(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return;

			//激发“Unregistering”事件
			this.OnUnregistering(credentialId);

			//将凭证资料从缓存容器中删除
			this.Cache.Remove(this.GetCacheKeyOfCredential(credentialId));

			//将当前用户及场景对应的凭证号记录删除
			if(_memoryCache.TryGetValue<CredentialToken>(credentialId, out var token))
				this.Cache.Remove(this.GetCacheKeyOfUser(token.Principal.Identity.GetUserId().ToString(), token.Principal.Scenario));

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

			//激发“Unregistered”事件
			this.OnUnregistered(credentialId, true);

			//创建一个新的凭证对象
			principal = principal.Clone(GenerateCredentialId(), GenerateCredentialId());

			//将当前用户身份保存到物理存储层中
			this.Cache.SetValue(this.GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Expiration);

			//将当前用户及场景对应的凭证号更改为新创建的凭证号
			this.Cache.SetValue(this.GetCacheKeyOfUser(principal.Identity.GetUserId().ToString(), principal.Scenario), principal.CredentialId, principal.Expiration);

			//将新建的凭证保存到本地内存缓存中
			_memoryCache.SetValue(principal.CredentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			//将原来的凭证从物理存储层中删除
			this.Cache.Remove(this.GetCacheKeyOfCredential(credentialId));

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

			var value = this.Cache.GetValue(this.GetCacheKeyOfCredential(credentialId));

			var principal = value switch
			{
				byte[] buffer => CredentialPrincipal.Deserialize(buffer),
				Stream stream => CredentialPrincipal.Deserialize(stream),
				BinaryReader reader => new CredentialPrincipal(reader),
				_ => null,
			};

			if(principal == null)
				return null;

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfUser(principal.Identity.GetUserId().ToString(), principal.Scenario), principal.Expiration);

			//顺延当前凭证缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfCredential(credentialId), principal.Expiration);

			//将获取到的凭证保存到本地内存缓存中
			_memoryCache.SetValue(credentialId, new CredentialToken(principal), TimeSpan.FromSeconds(principal.Expiration.TotalSeconds * 0.6));

			return principal;
		}

		public CredentialPrincipal GetPrincipal(string identity, string scene)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			var credentialId = this.Cache.GetValue(this.GetCacheKeyOfUser(identity, scene)) as string;

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
			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfUser(token.Principal.Identity.GetUserId().ToString(), token.Principal.Scenario), token.Principal.Expiration);

			//顺延当前凭证缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfCredential(credentialId), token.Principal.Expiration);

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

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GenerateCredentialId()
		{
			return ((ulong)(DateTime.UtcNow - EPOCH).TotalSeconds).ToString() + Zongsoft.Common.Randomizer.GenerateString(8);
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
