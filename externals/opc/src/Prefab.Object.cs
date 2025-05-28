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

namespace Zongsoft.Externals.Opc;

partial class Prefab
{
	public class ObjectPrefab : Prefab
	{
		internal ObjectPrefab(FolderPrefab folder, string @namespace, string name, Type type, string label = null, string description = null) : base(@namespace, name, label, description)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			this.Type = Prefab.Type(@namespace, type);
			this.Folder = folder;
		}

		internal ObjectPrefab(FolderPrefab folder, string @namespace, string name, TypePrefab type, string label = null, string description = null) : base(@namespace, name, label, description)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
			this.Folder = folder;
		}

		public override PrefabKind Kind => PrefabKind.Object;
		public FolderPrefab Folder { get; }
		public new TypePrefab Type { get; }
		public object Value { get; set; }
	}
}

partial class PrefabExtension
{
	public static Prefab.ObjectPrefab Object(this Prefab.FolderPrefab folder, string name, Type type, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		var result = new Prefab.ObjectPrefab(folder, folder.Namespace, name, type, label, description);
		folder.Children.Add(result);
		return result;
	}

	public static Prefab.ObjectPrefab Object(this Prefab.FolderPrefab folder, string name, Prefab.TypePrefab type, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		var result = new Prefab.ObjectPrefab(folder, folder.Namespace, name, type, label, description);
		folder.Children.Add(result);
		return result;
	}
}