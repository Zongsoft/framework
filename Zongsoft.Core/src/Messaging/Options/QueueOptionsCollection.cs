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

namespace Zongsoft.Messaging.Options;

public class QueueOptionsCollection : KeyedCollection<string, QueueOptions>
{
	protected override string GetKeyForItem(QueueOptions item) => item.Name;
	protected override void InsertItem(int index, QueueOptions item)
	{
		if(item == null || item.Name == null)
			return;

		base.InsertItem(index, item);
	}
}
