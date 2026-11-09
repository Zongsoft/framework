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

namespace Zongsoft.Plugins;

public class BuilderElement : FixedElement<IBuilder>
{
	#region 构造函数
	public BuilderElement(string typeName, string name, Plugin plugin) : base(typeName, name, plugin, FixedElementType.Builder)
	{
	}

	public BuilderElement(Type type, string name, Plugin plugin) : base(type, name, plugin, FixedElementType.Builder)
	{
	}
	#endregion
}
