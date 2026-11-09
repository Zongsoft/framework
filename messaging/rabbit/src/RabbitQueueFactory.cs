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

namespace Zongsoft.Messaging.RabbitMQ;

[Service<IMessageQueueFactory>]
public sealed class RabbitQueueFactory() : MessageQueueFactoryBase(RabbitQueue.NAME)
{
	public override IMessageQueue Create(IMessageQueueSettings settings) => new RabbitQueue(settings.Name, settings as Configuration.RabbitConnectionSettings);
	public override IMessageQueue Create(string name, string connectionString) => new RabbitQueue(name ?? string.Empty, Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings(connectionString));
}
