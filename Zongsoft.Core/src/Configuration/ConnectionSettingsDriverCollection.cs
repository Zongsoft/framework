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

namespace Zongsoft.Configuration;

public class ConnectionSettingsDriverCollection() : KeyedCollection<string, IConnectionSettingsDriver>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(IConnectionSettingsDriver driver) => driver.Name;
	protected override void InsertItem(int index, IConnectionSettingsDriver driver)
	{
		if(driver != null && !this.Contains(driver.Name))
			base.InsertItem(index, driver);
	}
}