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

namespace Zongsoft.Components
{
	public abstract class EventLocatorBase<TContext> : IHandlerLocator<TContext> where TContext : class
	{
		#region 构造函数
		protected EventLocatorBase() { }
		protected EventLocatorBase(EventRegistryBase registry) => this.Registry = registry ?? throw new ArgumentNullException(nameof(registry));
		#endregion

		#region 公共属性
		/// <summary>获取或设置事件注册表。</summary>
		public EventRegistryBase Registry { get; set; }
		#endregion

		#region 公共方法
		public virtual IHandler Locate(TContext context) =>
			this.Registry.Events.TryGetValue(GetName(context), out var descriptor) ? new EventHandler(descriptor) : null;
		#endregion

		#region 抽象方法
		/// <summary>根据上下文对象获取对应的事件名。</summary>
		/// <param name="context">指定的上下文对象。</param>
		/// <returns>如果成功则返回对应的事件名，否则返回空(null)。</returns>
		protected abstract string GetName(TContext context);
		#endregion

		#region 嵌套结构
		private class EventHandler : IHandler
		{
			private readonly EventDescriptor _descriptor;
			public EventHandler(EventDescriptor descriptor) => _descriptor = descriptor;
			public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
			public ValueTask HandleAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation = default) => _descriptor.HandleAsync(argument, parameters, cancellation);
		}
		#endregion
	}
}