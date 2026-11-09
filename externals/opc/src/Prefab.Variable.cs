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

using Zongsoft.Common;

namespace Zongsoft.Externals.Opc;

partial class Prefab
{
	public class VariablePrefab : Prefab
	{
		#region 成员字段
		private object _value;
		#endregion

		#region 内部构造
		internal VariablePrefab(FolderPrefab folder, string @namespace, string name, Type type, string label = null, string description = null) : base(@namespace, name, label, description)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
			this.Folder = folder;
		}
		#endregion

		#region 公共属性
		public override PrefabKind Kind => PrefabKind.Variable;
		public FolderPrefab Folder { get; }
		public new Type Type { get; }
		public object Value
		{
			get => _value;
			set => _value = value != null && Common.Convert.TryConvertValue(value, this.Type, out var result) ? result : null;
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Value == null ?
			$"[{this.Kind}]{this.Name}@{this.Type.GetAlias()}" :
			$"[{this.Kind}]{this.Name}@{this.Type.GetAlias()}={this.Value}";
		#endregion
	}
}

partial class PrefabExtension
{
	public static Prefab.VariablePrefab Variable(this Prefab.FolderPrefab folder, string name, Type type, string label = null, string description = null) => Variable(folder, name, type, null, label, description);
	public static Prefab.VariablePrefab Variable(this Prefab.FolderPrefab folder, string name, Type type, object value, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));

		var result = new Prefab.VariablePrefab(folder, folder.Namespace, name, type, label, description) { Value = value };
		folder.Children.Add(result);
		return result;
	}

	public static Prefab.VariablePrefab Variable<T>(this Prefab.FolderPrefab folder, string name, string label = null, string description = null) => Variable<T>(folder, name, default, label, description);
	public static Prefab.VariablePrefab Variable<T>(this Prefab.FolderPrefab folder, string name, T value, string label = null, string description = null)
	{
		if(folder == null)
			throw new ArgumentNullException(nameof(folder));

		var result = new Prefab.VariablePrefab(folder, folder.Namespace, name, typeof(T), label, description) { Value = value };
		folder.Children.Add(result);
		return result;
	}
}
