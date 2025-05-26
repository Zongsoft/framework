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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Opc;

public class PrefabCollection : KeyedCollection<string, Prefab>
{
	public PrefabCollection(string @namespace) : base(StringComparer.OrdinalIgnoreCase)
	{
		if(string.IsNullOrEmpty(@namespace))
			throw new ArgumentNullException(nameof(@namespace));

		this.Namespace = @namespace;
	}

	public PrefabCollection(Prefab.FolderPrefab folder = null) : base(StringComparer.OrdinalIgnoreCase)
	{
		this.Folder = folder ?? throw new ArgumentNullException(nameof(folder));
		this.Namespace = folder.Namespace;
	}

	public string Namespace { get; }
	public Prefab.FolderPrefab Folder { get; }
	protected override string GetKeyForItem(Prefab prefab) => prefab.Name;
}

public static class PrefabCollectionExtension
{
	public static Prefab.FolderPrefab Folder(this PrefabCollection prefabs, string name, string label = null, string description = null)
	{
		if(prefabs == null)
			throw new ArgumentNullException(nameof(prefabs));

		var result = new Prefab.FolderPrefab(prefabs.Folder, prefabs.Namespace, name, label, description);
		prefabs.Add(result);
		return result;
	}

	public static Prefab.ObjectPrefab Object(this PrefabCollection prefabs, string name, Prefab.TypePrefab type, string label = null, string description = null) => Object(prefabs, name, type, null, label, description);
	public static Prefab.ObjectPrefab Object(this PrefabCollection prefabs, string name, Prefab.TypePrefab type, object value, string label = null, string description = null)
	{
		if(prefabs == null)
			throw new ArgumentNullException(nameof(prefabs));

		var result = new Prefab.ObjectPrefab(prefabs.Folder, prefabs.Namespace, name, type, label, description) { Value = value };
		prefabs.Add(result);
		return result;
	}

	public static Prefab.VariablePrefab Variable(this PrefabCollection prefabs, string name, Type type, string label = null, string description = null) => Variable(prefabs, name, type, null, label, description);
	public static Prefab.VariablePrefab Variable(this PrefabCollection prefabs, string name, Type type, object value, string label = null, string description = null)
	{
		if(prefabs == null)
			throw new ArgumentNullException(nameof(prefabs));

		var result = new Prefab.VariablePrefab(prefabs.Folder, prefabs.Namespace, name, type, label, description) { Value = value };
		prefabs.Add(result);
		return result;
	}

	public static Prefab.VariablePrefab Variable<T>(this PrefabCollection prefabs, string name, string label = null, string description = null) => Variable<T>(prefabs, name, default, label, description);
	public static Prefab.VariablePrefab Variable<T>(this PrefabCollection prefabs, string name, T value, string label = null, string description = null)
	{
		if(prefabs == null)
			throw new ArgumentNullException(nameof(prefabs));

		var result = new Prefab.VariablePrefab(prefabs.Folder, prefabs.Namespace, name, typeof(T), label, description) { Value = value };
		prefabs.Add(result);
		return result;
	}
}