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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
	public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get; private set; }
	#endregion

	#region 内部属性
	internal ApplicationConfiguration Configuration => _launcher?.ApplicationConfiguration;
	#endregion

	#region 重写方法
	protected override async Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		//确认服务配置对象
		_options ??= new OpcServerOptions(this.Name);

		//获取应用配置
		var configuration = _options.GetConfiguration();

		//验证应用配置
		await configuration.ValidateAsync(ApplicationType.Server, cancellation);

		//创建应用实例（服务启动器）
		_launcher = new ApplicationInstance((ITelemetryContext)null)
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Server,
			ApplicationConfiguration = configuration,
			DisableCertificateAutoCreation = false,
		};

		try
		{
			//必须：检查应用启动器的安全证书
			await _launcher.CheckApplicationInstanceCertificatesAsync(false, null, cancellation);

			//证书副本由服务器拥有，停止时释放。
			using(var certificate = await configuration.CertificateManager.CertificateProvider.GetPrivateKeyCertificateAsync(configuration.SecurityConfiguration.ApplicationCertificate, null, configuration.ApplicationUri, cancellation))
				this.Certificate = certificate?.AsX509Certificate2();

			//启动服务器实例
			await _launcher.StartAsync(new Server(this), cancellation);
		}
		catch(Exception failure)
		{
			try { await this.OnStopAsync(args, CancellationToken.None); }
			catch(Exception cleanup) { throw new AggregateException(failure, cleanup); }

			throw;
		}
	}

	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var launcher = Interlocked.Exchange(ref _launcher, null);

		try
		{
			if(launcher != null)
				await launcher.StopAsync();
		}
		finally
		{
			this.Certificate?.Dispose();
			this.Certificate = null;
		}
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
	private sealed class Server(OpcServer server) : StandardServer(null)
	{
		#region 成员字段
		private readonly OpcServer _server = server;
		#endregion

		#region 重写方法
		protected override ServerProperties LoadServerProperties() => new()
		{
			ManufacturerName = nameof(Zongsoft),
			ProductUri = "http://zongsoft.com/externals/opc",
			ProductName = $"{typeof(OpcServer).Namespace}.{nameof(OpcServer)}",
			SoftwareVersion = typeof(OpcServer).Assembly.GetName().Version.ToString(),
			BuildDate = System.IO.File.GetCreationTime(typeof(OpcServer).Assembly.Location),
		};

		protected override void OnServerStarted(IServerInternal server)
		{
			//设置服务器的启动时间
			_server._started = DateTime.UtcNow;

			server.SessionManager.SessionCreated += this.SessionManager_SessionCreated;
			server.SessionManager.SessionClosing += this.SessionManager_SessionClosing;
			foreach(var tokenType in Enum.GetValues<UserTokenType>())
				server.IdentityRegistry.Register(new AuthenticationAdapter(_server, tokenType));
			base.OnServerStarted(server);
		}

		protected override ValueTask OnServerStoppingAsync(CancellationToken cancellation)
		{
			//清空所有会话通道
			_server.Channels.Clear();

			//注销所有相关事件处理
			this.ServerInternal.SessionManager.SessionCreated -= this.SessionManager_SessionCreated;
			this.ServerInternal.SessionManager.SessionClosing -= this.SessionManager_SessionClosing;

			//重置服务器的启动时间
			_server._started = null;

			//调用基类同名方法
			return base.OnServerStoppingAsync(cancellation);
		}

		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
		{
			if(_server.Options.Storages.Count == 0)
				_server.Options.Storages.Add(new OpcServerOptions.StorageOptions("http://zongsoft.com/opc/ua"));

			foreach(var option in _server.Options.Storages)
				_server.Storages.Add(new Storage(server, configuration, option));

			return new MasterNodeManager(server, configuration, null, [.. _server.Storages.Select(storage => storage.Manager)]);
		}

		public override async ValueTask<AddNodesResponse> AddNodesAsync(SecureChannelContext secureChannelContext, RequestHeader requestHeader, ArrayOf<AddNodesItem> nodes, RequestLifetime requestLifetime)
		{
			using var context = await this.ValidateRequestAsync(secureChannelContext, requestHeader, RequestType.AddNodes, requestLifetime);

			try
			{
				if(nodes.Count == 0)
					throw new ServiceResultException(StatusCodes.BadNothingToDo);

				var succeed = true;
				var storage = (Storage)null;
				var results = new List<AddNodesResult>(nodes.Count);
				var diagnostics = new List<DiagnosticInfo>(nodes.Count);

				for(var index = 0; index < nodes.Count; index++)
				{
					var node = nodes[index];
					if(node == null || node.RequestedNewNodeId.IsNull)
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
							AddedNodeId = node.RequestedNewNodeId.InnerNodeId,
							StatusCode = StatusCodes.BadNodeIdInvalid,
						});

						diagnostics.Add(new DiagnosticInfo()
						{
							NamespaceUri = node.RequestedNewNodeId.NamespaceIndex,
							InnerStatusCode = StatusCodes.BadNodeIdInvalid,
						});

						break;
					}

					(var result, var diagnostic) = await storage.Manager.AddNodeAsync(context, node, requestLifetime.CancellationToken);

					succeed &= StatusCode.IsGood(result.StatusCode);

					if(result != null)
						results.Add(result);
					if(diagnostic != null)
						diagnostics.Add(diagnostic);
				}

				return new AddNodesResponse
				{
					ResponseHeader = this.CreateResponse(requestHeader, succeed ? StatusCodes.Good : StatusCodes.UncertainNotAllNodesAvailable),
					Results = [.. results],
					DiagnosticInfos = [.. diagnostics],
				};
			}
			catch(ServiceResultException ex)
			{
				this.ServerInternal.UpdateServerDiagnostics(diagnostics =>
				{
					diagnostics.RejectedRequestsCount++;

					if(this.IsSecurityError(ex.StatusCode))
					{
						diagnostics.SecurityRejectedRequestsCount++;
					}
				});

				throw this.TranslateException(context, ex);
			}
			finally
			{
				this.OnRequestComplete(context);
			}
		}
		#endregion

		#region 事件处理
		private void SessionManager_SessionCreated(ISession session, SessionEventReason reason)
		{
			_server.Channels.Add(session);
		}

		private void SessionManager_SessionClosing(ISession session, SessionEventReason reason)
		{
			_server.Channels.Remove(session?.Id.ToString());
		}
		#endregion
	}

	private sealed class AuthenticationAdapter(OpcServer server, UserTokenType tokenType) : global::Opc.Ua.Identity.IUserTokenAuthenticator
	{
		#region 成员字段
		private readonly bool _allowed = server.Options.GetConfiguration().ServerConfiguration.UserTokenPolicies.ToArray().Any(policy => policy.TokenType == tokenType);
		#endregion

		#region 公共属性
		public UserTokenType TokenType => tokenType;
		public string IssuedTokenProfileUri => null;
		#endregion

		#region 验证方法

		public async ValueTask<global::Opc.Ua.Identity.AuthenticationResult> AuthenticateAsync(global::Opc.Ua.Identity.AuthenticationContext context, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(!_allowed)
				return global::Opc.Ua.Identity.AuthenticationResult.Reject(new ServiceResult(StatusCodes.BadIdentityTokenRejected));

			Security.AuthenticationIdentity identity = null;

			try
			{
				identity = Security.AuthenticationIdentity.GetIdentity(context.TokenHandler);
				var authenticated = tokenType == UserTokenType.Anonymous ||
					identity != null && server.Authenticator != null && await server.Authenticator.AuthenticateAsync(server, identity, cancellation);
				cancellation.ThrowIfCancellationRequested();

				return authenticated ?
					global::Opc.Ua.Identity.AuthenticationResult.Accept(new UserIdentity(context.TokenHandler)) :
					global::Opc.Ua.Identity.AuthenticationResult.Reject(new ServiceResult(StatusCodes.BadSecurityChecksFailed));
			}
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { throw; }
			catch(Exception exception)
			{
				return global::Opc.Ua.Identity.AuthenticationResult.Reject(new ServiceResult(StatusCodes.BadSecurityChecksFailed, exception));
			}
			finally
			{
				if(identity is IDisposable disposable)
					disposable.Dispose();
			}
		}
		#endregion
	}
}
