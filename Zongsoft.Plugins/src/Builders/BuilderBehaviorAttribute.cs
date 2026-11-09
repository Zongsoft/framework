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

namespace Zongsoft.Plugins.Builders;

/// <summary>提供构建器行为约定的特性类。</summary>
/// <remarks>在特定情况建议使用该类对构建器进行定制。</remarks>
[AttributeUsage(AttributeTargets.Class)]
public class BuilderBehaviorAttribute : Attribute
{
	#region 成员字段
	private Type _valueType;
	#endregion

	#region 构造函数
	public BuilderBehaviorAttribute(Type valueType) => _valueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
	#endregion

	#region 公共属性
	public Type ValueType
	{
		get => _valueType;
		set => _valueType = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion
}
