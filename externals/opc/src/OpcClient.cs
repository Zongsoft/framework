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
using Zongsoft.Communication;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Opc;

public class OpcClient : IDisposable
{
	#region 成员字段
	private Session _session;
	private ApplicationConfiguration _configuration;
	private Configuration.OpcConnectionSettings _settings;
	private ConcurrentDictionary<string, MonitoredItem> _monitoredItems;
	#endregion

	#region 构造函数
	public OpcClient(string name = null)
	{
		this.Name = string.IsNullOrEmpty(name) ? "Opc Client" : name;

		_configuration = new ApplicationConfiguration()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Client,
			ProductUri = ApplicationContext.Current?.Name,

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

		//验证客户端配置
		_configuration.Validate(ApplicationType.Client);

		_monitoredItems = new ConcurrentDictionary<string, MonitoredItem>();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public Configuration.OpcConnectionSettings Settings => _settings;
	public bool IsConnected => _session?.Connected ?? false;
	public IEnumerable<Subscription> Subscriptions => _session?.Subscriptions ?? [];
	#endregion

	#region 公共方法
	public ValueTask ConnectAsync(string url, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(url))
			throw new ArgumentNullException(nameof(url));

		return this.ConnectAsync(Configuration.OpcConnectionSettingsDriver.Instance.GetSettings($"{nameof(Configuration.OpcConnectionSettings.Url)}={url}"), cancellation);
	}

	public async ValueTask ConnectAsync(Configuration.OpcConnectionSettings settings, CancellationToken cancellation = default)
	{
		if(settings == null || string.IsNullOrEmpty(settings.Url))
			throw new ArgumentNullException(nameof(settings));

		var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration, settings.Url, false, 1000 * 10);
		var endpointConfiguration = EndpointConfiguration.Create(_configuration);
		var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

		var name = this.Name;
		if(string.IsNullOrEmpty(settings.Client))
			;
		else
			name = string.IsNullOrEmpty(settings.Instance) ? settings.Client : $"{settings.Client}:{settings.Instance}";

		_session = await Session.Create(
			_configuration,
			endpoint,
			false,
			name,
			(uint)settings.Timeout.TotalMilliseconds,
			new UserIdentity(),
			["zh", "en"],
			cancellation);

		_settings = settings;
		_session.KeepAliveInterval = (int)settings.Heartbeat.TotalMilliseconds;
		_session.DeleteSubscriptionsOnClose = true;

