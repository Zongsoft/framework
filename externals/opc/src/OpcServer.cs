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
	private OpcServerOptions _options;
	private ApplicationInstance _launcher;
	#endregion

	#region 构造函数
	public OpcServer(OpcServerOptions options = null) : base(options?.Name)
	{
		_options = options;
		this.Storages = new();
	}

	public OpcServer(string name, OpcServerOptions options = null) : base(name)
	{
		_options = options;
		this.Storages = new();
	}
	#endregion

	#region 公共属性
	public Security.IAuthenticator Authenticator { get; set; }
	public OpcServerOptions Options => _options ??= new OpcServerOptions(this.Name);
	public StorageCollection Storages { get; }
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
			DisableCertificateAutoCreation = false,
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

	#region 公共方法
	public Type GetDataType(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return null;

		var id = NodeId.Parse(identifier);
		var storage = this.Storages.Find(id.NamespaceIndex);
		return storage?.Manager.GetDataType(id);
	}

	public bool TryGetValue(string identifier, out object value)
	{
		value = null;

		if(string.IsNullOrEmpty(identifier))
			return false;

		var id = NodeId.Parse(identifier);
		var storage = this.Storages.Find(id.NamespaceIndex);
		return storage != null && storage.Manager.TryGetValue(id, out value);
	}

	public IEnumerable<KeyValuePair<string, object>> GetValues(IEnumerable<string> identifiers)
	{
		if(identifiers == null)
			return [];

		var result = new List<KeyValuePair<string, object>>();
		var groups = identifiers.Select(NodeId.Parse).GroupBy(id => id.NamespaceIndex);

		foreach(var group in groups)
		{
			var storage = this.Storages.Find(group.Key);

			if(storage != null)
				result.AddRange(storage.Manager.GetValues(group).Select(entry => new KeyValuePair<string, object>(entry.Key.ToString(), entry.Value)));
		}

		return result;
	}

	public bool SetValue<T>(string identifier, T value)
	{
		if(string.IsNullOrEmpty(identifier))
			return false;

		var id = NodeId.Parse(identifier);
		var storage = this.Storages.Find(id.NamespaceIndex);
		return storage != null && storage.Manager.SetValue(id, value);
	}

	public Failure[] SetValues(IEnumerable<KeyValuePair<string, object>> entries)
	{
		if(entries == null)
			return [];

		var result = new List<Failure>();
		var groups = entries
			.Select(entry => new KeyValuePair<NodeId, object>(NodeId.Parse(entry.Key), entry.Value))
			.GroupBy(entry => entry.Key.NamespaceIndex);

		foreach(var group in groups)
		{
			var storage = this.Storages.Find(group.Key);

			if(storage != null)
				result.AddRange(storage.Manager.SetValues(group));
		}

		return [.. result];
	}
	#endregion
}

partial class OpcServer
{
	private sealed class Server(OpcServer server) : StandardServer
	{
		#region 成员字段
		private readonly OpcServer _server = server;
		#endregion

		#region 重写方法
		protected override void OnServerStarting(ApplicationConfiguration configuration)
		{
			base.OnServerStarting(configuration);
		}

		protected override void OnServerStarted(IServerInternal server)
		{
			server.SessionManager.ImpersonateUser += this.SessionManager_ImpersonateUser;
			base.OnServerStarted(server);
		}

		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
		{
			if(_server.Options.Storages.Count == 0)
				_server.Options.Storages.Add(new OpcServerOptions.StorageOptions("http://zongsoft.com/opc/ua"));

			foreach(var option in _server.Options.Storages)
				_server.Storages.Add(new Storage(server, configuration, option));

			return new MasterNodeManager(server, configuration, null, [.. _server.Storages.Select(storage => storage.Manager)]);
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

				var succeed = true;
				var storage = (Storage)null;
				results = new AddNodesResultCollection(nodes.Count);
				diagnostics = new DiagnosticInfoCollection(nodes.Count);

				foreach(var node in nodes)
				{
					if(node == null || node.RequestedNewNodeId == null || node.RequestedNewNodeId.IsNull)
						storage = _server.Storages[0];
					else
						storage = string.IsNullOrEmpty(node.RequestedNewNodeId.NamespaceUri) ?
							_server.Storages.Find(node.RequestedNewNodeId.NamespaceIndex) :
							_server.Storages.Find(node.RequestedNewNodeId.NamespaceUri);

					if(storage == null)
					{
						succeed = false;

						results.Add(new AddNodesResult()
						{
							AddedNodeId = new NodeId(node.RequestedNewNodeId.Identifier, node.RequestedNewNodeId.NamespaceIndex),
							StatusCode = StatusCodes.BadNodeIdInvalid,
						});

						diagnostics.Add(new DiagnosticInfo()
						{
							NamespaceUri = node.RequestedNewNodeId.NamespaceIndex,
							InnerStatusCode = StatusCodes.BadNodeIdInvalid,
						});

						break;
					}

					(var result, var diagnostic) = storage.Manager.AddNode(context, node);

					succeed &= StatusCode.IsGood(result.StatusCode);

					if(result != null)
						results.Add(result);
					if(diagnostic != null)
						diagnostics.Add(diagnostic);
				}

				return this.CreateResponse(requestHeader, succeed ? StatusCodes.Good : StatusCodes.UncertainNotAllNodesAvailable);
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