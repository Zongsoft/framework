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
using System.Collections.ObjectModel;

namespace Zongsoft.Components;

public class EventDescriptorCollection : KeyedCollection<string, EventDescriptor>
{
	#region 成员字段
	private readonly EventRegistryBase _registry;
	#endregion

	#region 构造函数
	internal EventDescriptorCollection(EventRegistryBase registry) : base(StringComparer.OrdinalIgnoreCase, 3) => _registry = registry;
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(EventDescriptor descriptor) => descriptor.Name;
	protected override void InsertItem(int index, EventDescriptor descriptor)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		base.InsertItem(index, descriptor);
		descriptor.SetRegistry(_registry);
	}
	protected override void SetItem(int index, EventDescriptor descriptor)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		base.SetItem(index, descriptor);
		descriptor.SetRegistry(_registry);
	}
	#endregion
}