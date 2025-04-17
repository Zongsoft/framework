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
	private ApplicationInstance _launcher;
	private Server _server;

	public OpcServer(string name = null) : base(name)
	{
		var configuration = GetConfiguration(this.Name);

		configuration.Validate(ApplicationType.Server);

		_launcher = new ApplicationInstance()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Server,
			ApplicationConfiguration = configuration,
		};

		_launcher.CheckApplicationInstanceCertificates(false);

		_server = new Server();
	}

	private static ApplicationConfiguration GetConfiguration(string name) => new()
	{
		ApplicationName = "OpcServer",
		ApplicationUri = Utils.Format(@"urn:{0}:OpcServer", System.Net.Dns.GetHostName()),
		ApplicationType = ApplicationType.Server,

		ServerConfiguration = new ServerConfiguration()
		{
			BaseAddresses = { "opc.tcp://localhost:4844/OpcServer", "https://localhost:4841/OpcServer" },
			DiagnosticsEnabled = true,
			MinRequestThreadCount = 5,
			MaxRequestThreadCount = 100,
			MaxQueuedRequestCount = 200,

			RegistrationEndpoint = new EndpointDescription("opc.tcp://localhost:4848")
			{
				Server = new ApplicationDescription()
				{
					ApplicationType = ApplicationType.DiscoveryServer,
					ApplicationUri = "opc.tcp://localhost:4848",
					DiscoveryUrls = ["opc.tcp://localhost:4848"],
				}
			},

			SecurityPolicies =
			[
				new ServerSecurityPolicy()
				{
					SecurityMode = MessageSecurityMode.None,
					SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None"
				}
			],
			UserTokenPolicies =
			[
				new UserTokenPolicy(UserTokenType.Anonymous)
				{
					SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None",
				},
				new UserTokenPolicy(UserTokenType.UserName)
				{
					SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256",
				}
			],
		},
		SecurityConfiguration = new SecurityConfiguration
		{
			ApplicationCertificate = new CertificateIdentifier
			{
				StoreType = @"Directory",
				StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
				SubjectName = $"CN={name}, DC={System.Net.Dns.GetHostName()}",
			},
			//TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
			//TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
			//RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
			AutoAcceptUntrustedCertificates = true,
			AddAppCertToTrustedStore = true
		},
		//TransportConfigurations = new TransportConfigurationCollection(),
		TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
		//ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
		//TraceConfiguration = new TraceConfiguration()
	};

	protected override Task OnStartAsync(string[] args, CancellationToken cancellation) => _launcher.Start(_server);
	protected override Task OnStopAsync(string[] args, CancellationToken cancellation) { _launcher.Stop(); return Task.CompletedTask; }
}

partial class OpcServer
{
	private sealed class Server : StandardServer
	{
		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration) =>
			new MasterNodeManager(server, configuration, null, [new MyNodeManager(this.ServerInternal, this.Configuration)]);

		protected override void OnNodeManagerStarted(IServerInternal server)
		{
			base.OnNodeManagerStarted(server);
		}
	}
}