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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

public abstract partial class Prefab
{
	#region 构造函数
	protected Prefab(string @namespace, string name, string label = null, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Namespace = @namespace ?? string.Empty;
		this.Label = label;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public abstract PrefabKind Kind { get; }
	public string Name { get; }
	public string Namespace { get; }
	public string Label { get; set; }
	public string Description { get; set; }
	#endregion

	#region 内部属性
	internal NodeState Node { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Kind}]{this.Name}";
	#endregion
}

public static partial class PrefabExtension { }
