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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging;

[Service(typeof(IMessageQueueProvider))]
public class MessageQueueProvider : MessageQueueProviderBase
{
	#region 构造函数
	public MessageQueueProvider() : base("Aliyun.MNS") { }
	#endregion

	#region 公共方法
	public override bool Exists(string name)
	{
		var options = MessageUtility.GetOptions();
		return options != null && (options.Queues.Contains(name) || options.Topics.Contains(name));
	}

	protected override IMessageQueue OnCreate(string name, IEnumerable<KeyValuePair<string, string>> settings)
	{
		var options = MessageUtility.GetOptions();

		if(options.Queues.TryGetValue(name, out var queue))
			return new MessageQueue(queue.Name);
		if(options.Topics.TryGetValue(name, out var topic))
			return new MessageTopic(topic.Name);

		return null;
	}
	#endregion
}
