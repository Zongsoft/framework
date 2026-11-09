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

namespace Zongsoft.Externals.Opc;

partial class Prefab
{
	public class FolderPrefab : Prefab
	{
		internal FolderPrefab(FolderPrefab folder, string @namespace, string name, string label = null, string description = null) : base(@namespace, name, label, description)
		{
			this.Folder = folder;
			this.Children = new(this);
		}

		public override PrefabKind Kind => PrefabKind.Folder;
		public FolderPrefab Folder { get; }
		public PrefabCollection Children { get; }
	}
}

partial class PrefabExtension
{
	public static Prefab.FolderPrefab Folder(this Prefab.FolderPrefab folder, string name, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));

		var result = new Prefab.FolderPrefab(folder, folder.Namespace, name, label, description);
		folder.Children.Add(result);
		return result;
	}
}