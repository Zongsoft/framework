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
