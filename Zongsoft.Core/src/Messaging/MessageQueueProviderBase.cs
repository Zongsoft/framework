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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging;

public abstract class MessageQueueProviderBase : IMessageQueueProvider
{
	#region 常量定义
	internal const string SETTINGS_PATH = "/Messaging/ConnectionSettings";
	#endregion

	#region 成员字段
	private readonly Dictionary<string, WeakReference<IMessageQueue>> _queues;
	#endregion

	#region 构造函数
	protected MessageQueueProviderBase(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		_queues = new Dictionary<string, WeakReference<IMessageQueue>>(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	#endregion

	#region 公共方法
	public virtual bool Exists(string name) => name != null && _queues.ContainsKey(name);
	public IMessageQueue Queue(IEnumerable<KeyValuePair<string, string>> settings = null) => this.Queue(string.Empty, settings);
	public IMessageQueue Queue(string name, IEnumerable<KeyValuePair<string, string>> settings = null)
	{
		if(name == null)
			name = string.Empty;

		if(_queues.TryGetValue(name, out var reference) && reference.TryGetTarget(out var queue))
			return queue;

		lock(_queues)
		{
			if(_queues.TryGetValue(name, out reference) && reference.TryGetTarget(out queue))
				return queue;

			//创建指定名称的消息队列，如果创建失败则抛出异常
			queue = this.OnCreate(name, settings) ?? throw new InvalidOperationException($"{this.Name}: The specified '{name}' message queue is not defined.");

			//以弱引用的方式
			_queues[name] = new WeakReference<IMessageQueue>(queue);
		}

		return queue;
	}
	#endregion

	#region 创建队列
	protected abstract IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings);
	#endregion

	#region 保护方法
	protected TSettings GetSettings<TSettings>(string name, IEnumerable<KeyValuePair<string, string>> settings) where TSettings : class, IMessageQueueSettings
	{
		var connectionSettings = ApplicationContext.Current?.Configuration.GetConnectionSettings(SETTINGS_PATH, name, this.Name);
		if(connectionSettings == null)
			throw new ConfigurationException($"The specified {this.Name} message queue connection setting named '{name}' was not found.");

		if(settings != null)
		{
			foreach(var setting in settings)
				connectionSettings[setting.Key] = setting.Value;
		}

		return connectionSettings as TSettings ?? throw new InvalidOperationException($"The specified '{SETTINGS_PATH}/{name}@{this.Name}' connection setting is not of the required '{typeof(TSettings).FullName}' settings type.");
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IMessageQueue> GetEnumerator()
	{
		var queues = _queues.Values;

		foreach(var queue in queues)
		{
			if(queue != null && queue.TryGetTarget(out var value))
				yield return value;
		}
	}
	#endregion
}

public abstract class MessageQueueProviderBase<TQueue, TSettings>(string name) : MessageQueueProviderBase(name)
	where TQueue : class, IMessageQueue
	where TSettings : class, IMessageQueueSettings
{
	public override bool Exists(string name)
	{
		var connectionSettings = ApplicationContext.Current?.Configuration.GetOption<ConnectionSettingsCollection>(SETTINGS_PATH);
		return connectionSettings != null && connectionSettings.Contains(name, this.Name);
	}

	protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
	{
		return (IMessageQueue)Activator.CreateInstance(typeof(TQueue), name, this.GetSettings<TSettings>(name, settings));
	}
}
