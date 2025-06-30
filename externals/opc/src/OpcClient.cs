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
using System.Collections.Concurrent;

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc;

public partial class OpcClient : IDisposable
{
	#region 事件定义
	public event EventHandler<HeartbeatEventArgs> Heartbeat;
	#endregion

	#region 成员字段
	private Session _session;
	private OpcClientState _state;
	private ApplicationConfiguration _configuration;
	private Configuration.OpcConnectionSettings _settings;
	private SubscriberCollection _subscribers;
	#endregion

	#region 构造函数
	public OpcClient(string name = null)
	{
		this.Name = string.IsNullOrEmpty(name) ? "Zongsoft.OpcClient" : name;

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
				TrustedUserCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
				},
				TrustedPeerCertificates = new CertificateTrustList()
				{
					StoreType = @"Directory",
					StorePath = @"certificates",
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

		//挂载证书验证事件
		_configuration.CertificateValidator.CertificateValidation += this.OnCertificateValidation;

		//验证客户端配置
		_configuration.Validate(ApplicationType.Client);

		_subscribers = new SubscriberCollection();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public Configuration.OpcConnectionSettings Settings => _settings;
	public bool IsConnected => _session?.Connected ?? false;
	public OpcClientState State => _state ?? OpcClientState.Empty;
	public SubscriberCollection Subscribers => _subscribers;
	#endregion

	#region 公共方法
	public ValueTask ConnectAsync(string settings, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(settings))
			throw new ArgumentNullException(nameof(settings));

		return this.ConnectAsync(Configuration.OpcConnectionSettingsDriver.Instance.GetSettings(settings), cancellation);
	}

	public async ValueTask ConnectAsync(Configuration.OpcConnectionSettings settings, CancellationToken cancellation = default)
	{
		if(settings == null || string.IsNullOrEmpty(settings.Server))
			throw new ArgumentNullException(nameof(settings));

		var endpointConfiguration = EndpointConfiguration.Create(_configuration);
		var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration, settings.Server, false, 1000 * 10);

		endpointDescription.SecurityMode = Utility.GetSecurityMode(settings.SecurityMode);
		if(endpointDescription.SecurityMode == MessageSecurityMode.Sign || endpointDescription.SecurityMode == MessageSecurityMode.SignAndEncrypt)
			endpointDescription.SecurityPolicyUri = $"http://opcfoundation.org/UA/SecurityPolicy#{(string.IsNullOrEmpty(settings.SecurityPolicy) ? "Basic256Sha256" : settings.SecurityPolicy)}";

		var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

		var name = string.IsNullOrEmpty(settings.Client) ?
			string.IsNullOrEmpty(settings.Instance) ? $"{this.Name}#{Common.Randomizer.GenerateString()}" : $"{this.Name}.{settings.Instance}" :
			string.IsNullOrEmpty(settings.Instance) ? settings.Client : $"{settings.Client}:{settings.Instance}";

		var locales = string.IsNullOrEmpty(settings.Locales) ? ["en"] : settings.Locales.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var identity = settings.GetIdentity();

		//断开原有会话
		await this.DisconnectAsync(cancellation);

		//创建新的会话
		var session = await Session.Create(
			_configuration,
			endpoint,
			false,
			name,
			(uint)settings.Timeout.TotalMilliseconds,
			identity,
			locales,
			cancellation);

		//保存当前连接设置
		_settings = settings;

		//构建会话状态信息
		_state = new OpcClientState(session);

		//设置会话相关信息
		session.DeleteSubscriptionsOnClose = true;
		session.KeepAliveInterval = (int)settings.Heartbeat.TotalMilliseconds;
		session.KeepAlive += this.Session_KeepAlive;

		if(!session.Connected)
			await session.OpenAsync(name, identity, cancellation);

