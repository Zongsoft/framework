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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Polly library.
 *
 * The Zongsoft.Externals.Polly is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Polly is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Polly library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Externals.Polly;

public sealed class FeaturePipelineManager : IFeaturePipelineManager
{
	#region 单例字段
	public static readonly FeaturePipelineManager Instance = new();
	#endregion

	#region 私有字段
	private readonly ConcurrentDictionary<object, FeaturePipeline> _pipelines = new(Comparer.Instance);
	#endregion

	#region 私有构造
	private FeaturePipelineManager() { }
	#endregion

	#region 公共属性
	public string Name => "Polly";
	#endregion

	#region 公共方法
	public IFeaturePipeline GetPipeline(IEnumerable<IFeature> features) => this.GetPipeline(null, features);
	public IFeaturePipeline GetPipeline(object identifier, IEnumerable<IFeature> features)
	{
		if(features == null || (features.TryGetNonEnumeratedCount(out var count) && count == 0))
			return null;

		if(identifier == null)
			return new FeaturePipeline(features);

		return _pipelines.GetOrAdd(identifier, key => new FeaturePipeline(features));
	}
	#endregion

	#region 嵌套子类
	private sealed class Comparer : IEqualityComparer<object>
	{
		public static readonly Comparer Instance = new();

		public new bool Equals(object x, object y)
		{
			if(x == null && y == null)
				return true;
			if(x == null || y == null)
				return false;

			if(x is string a && y is string b)
				return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

			return x.Equals(y);
		}

		public int GetHashCode(object obj) => obj is string text ? text.ToUpperInvariant().GetHashCode() : obj.GetHashCode();
	}
	#endregion
}