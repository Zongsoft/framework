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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using Zongsoft.Services;

namespace Zongsoft.Messaging.Mqtt;

[Service<IMessageQueueFactory>]
public sealed class MqttQueueFactory() : MessageQueueFactoryBase(Configuration.MqttConnectionSettingsDriver.NAME)
{
	public override IMessageQueue Create(IMessageQueueSettings settings) => new MqttQueue(settings.Name, settings as Configuration.MqttConnectionSettings);
	public override IMessageQueue Create(string name, string connectionString) => new MqttQueue(name ?? string.Empty, Configuration.MqttConnectionSettingsDriver.Instance.GetSettings(connectionString));
}
