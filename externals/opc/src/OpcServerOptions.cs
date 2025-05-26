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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.ObjectModel;

using Opc.Ua;
using Opc.Ua.Server;

namespace Zongsoft.Externals.Opc;

public class OpcServerOptions
{
	#region 构造函数
	public OpcServerOptions(string name) : this(name, null) { }
	public OpcServerOptions(string name, string @namespace, params string[] urls)
	{
		this.Name = string.IsNullOrEmpty(name) ? "Zongsoft.OpcServer" : name;
		this.Namespace = string.IsNullOrEmpty(@namespace) ? $"urn:{Environment.MachineName}:{this.Name}" : @namespace;
		this.Storages = new();

		if(urls != null && urls.Length > 0)
			this.Urls = urls;
		else
		{
			this.Urls = ["opc.tcp://localhost:4840"];
			this.Discovery = "opc.tcp://localhost:4840";
		}
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Namespace { get; init; }
	public string Discovery { get; init; }
	public string[] Urls { get; init; }
	public StorageOptionsCollection Storages { get; }
	#endregion

	#region 内部方法
	internal ApplicationConfiguration GetConfiguration()
	{
		var configuration = new ApplicationConfiguration()
		{
			ApplicationName = this.Name,
			ApplicationUri = this.Namespace,
			ApplicationType = ApplicationType.Server,

			ServerConfiguration = new ServerConfiguration()
			{
				BaseAddresses = this.Urls,
				DiagnosticsEnabled = true,

				MinRequestThreadCount = 10,
				MaxRequestThreadCount = 100,
				MaxQueuedRequestCount = 200,

				MaxSubscriptionCount = 1000,
				MaxNotificationsPerPublish = 1000,
				MinMetadataSamplingInterval = 100,
				RegistrationEndpoint = string.IsNullOrEmpty(this.Discovery) ? null : new EndpointDescription(this.Discovery),

				SecurityPolicies =
				[
					new ServerSecurityPolicy()
					{
						SecurityMode = MessageSecurityMode.None,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None"
					},
					new ServerSecurityPolicy()
					{
						SecurityMode = MessageSecurityMode.Sign,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
					},
					new ServerSecurityPolicy()
					{
						SecurityMode = MessageSecurityMode.SignAndEncrypt,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
					},
				],
				UserTokenPolicies =
				[
					new UserTokenPolicy(UserTokenType.Anonymous)
					{
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None",
					},
					new UserTokenPolicy(UserTokenType.UserName)
					{
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
					},
					new UserTokenPolicy(UserTokenType.Certificate)
					{
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
					}
				],
			},
			SecurityConfiguration = new SecurityConfiguration
			{
				AddAppCertToTrustedStore = true,
				AutoAcceptUntrustedCertificates = true,

				ApplicationCertificate = new CertificateIdentifier
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
					SubjectName = $"CN={this.Name}, DC={Environment.MachineName}",
				},
				TrustedIssuerCertificates = new CertificateTrustList
				{
					StoreType = @"Directory",
					StorePath = @"certificates/authorities"
				},
				TrustedPeerCertificates = new CertificateTrustList
				{
					StoreType = @"Directory",
					StorePath = @"certificates/applications"
				},
				RejectedCertificateStore = new CertificateTrustList
				{
					StoreType = @"Directory",
					StorePath = @"certificates/rejected"
				},
			},
			TransportQuotas = new TransportQuotas(),
			//TransportConfigurations = [],
			//TraceConfiguration = new TraceConfiguration(),
		};

		//验证服务配置
		configuration.Validate(ApplicationType.Server);

		return configuration;
	}
	#endregion

	#region 嵌套子类
	public class StorageOptions
	{
		public StorageOptions(string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace))
				throw new ArgumentNullException(nameof(@namespace));

			this.Namespace = @namespace;
			this.Prefabs = new(this.Namespace);
		}

		public string Namespace { get; }
		public PrefabCollection Prefabs { get; }
	}

	public class StorageOptionsCollection : KeyedCollection<string, StorageOptions>
	{
		public StorageOptions Define(string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace))
				throw new ArgumentNullException(nameof(@namespace));

			if(this.TryGetValue(@namespace, out var result))
				return result;

			result = new StorageOptions(@namespace);
			this.Add(result);
			return result;
		}

		protected override string GetKeyForItem(StorageOptions storage) => storage.Namespace;
	}
	#endregion
}
