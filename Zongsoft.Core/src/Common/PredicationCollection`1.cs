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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Zongsoft.Common;

public class PredicationCollection<T> : Collection<IPredication<T>>, IPredication<T>
{
	#region 成员字段
	private readonly PredicationCombination _combination;
	#endregion

	#region 构造函数
	public PredicationCollection() : this(PredicationCombination.And) { }
	public PredicationCollection(PredicationCombination combination) => _combination = combination;
	#endregion

	#region 公共属性
	/// <summary>获取或设置断言集合内各断言的逻辑组合方式。</summary>
	public PredicationCombination Combination => _combination;
	#endregion

	#region 参数转换
	protected virtual bool OnConvert(object parameter, out T result) => Zongsoft.Common.Convert.TryConvertValue<T>(parameter, out result);
	#endregion

	#region 断言方法
	ValueTask<bool> IPredication.PredicateAsync(object argument, CancellationToken cancellation) =>
		this.OnConvert(argument, out var stronglyArgument) ? ValueTask.FromResult(false) : this.PredicateAsync(stronglyArgument, cancellation);
	ValueTask<bool> IPredication.PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) =>
		this.OnConvert(argument, out var stronglyArgument) ? ValueTask.FromResult(false) : this.PredicateAsync(stronglyArgument, parameters, cancellation);

	public ValueTask<bool> PredicateAsync(T argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
	public async ValueTask<bool> PredicateAsync(T argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		var predications = base.Items;

		if(predications == null || predications.Count < 1)
			return true;

		foreach(var predication in predications)
		{
			if(predication == null)
				continue;

			if(await predication.PredicateAsync(argument, parameters, cancellation))
			{
				if(_combination == PredicationCombination.Or)
					return true;
			}
			else
			{
				if(_combination == PredicationCombination.And)
					return false;
			}
		}

		return _combination != PredicationCombination.Or;
	}
	#endregion
}
