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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Components;

/// <summary>组合特性提供者，在当前调用流中捕获运行时特性的快照。</summary>
/// <remarks>按引用去重提供者及特性，保留首次出现的顺序；提供者及其资源由装配方管理。</remarks>
public sealed class FeatureProvider : IFeatureProvider
{
	/// <summary>获取包含事务能力及当前应用服务扩展的默认提供者。</summary>
	/// <remarks>内置 <see cref="Data.Transaction.Features"/>，独立使用时同样有效；每次捕获时解析当前应用的 <see cref="IFeatureProvider"/> 服务。</remarks>
	public static FeatureProvider Default { get; } = new(Data.Transaction.Features);

	/// <summary>创建没有特性来源的组合提供者。</summary>
	public FeatureProvider() : this([]) { }

	/// <summary>创建指定来源的组合提供者。</summary>
	/// <param name="providers">构造时复制的来源集合；空集合表示没有特性来源。</param>
	/// <exception cref="ArgumentNullException"><paramref name="providers"/> 为 <see langword="null"/>。</exception>
	public FeatureProvider(params IFeatureProvider[] providers)
	{
		ArgumentNullException.ThrowIfNull(providers);
		this.Providers = new SynchronizedList<IFeatureProvider>((IEnumerable<IFeatureProvider>)providers);
	}

	/// <summary>获取可扩展的特性来源集合。</summary>
	/// <remarks>增删与枚举同步。空项被忽略；捕获回调中的修改只影响后续捕获。普通组合只包含显式来源，可加入 <see cref="Default"/> 以保留默认能力。</remarks>
	public ICollection<IFeatureProvider> Providers { get; }

	/// <summary>先固定组合中全部来源的引用快照，再捕获绑定环境的特性。</summary>
	/// <returns>按引用去重的特性快照；同一来源只调用一次，重复或循环的组合引用只展开一次。</returns>
	public IEnumerable<IFeature> GetFeatures()
	{
		var providers = new List<IFeatureProvider>();
		var visited = new HashSet<IFeatureProvider>(ReferenceEqualityComparer.Instance);

		Collect(this);

		return providers
			.SelectMany(provider => provider.GetFeatures() ?? [])
			.Distinct<IFeature>(ReferenceEqualityComparer.Instance)
			.ToArray();

		void Collect(IFeatureProvider provider)
		{
			if(provider == null || !visited.Add(provider))
				return;

			if(provider is FeatureProvider composite)
			{
				//通过枚举持有集合读锁，避免 Count 与 CopyTo 分别加锁形成竞态。
				var sources = new List<IFeatureProvider>();
				foreach(var source in composite.Providers)
					sources.Add(source);

				var services = object.ReferenceEquals(composite, Default) ? ApplicationContext.Current?.Services.ResolveAll<IFeatureProvider>()?.ToArray() : null;

				foreach(var source in sources)
					Collect(source);

				foreach(var service in services ?? [])
					Collect(service);
			}
			else
				providers.Add(provider);
		}
	}
}
