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

namespace Zongsoft.Messaging;

public abstract class MessageQueueFactoryBase(string name) : IMessageQueueFactory, IMatchable, IMatchable<string>
{
	#region 公共属性
	public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
	#endregion

	#region 构建方法
	public abstract IMessageQueue Create(IMessageQueueSettings settings);
	public abstract IMessageQueue Create(string name, string connectionString);
	#endregion

	#region 服务匹配
	protected virtual bool OnMatch(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	bool IMatchable.Match(object argument) => this.OnMatch(argument as string);
	bool IMatchable<string>.Match(string argument) => this.OnMatch(argument);
	#endregion
}
