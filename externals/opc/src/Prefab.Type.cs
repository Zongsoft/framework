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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Externals.Opc;

partial class Prefab
{
	public static TypePrefab Type(string @namespace, Type type, string label = null, string description = null) => new(@namespace, type.GetAlias(), type, label, description);
	public static TypePrefab Type<T>(string @namespace, string label = null, string description = null) => new(@namespace, typeof(T).GetAlias(), typeof(T), label, description);
	public static TypePrefab Type(string @namespace, string name, Type type, string label = null, string description = null) => new(@namespace, name, type, label, description);
	public static TypePrefab Type<T>(string @namespace, string name, string label = null, string description = null) => new(@namespace, name, typeof(T), label, description);

	public class TypePrefab : Prefab
	{
		internal TypePrefab(string @namespace, string name, Type type, string label = null, string description = null) : base(@namespace, name, label, description)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override PrefabKind Kind => PrefabKind.Type;
		public new Type Type { get; }
	}
}
