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

public class SchemaMemberCollection<T> : KeyedCollection<string, T> where T : SchemaMemberBase
{
	public SchemaMemberCollection() : base(StringComparer.OrdinalIgnoreCase) { }
	public SchemaMemberCollection(IEnumerable<T> members) : base(StringComparer.OrdinalIgnoreCase)
	{
		if(members != null)
		{
			foreach(var member in members)
			{
				if(member != null)
					this.Add(member);
			}
		}
	}

	protected override string GetKeyForItem(T member) => member.Name;
}