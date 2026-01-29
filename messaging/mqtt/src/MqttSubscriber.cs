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
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Client;

using Zongsoft.Components;

namespace Zongsoft.Messaging.Mqtt;

public class MqttSubscriber : MessageConsumerBase<MqttQueue>, IEquatable<MqttSubscriber>
{
	#region 构造函数
	public MqttSubscriber(MqttQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, handler, options)
	{
		this.Subscription = new();
	}
	#endregion

	#region 内部属性
	internal MqttClientSubscribeOptions Subscription { get; }
	#endregion

	#region 取消订阅
	protected override ValueTask OnCloseAsync(CancellationToken cancellation) => this.Queue.UnsubscribeAsync(this);
	#endregion

	#region 重写方法
	public bool Equals(MqttSubscriber other) => string.Equals(this.Topic, other.Topic);
	public override bool Equals(object obj) => obj is MqttSubscriber subscriber && this.Equals(subscriber);
	public override int GetHashCode() => HashCode.Combine(this.Queue, this.Topic);
	public override string ToString() => this.Tags != null && this.Tags.Length > 0 ? $"{this.Topic}:{string.Join(',', this.Tags)}" : this.Topic;
	#endregion
}