		if(!_session.Connected)
			await _session.OpenAsync(this.Name, new UserIdentity(), cancellation);
	}

	public async ValueTask DisconnectAsync(CancellationToken cancellation = default)
	{
		var session = _session;
		if(session == null || !session.Connected)
			return;

		if(session.Subscriptions != null)
			await session.RemoveSubscriptionsAsync(session.Subscriptions, cancellation);

		await session.CloseAsync(cancellation);
	}

	public ValueTask<bool> ExistsAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		return ValueTask.FromResult(_session.NodeCache.Exists(NodeId.Parse(identifier)));
	}

	public async ValueTask<object> GetValueAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var id = NodeId.Parse(identifier);
		var result = await ReadValueAsync(_session, id, cancellation);

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

	public async ValueTask<(IEnumerable<object> result, IEnumerable<Failure> failures)> GetValuesAsync(IEnumerable<string> identifiers, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		(var result, var failures) = await _session.ReadValuesAsync([.. identifiers.Select(NodeId.Parse)], cancellation);

		return (
			result
				.Where(entry => StatusCode.IsGood(entry.StatusCode))
				.Select(entry => entry.Value is ExtensionObject extension ? extension.Body : entry.Value),
			failures
				.Where(failure => StatusCode.IsBad(failure.StatusCode))
				.Select(failure => new Failure((int)failure.Code, failure.SymbolicId, failure.ToString()))
		);
	}

	public async ValueTask<bool> SetValueAsync<T>(string identifier, T value, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow
		};

		var response = await _session.WriteAsync(
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

		var response = await _session.WriteAsync(request, [..nodes], cancellation);

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

		var root = await _session.NodeCache.FindAsync(ObjectIds.ObjectsFolder, cancellation);

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

		var response = await _session.AddNodesAsync(request, [folderNode], cancellation);

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
				DataType = Utility.GetDataType(type),
				ValueRank = ValueRanks.Scalar,
				ArrayDimensions = new UInt32Collection(),
				AccessLevel = AccessLevels.CurrentReadOrWrite,
				UserAccessLevel = AccessLevels.CurrentReadOrWrite,
				MinimumSamplingInterval = 0,
				Historizing = false,
				WriteMask = (uint)AttributeWriteMask.None,
				UserWriteMask = (uint)AttributeWriteMask.None,
				SpecifiedAttributes = (uint)NodeAttributesMask.All,
			}),
		};

		var response = await _session.AddNodesAsync(request, [variableNode], cancellation);

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

	public async ValueTask<bool> SubscribeAsync(IEnumerable<string> identifiers, SubscriptionOptions options, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		var subscription = new Subscription(_session.DefaultSubscription)
		{
			Handle = this,
			PublishingEnabled = true,
			PublishingInterval = options.GetPublishingInterval(),
			MinLifetimeInterval = options.GetMinLifetimeInterval(),
			KeepAliveCount = (uint)options.GetKeepAliveCount(),
			LifetimeCount = (uint)options.GetLifetimeCount(),
			TimestampsToReturn = TimestampsToReturn.Both,
		};

		subscription.StateChanged += this.Subscription_StateChanged;
		subscription.PublishStatusChanged += this.Subscription_PublishStatusChanged;

		var items = identifiers
			.Where(identifier => !string.IsNullOrEmpty(identifier) && !_monitoredItems.ContainsKey(identifier))
			.Select(identifier =>
		{
			var item = new MonitoredItem(subscription.DefaultItem)
			{
				Handle = identifier,
				StartNodeId = NodeId.Parse(identifier),
				AttributeId = Attributes.Value,
				SamplingInterval = options.GetSamplingInterval(),
				DiscardOldest = true,
				QueueSize = (uint)options.GetQueueSize(),
				DisplayName = identifier,
			};

			item.Notification += this.MonitoredItem_Notification;

			return item;
		});

		subscription.AddItems(items);

		if(subscription.MonitoredItemCount > 0 && _session.AddSubscription(subscription))
		{
			await subscription.CreateAsync(cancellation);
			await subscription.ApplyChangesAsync(cancellation);

			foreach(var item in subscription.MonitoredItems)
				_monitoredItems.TryAdd(item.StartNodeId.ToString(), item);

			return true;
		}

		return false;
	}

	public async ValueTask<int> UnsubscribeAsync(CancellationToken cancellation = default)
	{
		var session = _session;
		if(session == null || session.SubscriptionCount == 0)
			return 0;

		int count = 0;

		foreach(var subscription in session.Subscriptions)
		{
			foreach(var item in subscription.MonitoredItems)
			{
				item.Notification -= this.MonitoredItem_Notification;
				_monitoredItems.Remove(item.StartNodeId.ToString(), out _);
				count++;
			}

			subscription.RemoveItems(subscription.MonitoredItems);

			if(subscription.MonitoredItemCount == 0)
			{
				subscription.StateChanged -= this.Subscription_StateChanged;
				subscription.PublishStatusChanged -= this.Subscription_PublishStatusChanged;
			}
		}

		foreach(var subscription in session.Subscriptions)
			await subscription.DeleteAsync(false, cancellation);

		await session.RemoveSubscriptionsAsync(session.Subscriptions.ToArray(), cancellation);

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		//await session.DeleteSubscriptionsAsync(request, session.Subscriptions.Select(subscription => subscription.Id).ToArray(), cancellation);

		return count;
	}

	public async ValueTask<int> UnsubscribeAsync(IEnumerable<string> identifiers, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			return 0;

		var session = _session;
		if(session == null || session.SubscriptionCount == 0)
			return 0;

		var count = 0;

		foreach(var identifier in identifiers)
		{
			if(identifier != null && _monitoredItems.Remove(identifier, out var item))
			{
				item.Notification -= this.MonitoredItem_Notification;
				item.Subscription.RemoveItem(item);
				count++;
			}
		}

		var removables = new List<Subscription>();

		foreach(var subscription in session.Subscriptions)
		{
			if(subscription.MonitoredItemCount == 0)
			{
				subscription.StateChanged -= this.Subscription_StateChanged;
				subscription.PublishStatusChanged -= this.Subscription_PublishStatusChanged;
				removables.Add(subscription);
			}
		}

		if(removables != null && removables.Count > 0)
		{
			var request = new RequestHeader()
			{
				Timestamp = DateTime.UtcNow,
			};

			await session.RemoveSubscriptionsAsync(removables, cancellation);
			await session.DeleteSubscriptionsAsync(request, removables.Select(subscription => subscription.Id).ToArray(), cancellation);
		}

		return count;
	}
	#endregion

	#region 事件处理
	private void Subscription_StateChanged(Subscription subscription, SubscriptionStateChangedEventArgs args)
	{
		Console.WriteLine($"[Subscription.StateChanged] {args.Status}");
	}

	private void Subscription_PublishStatusChanged(Subscription subscription, PublishStateChangedEventArgs args)
	{
		Console.WriteLine($"[Subscription.PublishStatusChanged] {args.Status}");
	}

	private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
	{
		if(args.NotificationValue is MonitoredItemNotification notification)
			Console.WriteLine($"[Notification] {monitoredItem.StartNodeId} => {notification.Value}");
		else
			Console.WriteLine($"[Notification] {monitoredItem.StartNodeId} => {args.NotificationValue}");
	}
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
			session.Dispose();

		_configuration = null;
	}
	#endregion
}
