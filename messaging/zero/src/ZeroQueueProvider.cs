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

[Service<IMessageQueueProvider, IServiceProvider<IMessageQueue>>(Configuration.ZeroConnectionSettingsDriver.NAME)]
public class ZeroQueueProvider() : MessageQueueProviderBase<ZeroQueue, Configuration.ZeroConnectionSettings>(Configuration.ZeroConnectionSettingsDriver.NAME), IServiceProvider<IMessageQueue>
{
	#region 显式实现
	IMessageQueue IServiceProvider<IMessageQueue>.GetService(string name) => this.Queue(name);
	#endregion
}
