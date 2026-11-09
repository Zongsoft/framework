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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public class FeatureBuilder : IFeatureBuilder, IEnumerable<IFeature>
{
	#region 构造函数
	public FeatureBuilder(params IEnumerable<IFeature> features)
	{
		this.Features = [.. features];
	}
	#endregion

	#region 公共属性
	public List<IFeature> Features { get; }
	#endregion

	#region 公共方法
	public void Reset() => this.Features.Clear();
	#endregion

	#region 显式实现
	IEnumerable<IFeature> IFeatureBuilder.Build() => this.Features;
	IEnumerator IEnumerable.GetEnumerator() => this.Features.GetEnumerator();
	public IEnumerator<IFeature> GetEnumerator() => this.Features.GetEnumerator();
	#endregion
}
