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

using System.Collections.Generic;

namespace Zongsoft.Components;

/// <summary>提供当前调用环境的运行时特性。</summary>
/// <remarks>与 <see cref="IFeatureBuilder"/> 的执行策略配置独立；返回的特性必须绑定已捕获的环境。</remarks>
public interface IFeatureProvider
{
	/// <summary>在当前调用流中捕获环境特性。</summary>
	/// <returns>已绑定环境的特性集合；没有特性时返回空集合。同一延期条件应复用同一个实例。</returns>
	IEnumerable<IFeature> GetFeatures();
}
