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

/// <summary>表示按名称注册的 Profile 指令实现。</summary>
/// <remarks>实例在多次加载间共享；每次执行的可变状态应保存在上下文或局部变量中。</remarks>
public abstract class ProfileDirectiveBase
{
	#region 构造函数
	protected ProfileDirectiveBase(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if(!ProfileUtility.IsDirectiveName(name))
			throw new ArgumentException(Properties.Resources.Profiles_DirectiveNameInvalid_Message, nameof(name));

		this.Name = name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令名称，不包含注释标记和 <c>@</c>。</summary>
	public string Name { get; }
	#endregion

	#region 公共方法
	/// <summary>处理本次指令，正常返回后由调度逻辑标记为已处理。</summary>
	/// <param name="context">本次指令上下文。</param>
	public abstract void Process(ProfileDirectiveContext context);
	#endregion
}
