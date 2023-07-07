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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	[System.Reflection.DefaultMember(nameof(Events))]
	public class EventRegistry
	{
		#region 构造函数
		public EventRegistry()
		{
			this.Events = new EventDescriptorCollection();
		}
		#endregion

		#region 公共属性
		public EventDescriptorCollection Events { get; }
		#endregion

		#region 公共方法
		public ValueTask RaiseAsync(string name, object request, CancellationToken cancellation = default) => this.RaiseAsync(name, request, null, cancellation);
		public ValueTask RaiseAsync(string name, object request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(this.Events.TryGetValue(name, out var descriptor) && descriptor != null)
				return descriptor.HandleAsync(request, parameters, cancellation);
			else
				return ValueTask.CompletedTask;
		}

		public ValueTask RaiseAsync<T>(string name, T request, CancellationToken cancellation = default) => this.RaiseAsync(name, request, null, cancellation);
		public ValueTask RaiseAsync<T>(string name, T request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(this.Events.TryGetValue(name, out var descriptor) && descriptor != null)
				return descriptor.HandleAsync(request, parameters, cancellation);
			else
				return ValueTask.CompletedTask;
		}
		#endregion
	}
}