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

namespace Zongsoft.Configuration.Profiles;

/// <summary>表示指定名称的 Profile 指令选项。</summary>
public partial class ProfileDirectiveOptions
{
	#region 构造函数
	public ProfileDirectiveOptions(string name, ProfileDirectiveBehavior behavior = ProfileDirectiveBehavior.None)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if(!ProfileUtility.IsDirectiveName(name))
			throw new ArgumentException(Properties.Resources.Profiles_DirectiveNameInvalid_Message, nameof(name));

		this.Name = name;
		this.Behavior = behavior;
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令名称，不包含注释符和 <c>@</c> 前缀。</summary>
	public string Name { get; }
	/// <summary>获取或设置指令行为；<see cref="ProfileDirectiveBehavior.None"/> 表示采用指令内置默认行为。</summary>
	public ProfileDirectiveBehavior Behavior
	{
		get => field;
		set => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}({this.Behavior})";
	#endregion

	#region 公共方法
	/// <summary>复制选项，保留实际派生类型。</summary>
	/// <returns>返回独立的选项副本。</returns>
	/// <remarks>默认浅复制实例字段；包含可变引用成员的派生类型应重写此方法，复制其可变成员。</remarks>
	public virtual ProfileDirectiveOptions Clone() => (ProfileDirectiveOptions)this.MemberwiseClone();
	#endregion
}
