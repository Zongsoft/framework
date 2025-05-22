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
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Configuration;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc;

public partial class OpcServer(string name = null) : WorkerBase(name)
{
	#region 成员字段
	private OpcServerOptions _options;
	private ApplicationInstance _launcher;
	#endregion

	#region 公共属性
	public Security.IAuthenticator Authenticator { get; set; }
	public OpcServerOptions Options { get => _options; init => _options = value; }
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		//确认服务配置对象
		_options ??= new OpcServerOptions(this.Name);

		//创建应用实例（服务启动器）
		_launcher = new ApplicationInstance()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Server,
			ApplicationConfiguration = _options.GetConfiguration(),
		};

		//必须：检查应用启动器的安全证书
		_launcher.CheckApplicationInstanceCertificates(false);

		//启动服务器实例
		return _launcher.Start(new Server(this));
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_launcher.Stop();
		return Task.CompletedTask;
	}
	#endregion
}

partial class OpcServer
{
	private sealed class Server(OpcServer server) : StandardServer
	{
		#region 成员字段
		private readonly OpcServer _server = server;
		private NodeManager _manager;
		#endregion

		#region 重写方法
		protected override void OnServerStarting(ApplicationConfiguration configuration) => base.OnServerStarting(configuration);
		protected override void OnServerStarted(IServerInternal server)
		{
			server.SessionManager.ImpersonateUser += this.SessionManager_ImpersonateUser;
			base.OnServerStarted(server);
		}

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

		#region 身份验证
		private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
		{
			var authenticator = _server.Authenticator;

			if(authenticator == null)
				return;

			try
			{
				args.Identity = authenticator.AuthenticateAsync(args.NewIdentity).AsTask().GetAwaiter().GetResult();
			}
			catch(Exception ex)
			{
				args.IdentityValidationError = new ServiceResult(StatusCodes.BadSecurityChecksFailed, ex);
			}
		}
		#endregion
	}
}