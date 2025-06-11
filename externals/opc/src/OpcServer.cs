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
	private DateTime? _started;
	private OpcServerOptions _options;
	private ApplicationInstance _launcher;
	#endregion

	#region 构造函数
	public OpcServer(OpcServerOptions options = null) : base(options?.Name)
	{
		_options = options;
		this.Storages = new();
		this.Channels = new();
		this.Authenticator = Security.Authenticator.Default;
	}

	public OpcServer(string name, OpcServerOptions options = null) : base(name)
	{
		_options = options;
		this.Storages = new();
		this.Channels = new();
		this.Authenticator = Security.Authenticator.Default;
	}
	#endregion

	#region 公共属性
	public TimeSpan Elapsed => _started.HasValue ? Common.DateTimeExtension.GetElapsed(_started.Value) : TimeSpan.Zero;
	public Security.IAuthenticator Authenticator { get; set; }
	public OpcServerOptions Options => _options ??= new OpcServerOptions(this.Name);
	public StorageCollection Storages { get; }
	public ChannelCollection Channels { get; }
	#endregion

	#region 内部属性
	internal ApplicationConfiguration Configuration => _launcher.ApplicationConfiguration;
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
		protected override void OnServerStarted(IServerInternal server)
		{
			//设置服务器的启动时间
			_server._started = DateTime.UtcNow;

			server.SessionManager.SessionCreated += this.SessionManager_SessionCreated;
			server.SessionManager.SessionClosing += this.SessionManager_SessionClosing;
			server.SessionManager.ImpersonateUser += this.SessionManager_ImpersonateUser;
			base.OnServerStarted(server);
		}

		protected override void OnServerStopping()
		{
			//清空所有会话通道
			_server.Channels.Clear();

			//注销所有相关事件处理
			this.ServerInternal.SessionManager.SessionCreated -= this.SessionManager_SessionCreated;
			this.ServerInternal.SessionManager.SessionClosing -= this.SessionManager_SessionClosing;
			this.ServerInternal.SessionManager.ImpersonateUser -= this.SessionManager_ImpersonateUser;

			//重置服务器的启动时间
			_server._started = null;

			//调用基类同名方法
			base.OnServerStopping();
		}

		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
		{
			if(_server.Options.Storages.Count == 0)
				_server.Options.Storages.Add(new OpcServerOptions.StorageOptions("http://zongsoft.com/opc/ua"));

			foreach(var option in _server.Options.Storages)
				_server.Storages.Add(new Storage(server, configuration, option));

			return new MasterNodeManager(server, configuration, null, [.. _server.Storages.Select(storage => storage.Manager)]);
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

		#region 事件处理
		private void SessionManager_SessionCreated(Session session, SessionEventReason reason)
		{
			_server.Channels.Add(session);
		}

		private void SessionManager_SessionClosing(Session session, SessionEventReason reason)
		{
			_server.Channels.Remove(session?.Id.ToString());
		}

		private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
		{
			if(!this.CanAuthenticate(args.NewIdentity))
			{
				args.IdentityValidationError = new ServiceResult(StatusCodes.BadIdentityTokenRejected);
				return;
			}

			try
			{
				Security.AuthenticationIdentity identity = args.NewIdentity switch
				{
					UserNameIdentityToken user => new Security.AuthenticationIdentity.Account(user.UserName, user.DecryptedPassword),
					X509IdentityToken x509 => new Security.AuthenticationIdentity.Certificate(x509.Certificate),
					_ => throw new Zongsoft.Security.SecurityException(),
				};

				//执行身份验证
				var authenticated = _server.Authenticator.AuthenticateAsync(identity).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

				if(authenticated)
					args.Identity = new UserIdentity(args.NewIdentity);
				else
					args.IdentityValidationError = new ServiceResult(StatusCodes.BadSecurityChecksFailed);
			}
			catch(Exception ex)
			{
				args.IdentityValidationError = new ServiceResult(StatusCodes.BadSecurityChecksFailed, ex);
			}
		}
		#endregion

		#region 私有方法
		private bool CanAuthenticate(UserIdentityToken token)
		{
			var configuration = _server?.Configuration;

			if(configuration == null)
				return false;

			var tokenType = GetIdentityType(token);

			if(token == null)
				return configuration.ServerConfiguration.UserTokenPolicies.Find(policy => policy.TokenType == tokenType) != null;
			else
				return _server?.Authenticator != null && configuration.ServerConfiguration.UserTokenPolicies.Find(policy => policy.TokenType == tokenType) != null;
		}

		private static UserTokenType GetIdentityType(UserIdentityToken token)
		{
			if(token == null)
				return UserTokenType.Anonymous;

			return token switch
			{
				UserNameIdentityToken => UserTokenType.UserName,
				X509IdentityToken => UserTokenType.Certificate,
				IssuedIdentityToken => UserTokenType.IssuedToken,
				_ => throw new Zongsoft.Security.SecurityException(),
			};
		}
		#endregion
	}
}