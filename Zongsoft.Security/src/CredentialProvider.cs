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

using Zongsoft.Services;
using Zongsoft.Runtime.Caching;

namespace Zongsoft.Security
{
	[Service(Modules.Security, typeof(ICredentialProvider))]
	public class CredentialProvider : ICredentialProvider
	{
		#region 事件定义
		public event EventHandler<CredentialRegisterEventArgs> Registered;
		public event EventHandler<CredentialRegisterEventArgs> Registering;
		public event EventHandler<CredentialUnregisterEventArgs> Unregistered;
		public event EventHandler<CredentialUnregisterEventArgs> Unregistering;
		#endregion

		#region 成员字段
		private ICache _cache;
		private readonly MemoryCache _memoryCache;
		#endregion

		#region 构造函数
		public CredentialProvider()
		{
			_memoryCache = new Runtime.Caching.MemoryCache("Zongsoft.Security.CredentialProvider.MemoryCache");

			//挂载内存缓存容器的事件
			_memoryCache.Changed += MemoryCache_Changed;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置凭证的缓存器。
		/// </summary>
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
		public void Register(Credential credential)
		{
			if(credential == null)
				throw new ArgumentNullException(nameof(credential));

			//激发注册开始事件
			this.OnRegistering(credential);

			//获取要注册的用户及应用场景已经注册的凭证号
			var originalCredentialId = this.Cache.GetValue(this.GetCacheKeyOfUser(credential.User.UserId.ToString(), credential.Scene)) as string;

			//确保同个用户在相同场景下只能存在一个凭证：如果获取的凭证号不为空并且有值，则
			if(originalCredentialId != null && originalCredentialId.Length > 0)
			{
				//将同名用户及场景下的原来的凭证删除（即踢下线）
				this.Cache.Remove(this.GetCacheKeyOfCredential(originalCredentialId));

				//将本地内存缓存中的凭证对象删除
				_memoryCache.Remove(originalCredentialId);
			}

			//设置当前用户及场景所对应的唯一凭证号为新注册的凭证号
			this.Cache.SetValue(this.GetCacheKeyOfUser(credential.User.UserId.ToString(), credential.Scene), credential.CredentialId, credential.Duration);

			//将当前凭证信息以JSON文本的方式保存到物理存储层中
			this.Cache.SetValue(this.GetCacheKeyOfCredential(credential.CredentialId), this.SerializeCertificationToJson(credential), credential.Duration);

			//将凭证对象保存到本地内存缓存中
			_memoryCache.SetValue(credential.CredentialId, new CredentialToken(credential), TimeSpan.FromSeconds(credential.Duration.TotalSeconds * 0.6));

			//激发注册完成事件
			this.OnRegistered(credential);
		}

		public void Unregister(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return;

			//激发准备注销事件
			this.OnUnregistering(credentialId);

			//获取指定编号的凭证对象
			var credential = this.GetCredential(credentialId);

			if(credential != null)
			{
				//从本地内存缓存中把指定编号的凭证对象删除
				_memoryCache.Remove(credentialId);

				//将凭证资料从缓存容器中删除
				this.Cache.Remove(this.GetCacheKeyOfCredential(credentialId));
				//将当前用户及场景对应的凭证号记录删除
				this.Cache.Remove(this.GetCacheKeyOfUser(credential.User.UserId.ToString(), credential.Scene));
			}

			//激发注销完成事件
			this.OnUnregistered(credential);
		}

		public Credential Renew(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				throw new ArgumentNullException(nameof(credentialId));

			//查找指定编号的凭证对象
			var credential = this.GetCredential(credentialId);

			//指定编号的凭证不存在，则中止续约
			if(credential == null)
				return null;

			//激发续约开始事件
			this.OnRenewing(credential);

			//创建一个新的凭证对象
			credential = credential.Renew();

			//将新的凭证对象以JSON文本的方式保存到物理存储层中
			this.Cache.SetValue(this.GetCacheKeyOfCredential(credential.CredentialId), this.SerializeCertificationToJson(credential), credential.Duration);

			//将当前用户及场景对应的凭证号更改为新创建的凭证号
			this.Cache.SetValue(this.GetCacheKeyOfUser(credential.User.UserId.ToString(), credential.Scene), credential.CredentialId, credential.Duration);

			//将新建的凭证保存到本地内存缓存中
			_memoryCache.SetValue(credential.CredentialId, new CredentialToken(credential), TimeSpan.FromSeconds(credential.Duration.TotalSeconds * 0.5));

			//将原来的凭证从物理存储层中删除
			this.Cache.Remove(this.GetCacheKeyOfCredential(credentialId));

			//将原来的凭证从本地内存缓存中删除
			_memoryCache.Remove(credentialId);

			//激发续约完成事件
			this.OnRenewed(credential);

			//返回续约后的新凭证对象
			return credential;
		}

		public Credential GetCredential(string credentialId)
		{
			if(string.IsNullOrEmpty(credentialId))
				return null;

			//首先从本地内存缓存中获取指定编号的凭证标记
			var token = _memoryCache.GetValue(credentialId) as CredentialToken;

			//如果本地缓存获取成功则直接返回
			if(token != null)
			{
				if(token.Active())
					this.Refresh(token);

				return token.Credential;
			}

			//在缓存容器中查找指定编号的凭证对象的序列化后的JSON文本
			var text = this.Cache.GetValue(this.GetCacheKeyOfCredential(credentialId)) as string;

			//如果缓存容器中没有找到指定编号的凭证则说明指定的编号无效或者该编号对应的凭证已经过期
			if(string.IsNullOrEmpty(text))
				return null;

			//反序列化JSON文本到凭证对象
			var credential = Zongsoft.Runtime.Serialization.Serializer.Json.Deserialize<Credential>(text);

			//如果反序列化失败则始终抛出异常
			if(credential == null)
				throw new CredentialException(credentialId, $"The cached content of the specified '{credentialId}' credential cannot be deserialized.");

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfUser(credential.User.UserId.ToString(), credential.Scene), credential.Duration);

			//顺延当前凭证缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfCredential(credential.CredentialId), credential.Duration);

			//将获取到的凭证保存到本地内存缓存中
			_memoryCache.SetValue(credential.CredentialId, new CredentialToken(credential), TimeSpan.FromSeconds(credential.Duration.TotalSeconds * 0.5));

			return credential;
		}

