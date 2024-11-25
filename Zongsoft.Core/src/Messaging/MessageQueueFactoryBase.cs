﻿/*
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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging;

public abstract class MessageQueueFactoryBase(string name) : IMessageQueueFactory, IMatchable, IMatchable<string>
{
	#region 公共属性
	public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
	#endregion

	#region 构建方法
	public abstract IMessageQueue Create(IConnectionSettings settings);
	#endregion

	#region 服务匹配
	protected virtual bool OnMatch(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	bool IMatchable.Match(object argument) => this.OnMatch(argument as string);
	bool IMatchable<string>.Match(string argument) => this.OnMatch(argument);
	#endregion
}