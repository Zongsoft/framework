﻿/*
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
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(ICredentialProvider))]
	public class CredentialProvider : ICredentialProvider
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
		public CredentialProvider() => _memoryCache = new MemoryCache();
		#endregion

		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthenticationOptions Options { get; set; }

		[ServiceDependency("~", IsRequired = true)]
		public IDistributedCache Cache { get; set; }
		#endregion

		#region 公共方法
		public void Register(CredentialPrincipal principal)
		{
			if(principal == null || principal.Identity == null)
				return;

			var cache = this.Cache;

			//确保同个用户在相同场景下只能存在一个凭证
			if(cache.GetValue(GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario)) is string credentialId && credentialId.Length > 0)
			{
				//将同名用户及场景下的原来的凭证删除（即踢下线）
				cache.Remove(GetCacheKeyOfCredential(credentialId));

				//将本地内存缓存中的凭证对象删除
				_memoryCache.Remove(credentialId);
			}

			//确保指定的凭证主体的过期时间是有效的
			if(principal.Validity == TimeSpan.Zero)
				principal.Validity = this.Options.GetPeriod(principal.Scenario);

			//激发“Registering”事件
			this.OnRegistering(principal);

			//将当前凭证主体保存到分布式缓存中
			cache.SetValue(GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Validity);

			//设置当前用户及场景所对应的唯一凭证号为新注册的凭证号
			cache.SetValue(GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.CredentialId, principal.Validity);

			//将凭证对象保存到本地内存缓存中
			_memoryCache.SetValue(
				principal.CredentialId,
				new CredentialToken(principal),
				CachePriority.High,
				TimeSpan.FromTicks(principal.Validity.Ticks));

			//激发“Registered”事件
			this.OnRegistered(principal, false);
		}

		public void Unregister(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return;

			var cache = this.Cache;

			//激发“Unregistering”事件
			this.OnUnregistering(credentialId);

			//将凭证资料从缓存容器中删除
			cache.Remove(GetCacheKeyOfCredential(credentialId));

			//将当前用户及场景对应的凭证号记录删除
			if(_memoryCache.TryGetValue<CredentialToken>(credentialId, out var token))
			{
				cache.Remove(GetCacheKeyOfUser(token.Principal.Identity.GetIdentifier(), token.Principal.Scenario));

				//从本地内存缓存中把指定编号的凭证对象删除
				_memoryCache.Remove(credentialId);
			}

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

			var cache = this.Cache;

			//激发“Unregistered”事件
			this.OnUnregistered(credentialId, true);

			//创建一个新的凭证对象
			principal = principal.Clone();

			//将当前用户身份保存到分布式缓存中
			cache.SetValue(GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Validity);

			//将当前用户及场景对应的凭证号更改为新创建的凭证号
			cache.SetValue(GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.CredentialId, principal.Validity);

			//将新建的凭证保存到本地内存缓存中
			_memoryCache.SetValue(
				principal.CredentialId,
				new CredentialToken(principal),
				CachePriority.High,
				TimeSpan.FromTicks(principal.Validity.Ticks));

			//将原来的凭证从分布式缓存中删除
			cache.Remove(GetCacheKeyOfCredential(credentialId));

			//将原来的凭证从本地内存缓存中删除
			_memoryCache.Remove(credentialId);

			//激发“Registered”事件
			this.OnRegistered(principal, true);

			//返回续约后的新凭证对象
			return principal;
		}

		public CredentialPrincipal Refresh(string credentialId)
		{
			var principal = this.GetPrincipal(credentialId);
			if(principal == null)
				return null;

			foreach(var challenger in Authentication.Instance.Challengers)
				challenger.Challenge(principal, principal.Scenario);

			//将刷新后的凭证主体保存到分布式缓存中
			this.Cache.SetValue(GetCacheKeyOfCredential(principal.CredentialId), principal.Serialize(), principal.Validity);

			//将凭证对象保存到本地内存缓存中
			_memoryCache.SetValue(
				principal.CredentialId,
				new CredentialToken(principal),
				CachePriority.High,
				TimeSpan.FromTicks(principal.Validity.Ticks));

			return principal;
		}

		public IEnumerable<CredentialPrincipal> Refresh(string identifier, string scenario = null)
		{
			if(string.IsNullOrEmpty(identifier))
				yield break;

			if(string.IsNullOrEmpty(scenario) || scenario == "*")
			{
				var keys = this.Cache.Find(GetCacheKeyOfUser(identifier, "*"));

				foreach(var key in keys)
				{
					var credentialId = this.Cache.GetValue<string>(key);

					if(!string.IsNullOrEmpty(credentialId))
						yield return this.Refresh(this.Cache.GetValue<string>(key));
				}
			}
			else
			{
				var credentialId = this.Cache.GetValue<string>(GetCacheKeyOfUser(identifier, scenario?.Trim().ToLowerInvariant()));

				if(!string.IsNullOrEmpty(credentialId))
					yield return this.Refresh(credentialId);
			}
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

			var cache = this.Cache;
			var buffer = cache.GetValue<byte[]>(GetCacheKeyOfCredential(credentialId));

			if(buffer == null || buffer.Length == 0)
				return null;

			var principal = CredentialPrincipal.Deserialize(buffer);

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			cache.SetExpiry(GetCacheKeyOfUser(principal.Identity.GetIdentifier(), principal.Scenario), principal.Validity);

			//顺延当前凭证缓存项的过期时长
			cache.SetExpiry(GetCacheKeyOfCredential(credentialId), principal.Validity);

			//将获取到的凭证保存到本地内存缓存中
			_memoryCache.SetValue(
				credentialId,
				new CredentialToken(principal),
				CachePriority.High,
				TimeSpan.FromTicks(principal.Validity.Ticks));

			return principal;
		}

		public IEnumerable<CredentialPrincipal> GetPrincipals(string identifier, string scenario = null)
		{
			if(string.IsNullOrEmpty(identifier))
				yield break;

			if(string.IsNullOrEmpty(scenario) || scenario == "*")
			{
				var keys = this.Cache.Find(GetCacheKeyOfUser(identifier, "*"));

				foreach(var key in keys)
				{
					var credentialId = this.Cache.GetValue<string>(key);

					if(!string.IsNullOrEmpty(credentialId))
						yield return this.GetPrincipal(this.Cache.GetValue<string>(key));
				}
			}
			else
			{
				var credentialId = this.Cache.GetValue<string>(GetCacheKeyOfUser(identifier, scenario?.Trim().ToLowerInvariant()));

				if(!string.IsNullOrEmpty(credentialId))
					yield return this.GetPrincipal(credentialId);
			}
		}
		#endregion

		#region 激发事件
		protected virtual void OnRegistered(CredentialPrincipal principal, bool renewal) => this.Registered?.Invoke(this, new CredentialRegisterEventArgs(principal, renewal));
		protected virtual void OnRegistering(CredentialPrincipal principal) => this.Registering?.Invoke(this, new CredentialRegisterEventArgs(principal));
		protected virtual void OnUnregistered(string credentialId, bool renewal) => this.Unregistered?.Invoke(this, new CredentialUnregisterEventArgs(credentialId, renewal));
		protected virtual void OnUnregistering(string credentialId) => this.Unregistering?.Invoke(this, new CredentialUnregisterEventArgs(credentialId));
		#endregion

		#region 私有方法
		private void Refresh(string credentialId, CredentialToken token)
		{
			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			this.Cache.SetExpiry(GetCacheKeyOfUser(token.Principal.Identity.GetIdentifier(), token.Principal.Scenario), token.Principal.Validity);

			//顺延当前凭证缓存项的过期时长
			this.Cache.SetExpiry(GetCacheKeyOfCredential(credentialId), token.Principal.Validity);

			//重置本地缓存的时间信息
			token.Reset();
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCacheKeyOfUser(string identifier, string scenario) => "Zongsoft.Security:" +
		(
			string.IsNullOrWhiteSpace(scenario) ? identifier : $"{identifier}!{scenario}"
		);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCacheKeyOfCredential(string credentialId) => $"Zongsoft.Security.Credential:{credentialId}";
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
			public CredentialPrincipal Principal { get; }
			public DateTime IssuedTime => _issuedTime;
			public DateTime ActiveTime => _activeTime;
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