		public Credential GetCredential(string identity, string scene)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			var credentialId = this.Cache.GetValue(this.GetCacheKeyOfUser(identity, scene)) as string;

			if(string.IsNullOrEmpty(credentialId))
				return null;

			return this.GetCredential(credentialId);
		}
		#endregion

		#region 事件处理
		private void MemoryCache_Changed(object sender, Runtime.Caching.CacheChangedEventArgs e)
		{
			if(e.Reason != Runtime.Caching.CacheChangedReason.Expired)
				return;

			this.Refresh(e.OldValue as CredentialToken);
		}
		#endregion

		#region 激发事件
		protected virtual void OnRegistered(Credential credential)
		{
			this.Registered?.Invoke(this, new CredentialRegisterEventArgs(credential));
		}

		protected virtual void OnRegistering(Credential credential)
		{
			this.Registering?.Invoke(this, new CredentialRegisterEventArgs(credential));
		}

		protected virtual void OnUnregistered(Credential credential)
		{
			this.Unregistered?.Invoke(this, new CredentialUnregisterEventArgs(credential));
		}

		protected virtual void OnUnregistering(string credentialId)
		{
			this.Unregistering?.Invoke(this, new CredentialUnregisterEventArgs(credentialId));
		}

		protected virtual void OnRenewed(Credential credential)
		{
			this.Registered?.Invoke(this, new CredentialRegisterEventArgs(credential, true));
		}

		protected virtual void OnRenewing(Credential credential)
		{
			this.Unregistered?.Invoke(this, new CredentialUnregisterEventArgs(credential, true));
		}
		#endregion

		#region 私有方法
		private void Refresh(CredentialToken token)
		{
			if(token == null)
				return;

			//顺延当前用户及场景对应凭证号的缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfUser(token.Credential.User.UserId.ToString(), token.Credential.Scene), token.Credential.Duration);

			//顺延当前凭证缓存项的过期时长
			this.Cache.SetExpiry(this.GetCacheKeyOfCredential(token.Credential.CredentialId), token.Credential.Duration);

			//重置本地缓存项
			token.Reset();
		}

		private string SerializeCertificationToJson(Credential credential)
		{
			return Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(credential, new Runtime.Serialization.TextSerializationSettings()
			{
				Indented = false,
				Typed = true,
				IgnoreNull = true,
			});
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
			public CredentialToken(Credential credential)
			{
				this.Credential = credential ?? throw new ArgumentNullException(nameof(credential));
				_issuedTime = _activeTime = DateTime.Now;
			}
			#endregion

			#region 公共属性
			public Credential Credential
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
