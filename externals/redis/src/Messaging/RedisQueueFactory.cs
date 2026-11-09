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

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Externals.Redis.Messaging;

[Service<IMessageQueueFactory>]
public sealed class RedisQueueFactory() : MessageQueueFactoryBase(Configuration.RedisConnectionSettingsDriver.NAME)
{
	public override IMessageQueue Create(IMessageQueueSettings settings) => new RedisQueue(settings.Name, settings as Configuration.RedisConnectionSettings);
	public override IMessageQueue Create(string name, string connectionString) => new RedisQueue(name ?? string.Empty, Configuration.RedisConnectionSettingsDriver.Instance.GetSettings(connectionString));
}
