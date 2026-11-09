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

namespace Zongsoft.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public class SerializationMemberAttribute : Attribute
{
	#region 构造函数
	public SerializationMemberAttribute() { }
	public SerializationMemberAttribute(string name)
	{
		this.Name = name == null ? string.Empty : name.Trim();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置序列化后的成员名称，如果为空(<c>null</c>)或空字符串则取对应的成员本身的名称。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置成员序列化方向。</summary>
	public SerializationDirection Direction { get; set; }

	/// <summary>获取或设置是否忽略序列化成员。</summary>
	public bool Ignored { get; set; }
	#endregion
}
