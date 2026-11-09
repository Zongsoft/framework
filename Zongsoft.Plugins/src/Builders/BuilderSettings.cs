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

/// <summary>表示构建设置的类。</summary>
public class BuilderSettings
{
	#region 构造函数
	public BuilderSettings(Type targetType)
	{
		this.TargetType = targetType;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置构建的结果类型。</summary>
	public Type TargetType { get; set; }
	#endregion
}
