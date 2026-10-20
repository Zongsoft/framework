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
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;

using Zongsoft.Services;

namespace Zongsoft.Externals.Opc;

public partial class OpcClient : IAsyncDisposable
{
	#region 事件定义
	public event EventHandler<HeartbeatEventArgs> Heartbeat;
	#endregion

	#region 成员字段
	private readonly SemaphoreSlim _lifecycle = new(1, 1);
	private TaskCompletionSource _disposal;
	private ISession _session;
	private OpcClientState _state;
	private DefaultSessionFactory _sessionFactory;
	private ApplicationConfiguration _configuration;
	private Configuration.OpcConnectionSettings _settings;
	private SubscriberCollection _subscribers;
	private string[] _namespaces;
	#endregion

	#region 构造函数
	public OpcClient(string name = null)
	{
		this.Name = string.IsNullOrEmpty(name) ? "Zongsoft.OpcClient" : name;
		_subscribers = new SubscriberCollection();
		_sessionFactory = new(null);

		_configuration = new ApplicationConfiguration()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Client,
			ProductUri = ApplicationContext.Current?.Name,

			SecurityConfiguration = new SecurityConfiguration
			{
				AutoAcceptUntrustedCertificates = false,
				AddAppCertToTrustedStore = true,
				UseValidatedCertificates = false,
				RejectSHA1SignedCertificates = false,

				ApplicationCertificate = new CertificateIdentifier
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
					SubjectName = $"CN={this.Name}, DC={Environment.MachineName}",
				},
				TrustedIssuerCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
				},
				TrustedPeerCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
				},
				TrustedUserCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
				},
				UserIssuerCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
				},
				RejectedCertificateStore = new CertificateTrustList
				{
					StoreType = @"Directory",
					StorePath = @"certificates/blocked"
				},
			},
			ClientConfiguration = new ClientConfiguration()
			{
				DefaultSessionTimeout = 60 * 1000,
				OperationLimits = new OperationLimits() { },
			},
			TransportQuotas = new TransportQuotas()
			{
				OperationTimeout = 60 * 1000,
			},
		};

	}
	#endregion

	#region 公共属性
	/// <summary>获取客户端名称。</summary>
	public string Name { get; }
	/// <summary>获取客户端的连接设置。</summary>
	public Configuration.OpcConnectionSettings Settings => _settings;
	/// <summary>获取一个值，指示客户端是否已经连接。</summary>
	public bool IsConnected => _session?.Connected ?? false;
	/// <summary>获取客户端的状态。</summary>
	public OpcClientState State => _state ?? OpcClientState.Empty;
	/// <summary>获取订阅者集合。</summary>
	public SubscriberCollection Subscribers => _subscribers;
	/// <summary>获取连接的服务器端的命名空间集。</summary>
	public string[] Namespaces => _namespaces ??= _session?.NamespaceUris.ToArray();
	#endregion

	#region 事件处理
	private void Session_KeepAlive(ISession session, KeepAliveEventArgs args)
	{
		this.Heartbeat?.Invoke(this,
			IsGood(args, out var failure) ?
			new(args.CurrentState.ToString()) :
			new(failure, args.CurrentState.ToString()));

		static bool IsGood(KeepAliveEventArgs args, out Failure failure)
		{
			if(args.Status == null)
			{
				if(args.CurrentState == ServerState.Running)
				{
					failure = default;
					return true;
				}

				failure = new(-1, args.CurrentState.ToString());
				return false;
			}

			if(StatusCode.IsGood(args.Status.StatusCode))
			{
				failure = default;
				return true;
			}

			failure = Failure.GetFailure(args.Status.StatusCode);
			return false;
		}
	}
	#endregion

	#region 私有方法
	private ISession GetSession()
	{
		ObjectDisposedException.ThrowIf(_disposal != null, this);
		return _session ?? throw new InvalidOperationException(string.Format(Properties.Resources.Opc_ClientNotConnected_Message, nameof(OpcClient), this.Name));
	}
	#endregion

	#region 处置方法
	/// <summary>等待订阅、会话和连接操作全部退出。</summary>
	/// <returns>共享的异步释放任务。</returns>
	public ValueTask DisposeAsync()
	{
		var completion = Volatile.Read(ref _disposal);
		if(completion == null)
		{
			var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			completion = Interlocked.CompareExchange(ref _disposal, candidate, null) ?? candidate;
			if(ReferenceEquals(completion, candidate))
				_ = this.DisposeCoreAsync(candidate);
		}

		return new(completion.Task);
	}

	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		await _lifecycle.WaitAsync();

		try
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
			completion.TrySetResult();
		}
		catch(Exception exception) { completion.TrySetException(exception); }
		finally { _lifecycle.Release(); }
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(!disposing)
			return;

		try { await this.DisconnectCoreAsync(CancellationToken.None); }
		finally { _configuration = null; }
	}
	#endregion

	#region 嵌套子类
	public class HeartbeatEventArgs : EventArgs
	{
		public HeartbeatEventArgs(string status = null) => this.Status = status;
		public HeartbeatEventArgs(Failure failure, string status = null)
		{
			this.Failure = failure;
			this.Status = status;
		}

		public Failure? Failure { get; }
		public string Status { get; set; }

		public override string ToString() => this.Failure.HasValue ? $"{this.Status}{this.Failure}" : this.Status;
	}
	#endregion
}
