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

namespace Zongsoft.Data.Archiving;

/// <summary>表示数据文件格式化器的接口。</summary>
/// <remarks>数据文件生成器会对每条数据生成目标记录时候会调用该接口的 <see cref="Format(object, ModelPropertyDescriptor)"/> 格式化方法，并将其格式化的结果作为最终结果写入到目标记录的相应单元内。</remarks>
public interface IDataArchiveFormatter
{
	/// <summary>格式化指定的数据属性值。</summary>
	/// <param name="target">指定的待格式化的目标对象。</param>
	/// <param name="property">指定的待格式化的目标属性。</param>
	/// <returns>返回格式化后的属性值。</returns>
	object Format(object target, ModelPropertyDescriptor property);
}