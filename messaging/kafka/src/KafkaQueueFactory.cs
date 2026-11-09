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
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka;

[Service<IMessageQueueFactory>]
public sealed class KafkaQueueFactory() : MessageQueueFactoryBase(Configuration.KafkaConnectionSettingsDriver.NAME)
{
	public override IMessageQueue Create(IMessageQueueSettings settings) => new KafkaQueue(settings.Name, settings as Configuration.KafkaConnectionSettings);
	public override IMessageQueue Create(string name, string connectionString) => new KafkaQueue(name ?? string.Empty, Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings(connectionString));
}
