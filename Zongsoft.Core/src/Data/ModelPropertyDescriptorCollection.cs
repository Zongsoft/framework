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
using System.Collections.ObjectModel;

namespace Zongsoft.Data;

public class ModelPropertyDescriptorCollection(ModelDescriptor model) : KeyedCollection<string, ModelPropertyDescriptor>(StringComparer.OrdinalIgnoreCase)
{
	private readonly ModelDescriptor _model = model ?? throw new ArgumentNullException(nameof(model));

	internal int AddRange(IEnumerable<ModelPropertyDescriptor> properties)
	{
		if(properties == null)
			throw new ArgumentNullException(nameof(properties));

		int count = 0;

		foreach(var property in properties)
		{
			this.Add(property);
			count++;
		}

		return count;
	}

	protected override string GetKeyForItem(ModelPropertyDescriptor item) => item.Name;

	protected override void InsertItem(int index, ModelPropertyDescriptor item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		item.Model = _model;
		base.InsertItem(index, item);
	}

	protected override void SetItem(int index, ModelPropertyDescriptor item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		item.Model = _model;
		base.SetItem(index, item);
	}
}