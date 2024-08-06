/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Components
{
	public abstract class EventSubscriptionProviderBase<TArgument> : IEventSubscriptionProvider, IMatchable
	{
		#region 成员字段
		private readonly HashSet<string> _names;
		#endregion

		#region 构造函数
		protected EventSubscriptionProviderBase(params string[] names) => _names = new(names ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		protected EventSubscriptionProviderBase(IEnumerable<string> names) => _names = new(names ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 匹配方法
		bool IMatchable.Match(object parameter) => this.OnMatch(parameter);
		protected virtual bool OnMatch(object parameter) => _names == null || _names.Count == 0 || _names.Contains(string.Empty) || _names.Contains("*") || (parameter is string name && _names.Contains(name));
		#endregion

		#region 获取方法
		IAsyncEnumerable<IEventSubscription> IEventSubscriptionProvider.GetSubscriptionsAsync(string qualifiedName, object argument, Collections.Parameters parameters, CancellationToken cancellation) => GetSubscriptionsAsync(qualifiedName, Convert(argument), parameters, cancellation);
		public abstract IAsyncEnumerable<IEventSubscription> GetSubscriptionsAsync(string qualifiedName, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
		#endregion

		#region 参数转换
		protected virtual TArgument Convert(object argument) => Common.Convert.TryConvertValue<TArgument>(argument, out var value) ? value : default;
		#endregion
	}
}