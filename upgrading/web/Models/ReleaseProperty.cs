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

namespace Zongsoft.Upgrading.Models;

/// <summary>表示发布属性的实体类。</summary>
public abstract class ReleaseProperty
{
	#region 公共属性
	/// <summary>获取或设置发布编号。</summary>
	public abstract uint ReleaseId { get; set; }
	/// <summary>获取或设置属性名称。</summary>
	public abstract string Name { get; set; }
	/// <summary>获取或设置属性类型。</summary>
	public abstract string Type { get; set; }
	/// <summary>获取或设置属性值。</summary>
	public abstract string Value { get; set; }
	/// <summary>获取或设置所属发布。</summary>
	public abstract Release Release { get; set; }
	#endregion
}