		//保存当前为新建会话
		_session = session;
	}

	public async ValueTask DisconnectAsync(CancellationToken cancellation = default)
	{
		var session = _session;
		if(session == null || !session.Connected)
			return;

		try
		{
			foreach(var subscriber in _subscribers)
				await subscriber.DisposeAsync();
		}
		catch { }

		var status = await session.CloseAsync(cancellation);

		if(StatusCode.IsGood(status))
			session.KeepAlive -= this.Session_KeepAlive;
	}

	public ValueTask<bool> ExistsAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var session = this.GetSession();
		return ValueTask.FromResult(session.NodeCache.Exists(NodeId.Parse(identifier)));
	}

	public async ValueTask<Type> GetDataTypeAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var id = NodeId.Parse(identifier);
		var session = this.GetSession();

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var response = await session.ReadAsync(
			request, 0, TimestampsToReturn.Server,
			[
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.DataType,
				},
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.ValueRank,
				},
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.ArrayDimensions,
				},
			], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to get the data type of the “{identifier}” node.");

		if(response.Results.Count < 2)
			return null;

		var elementType = response.Results[0].GetDataType();
		var rank = response.Results[1].GetValueOrDefault<int>();
		return rank > 0 ? elementType.MakeArrayType(rank) : elementType;
	}

	public async ValueTask<object> GetValueAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var id = NodeId.Parse(identifier);
		var session = this.GetSession();
		var result = await ReadValueAsync(session, id, cancellation);

		if(result == null)
			return null;

		if(StatusCode.IsBad(result.StatusCode))
			throw new InvalidOperationException($"[{result.StatusCode}] Failed to read the value of the “{identifier}” node.");

		if(result.Value is ExtensionObject extension)
			return extension.Body;

		return result.Value;

		static async ValueTask<DataValue> ReadValueAsync(Session session, NodeId id, CancellationToken cancellation)
		{
			try
			{
				return await session.ReadValueAsync(id, cancellation);
			}
			catch(ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdUnknown)
			{
				return null;
			}
		}
	}

	public async IAsyncEnumerable<KeyValuePair<string, object>> GetValuesAsync(IEnumerable<string> identifiers, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		var session = this.GetSession();
		NodeId[] nodes = [..identifiers.Select(NodeId.Parse)];

		(var result, var failures) = await session.ReadValuesAsync(nodes, cancellation);

		if(result.Count != nodes.Length)
			yield break;

		for(int i = 0; i < nodes.Length; i++)
		{
			var entry = result[i];

			if(StatusCode.IsGood(entry.StatusCode))
				yield return new(nodes[i].ToString(), entry.Value is ExtensionObject extension ? extension.Body : entry.Value);
			else
				yield return new(nodes[i].ToString(), new Failure((int)entry.StatusCode.Code, entry.StatusCode.ToString()));
		}
	}

	public async ValueTask<bool> SetValueAsync<T>(string identifier, T value, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var session = this.GetSession();
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow
		};

		var response = await session.WriteAsync(
			request,
			[
				new WriteValue()
				{
					NodeId = NodeId.Parse(identifier),
					Value = new DataValue(new Variant(value), StatusCodes.Good),
					AttributeId = Attributes.Value,
				}
			],
			cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to write the value of the “{identifier}” node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0] == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(StatusCode.IsBad);

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to write the value of the “{identifier}” node.");
		}

		return true;
	}

	public async ValueTask<Failure[]> SetValuesAsync(IEnumerable<KeyValuePair<string, object>> entries, CancellationToken cancellation = default)
	{
		if(entries == null)
			throw new ArgumentNullException(nameof(entries));

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var nodes = entries.Select(entry => new WriteValue()
		{
			NodeId = NodeId.Parse(entry.Key),
			Value = new DataValue(new Variant(entry.Value)),
		});

		var session = this.GetSession();
		var response = await session.WriteAsync(request, [..nodes], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to write node value.");

		if(response.Results != null && response.Results.Count > 0)
			return response.Results.Where(StatusCode.IsBad).Select(Failure.GetFailure).ToArray();

		return null;
	}

	public async ValueTask<bool> CreateFolderAsync(string name, string description = null, CancellationToken cancellation = default)
	{
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var session = this.GetSession();
		var root = await session.NodeCache.FindAsync(ObjectIds.ObjectsFolder, cancellation);

		var folderNode = new AddNodesItem()
		{
			BrowseName = new QualifiedName(name),
			NodeClass = NodeClass.Object,
			ReferenceTypeId = ReferenceTypes.Organizes,
			TypeDefinition = ObjectTypeIds.FolderType,
			NodeAttributes = new ExtensionObject(new ObjectAttributes()
			{
				DisplayName = new LocalizedText(name),
				Description = new LocalizedText(description),
				EventNotifier = EventNotifiers.None,
				WriteMask = (uint)AttributeWriteMask.None,
				UserWriteMask = (uint)AttributeWriteMask.None,
				SpecifiedAttributes = (uint)(NodeAttributesMask.DisplayName | NodeAttributesMask.Description | NodeAttributesMask.EventNotifier | NodeAttributesMask.WriteMask | NodeAttributesMask.UserWriteMask),
			}),
		};

		var response = await session.AddNodesAsync(request, [folderNode], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to create the folder node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to create the folder node.");
		}

		return true;
	}

	public async ValueTask<bool> CreateVariableAsync(string name, Type type, string label, string description = null, CancellationToken cancellation = default)
	{
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var variableNode = new AddNodesItem
		{
			ReferenceTypeId = ReferenceTypes.HasComponent,
			RequestedNewNodeId = null,
			BrowseName = new QualifiedName(name),
			NodeClass = NodeClass.Variable,
			TypeDefinition = VariableTypeIds.BaseDataVariableType,
			NodeAttributes = new ExtensionObject(new VariableAttributes()
			{
				DisplayName = label,
				Description = description,
				DataType = Utility.GetDataType(type, out var rank),
				ValueRank = rank,
				ArrayDimensions = [],
				AccessLevel = AccessLevels.CurrentReadOrWrite,
				UserAccessLevel = AccessLevels.CurrentReadOrWrite,
				MinimumSamplingInterval = 0,
				Historizing = false,
				WriteMask = (uint)AttributeWriteMask.None,
				UserWriteMask = (uint)AttributeWriteMask.None,
				SpecifiedAttributes = (uint)NodeAttributesMask.All,
			}),
		};

		var session = this.GetSession();
		var response = await session.AddNodesAsync(request, [variableNode], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to create the variable node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to create the variable node.");
		}

		return true;
	}

	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, consumer, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, SubscriberOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, options, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<string> identifiers, SubscriberOptions options, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers?.Select(id => new KeyValuePair<string, object>(id, null)), options, consumer, cancellation);

	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, null, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, null, consumer, cancellation);
	public ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, SubscriberOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(identifiers, options, null, cancellation);
	public async ValueTask<Subscriber> SubscribeAsync(IEnumerable<KeyValuePair<string, object>> identifiers, SubscriberOptions options, Action<Subscriber, Subscriber.Entry, object> consumer, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		//确保待订阅的条目未被订阅
		var entries = identifiers
			.Where(entry => !string.IsNullOrEmpty(entry.Key) && !Exists(entry.Key))
			.DistinctBy(entry => entry.Key)
			.Select(entry => new Subscriber.Entry(entry.Key, entry.Value))
			.ToArray();

		//如果待订阅的条目为空则退出
		if(entries.Length == 0)
			return null;

		var session = this.GetSession();
		var subscriber = new Subscriber(options, consumer);

		for(int i = 0; i < entries.Length; i++)
			subscriber.Entries.Add(entries[i]);

		if(session.AddSubscription((Subscription)subscriber.Subscription) && await _subscribers.RegisterAsync(subscriber, cancellation))
			return subscriber;

		return null;

		bool Exists(string identifier)
		{
			foreach(var subscriber in _subscribers)
			{
				if(subscriber.Entries.Contains(identifier))
					return true;
			}

			return false;
		}
	}
	#endregion

	#region 事件处理
	private void OnCertificateValidation(CertificateValidator validator, CertificateValidationEventArgs args)
	{
		args.Accept = args.AcceptAll = true;
	}

	private void Session_KeepAlive(ISession session, KeepAliveEventArgs args) =>
		this.Heartbeat?.Invoke(this, StatusCode.IsGood(args.Status.StatusCode) ? new(args.CurrentState.ToString()) : new(Failure.GetFailure(args.Status.StatusCode), args.CurrentState.ToString()));
	#endregion

	#region 私有方法
	private Session GetSession() => _session ?? throw new InvalidOperationException($"The {nameof(OpcClient)}({this.Name}) is not connected.");
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		var session = _session;

		if(session != null && !session.Disposed)
		{
			session.KeepAlive -= this.Session_KeepAlive;
			session.Dispose();
		}

		_configuration = null;
	}
	#endregion
}
