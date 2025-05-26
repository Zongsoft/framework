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

using Zongsoft.Common;

namespace Zongsoft.Externals.Opc;

partial class Prefab
{
	public class VariablePrefab : Prefab
	{
		private object _value;

		public VariablePrefab(string name, Type type, string label = null, string description = null) : this(null, name, type, label, description) { }
		public VariablePrefab(FolderPrefab folder, string name, Type type, string label = null, string description = null) : base(name, label, description)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
			this.Folder = folder;
		}

		public override PrefabKind Kind => PrefabKind.Variable;
		public FolderPrefab Folder { get; }
		public new Type Type { get; }
		public object Value
		{
			get => _value;
			set => _value = value != null && Common.Convert.TryConvertValue(value, this.Type, out var result) ? result : null;
		}

		public override string ToString() => this.Value == null ?
			$"[{this.Kind}]{this.Name}@{this.Type.GetAlias()}" :
			$"[{this.Kind}]{this.Name}@{this.Type.GetAlias()}={this.Value}";
	}
}

partial class PrefabExtension
{
	public static Prefab.VariablePrefab Variable(this Prefab.FolderPrefab folder, string name, Type type, string label = null, string description = null) => Variable(folder, name, type, null, label, description);
	public static Prefab.VariablePrefab Variable(this Prefab.FolderPrefab folder, string name, Type type, object value, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));

		var result = new Prefab.VariablePrefab(folder, name, type, label, description) { Value = value };
		folder.Children.Add(result);
		return result;
	}

	public static Prefab.VariablePrefab Variable<T>(this Prefab.FolderPrefab folder, string name, string label = null, string description = null) => Variable<T>(folder, name, default, label, description);
	public static Prefab.VariablePrefab Variable<T>(this Prefab.FolderPrefab folder, string name, T value, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));

		var result = new Prefab.VariablePrefab(folder, name, typeof(T), label, description) { Value = value };
		folder.Children.Add(result);
		return result;
	}
}
