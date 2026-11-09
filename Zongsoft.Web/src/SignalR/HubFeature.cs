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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Web.SignalR;

public class HubFeature
{
	#region 成员字段
	private readonly HashSet<HubDescriptor> _descriptors = new();
	#endregion

	#region 公共属性
	public ICollection<HubDescriptor> Hubs => _descriptors;
	#endregion

	#region 公共方法
	public bool Contains(TypeInfo type) => type != null && _descriptors.Any(descriptor => descriptor.Type == type);
	#endregion
}