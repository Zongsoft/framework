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

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据命令的元数据类。</summary>
public class DataCommandBase<TScriptor> : IDataCommand where TScriptor : IDataCommandScriptor
{
	#region 成员字段
	private string _alias;
	#endregion

	#region 构造函数
	protected DataCommandBase(string @namespace, string name, string alias = null) : this(@namespace, name, DataCommandMutability.None, alias) { }
	protected DataCommandBase(string @namespace, string name, DataCommandMutability mutability, string alias = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Namespace = @namespace;
		this.Name = name;
		this.Alias = alias;
		this.Mutability = mutability;
		this.QualifiedName = DataUtility.Qualify(this.Name, this.Namespace);
		this.Parameters = new();
	}
	#endregion

	#region 公共属性
	/// <summary>获取所属命名空间。</summary>
	public string Namespace { get; }

	/// <summary>获取数据命令的名称。</summary>
	public string Name { get; }

	/// <summary>获取数据命令的限定名称。</summary>
	public string QualifiedName { get; }

	/// <summary>获取或设置数据命令的类型。</summary>
	public DataCommandType Type { get; set; }

	/// <summary>获取或设置数据命令的别名（表名、存储过程名）。</summary>
	/// <remarks>注意：如果 <see cref="Namespace"/> 属性不为空(<c>null</c>)或空字符串，则该属性默认为：<c>{Namespace}_{Name}</c>。</remarks>
	public string Alias { get => _alias; set => _alias = value ?? (string.IsNullOrEmpty(this.Namespace) ? value : $"{this.Namespace}_{this.Name}"); }

	/// <summary>获取或设置数据命令的变化性。</summary>
	public DataCommandMutability Mutability { get; set; }

	/// <summary>获取数据命令的参数集合。</summary>
	public DataCommandParameterCollection Parameters { get; }

	/// <summary>获取数据命令的脚本对象。</summary>
	public TScriptor Scriptor { get; protected init; }
	#endregion

	#region 显式实现
	IDataCommandScriptor IDataCommand.Scriptor => this.Scriptor;
	#endregion

	#region 重写方法
	public override string ToString() => this.Mutability == DataCommandMutability.None ?
		$"{this.QualifiedName}({(this.Parameters.Count > 0 ? "..." : null)})" :
		$"{this.QualifiedName}({(this.Parameters.Count > 0 ? "..." : null)}):{this.Mutability}";
	#endregion
}
