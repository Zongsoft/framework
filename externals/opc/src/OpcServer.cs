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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Configuration;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc;

public partial class OpcServer : WorkerBase
{
	#region 成员字段
	private ApplicationInstance _launcher;
	private Server _server;
	#endregion

	#region 构造函数
	public OpcServer(string name = null) : base(name)
	{
		var configuration = GetConfiguration(this.Name);

		//验证服务配置
		configuration.Validate(ApplicationType.Server);

		_launcher = new ApplicationInstance()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Server,
			ApplicationConfiguration = configuration,
		};

		//必须：检查应用启动器的安全证书
		_launcher.CheckApplicationInstanceCertificates(false);

		_server = new Server();
	}
	#endregion

	#region 配置方法
	private static ApplicationConfiguration GetConfiguration(string name) => new()
	{
		ApplicationName = "OpcServer",
		ApplicationUri = Utils.Format(@"urn:{0}:OpcServer", System.Net.Dns.GetHostName()),
		ApplicationType = ApplicationType.Server,
		ProductUri = ApplicationContext.Current?.Name,

		ServerConfiguration = new ServerConfiguration()
		{
			BaseAddresses = { "opc.tcp://localhost:4841/OpcServer" },
			DiagnosticsEnabled = true,
			MinRequestThreadCount = 5,
			MaxRequestThreadCount = 100,
			MaxQueuedRequestCount = 200,

			RegistrationEndpoint = new EndpointDescription("opc.tcp://localhost:4840")
			{
				SecurityMode = MessageSecurityMode.None,
				Server = new ApplicationDescription()
				{
					ApplicationType = ApplicationType.DiscoveryServer,
				}
			},

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
			ApplicationCertificate = new CertificateIdentifier
			{
				StoreType = @"Directory",
				StorePath = @"certificates",
				SubjectName = $"CN={name}, DC={System.Net.Dns.GetHostName()}",
			},
			//TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"certificates/authorities" },
			//TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"certificates/applications" },
			//RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"certificates/rejected" },
			AutoAcceptUntrustedCertificates = true,
			AddAppCertToTrustedStore = true
		},
		//TransportConfigurations = new TransportConfigurationCollection(),
		TransportQuotas = new TransportQuotas
		{
			OperationTimeout = 60000,
			ChannelLifetime = 60000,
			SecurityTokenLifetime = 3600000,
		},
		//ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
		//TraceConfiguration = new TraceConfiguration()
	};
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation) => _launcher.Start(_server);
	protected override Task OnStopAsync(string[] args, CancellationToken cancellation) { _launcher.Stop(); return Task.CompletedTask; }
	#endregion
}

partial class OpcServer
{
	private sealed class Server : StandardServer
	{
		#region 成员字段
		private NodeManager _manager;
		#endregion

		#region 重写方法
		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
		{
			_manager = new NodeManager(server, configuration);
			return new MasterNodeManager(server, configuration, null, [_manager]);
		}

		protected override void OnNodeManagerStarted(IServerInternal server)
		{
			base.OnNodeManagerStarted(server);
		}

		public override ResponseHeader AddNodes(RequestHeader requestHeader, AddNodesItemCollection nodes, out AddNodesResultCollection results, out DiagnosticInfoCollection diagnostics)
		{
			var context = this.ValidateRequest(requestHeader, RequestType.AddNodes);

			try
			{
				if(nodes == null || nodes.Count == 0)
					throw new ServiceResultException(StatusCodes.BadNothingToDo);

				_manager.AddNodes(context, nodes, out results, out diagnostics);

				return this.CreateResponse(requestHeader, StatusCodes.Good);
			}
			catch(ServiceResultException ex)
			{
				lock(this.ServerInternal.DiagnosticsLock)
				{
					this.ServerInternal.ServerDiagnostics.RejectedRequestsCount++;

					if(this.IsSecurityError(ex.StatusCode))
					{
						this.ServerInternal.ServerDiagnostics.SecurityRejectedRequestsCount++;
					}
				}

				throw this.TranslateException(context, ex);
			}
			finally
			{
				this.OnRequestComplete(context);
			}
		}
		#endregion
	}
}