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

namespace Zongsoft.Messaging.ZeroMQ;

[Service<IMessageQueueFactory>]
public sealed class ZeroQueueFactory() : MessageQueueFactoryBase(Configuration.ZeroConnectionSettingsDriver.NAME)
{
	public override IMessageQueue Create(IMessageQueueSettings settings) => new ZeroQueue(settings.Name, settings as Configuration.ZeroConnectionSettings);
	public override IMessageQueue Create(string name, string connectionString) => new ZeroQueue(name ?? string.Empty, Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings(connectionString));
}
