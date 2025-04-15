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
 * Copyright (C) 2020-2023 Zongsoft Studio <http://www.zongsoft.com>
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