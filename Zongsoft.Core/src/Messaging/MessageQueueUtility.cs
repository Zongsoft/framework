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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Messaging;

public static class MessageQueueUtility
{
	public static IMessageQueue Queue(string name, IEnumerable<KeyValuePair<string, string>> settings = null) => Queue(null, name, settings);
	public static IMessageQueue Queue(IServiceProvider services, string name, IEnumerable<KeyValuePair<string, string>> settings = null)
	{
		name ??= string.Empty;
		services ??= ApplicationContext.Current?.Services ?? throw new ArgumentNullException(nameof(services));

		foreach(var provider in services.ResolveAll<IMessageQueueProvider>())
		{
			if(provider.Exists(name))
				return provider.Queue(name, settings);
		}

		return null;
	}
}
