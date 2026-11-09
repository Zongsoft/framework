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

namespace Zongsoft.Data;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public class ModelAttribute : Attribute
{
	#region 构造函数
	public ModelAttribute(bool immutable = false) => this.Immutable = immutable;
	public ModelAttribute(string name, bool immutable = false) : this(name, null, immutable) { }
	public ModelAttribute(string name, string alias, bool immutable = false)
	{
		this.Name = name;
		this.Alias = alias;
		this.Immutable = immutable;
	}
	#endregion

	#region 公共属性
	/// <summary>获取模型名称。</summary>
	public string Name { get; }
	/// <summary>获取或设置模型别名。</summary>
	public string Alias { get; set; }
	/// <summary>获取或设置一个值，指示是否为不可变模型。</summary>
	public bool Immutable { get; set; }
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Name))
			return string.IsNullOrEmpty(this.Alias) ? $"({GetDescription(this.Immutable)})" : $":{this.Alias}({GetDescription(this.Immutable)})";
		else
			return string.IsNullOrEmpty(this.Alias) ? $"{this.Name}({GetDescription(this.Immutable)})" : $"{this.Name}:{this.Alias}({GetDescription(this.Immutable)})";

		static string GetDescription(bool immutable) => immutable ? "immutabled" : "mutabled";
	}
	#endregion
}
