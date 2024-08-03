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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	/// <summary>
	/// 提供全局事件管理功能的类。
	/// </summary>
	public sealed class EventManager : IEnumerable<EventDescriptor>
	{
		#region 单例字段
		/// <summary>获取全局事件管理器的单例对象。</summary>
		public static readonly EventManager Global = new();
		#endregion

		#region 私有构造
		private EventManager()
		{
			this.Events = new EventCollectionView();
			this.Filters = new List<IFilter<EventContext>>();
		}
		#endregion

		#region 公共属性
		/// <summary>获取事件描述器集合。</summary>
		public EventCollectionView Events { get; }

		/// <summary>获取事件过滤器集合。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public ICollection<IFilter<EventContext>> Filters { get; }

		/// <summary>获取指定名称的事件描述器。</summary>
		/// <param name="name">指定的事件名称。</param>
		/// <returns>返回对应名称的事件描述器。</returns>
		public EventDescriptor this[string name] => this.Events[name];
		#endregion

		#region 遍历枚举
		public IEnumerator<EventDescriptor> GetEnumerator() => this.Events.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.Events.GetEnumerator();
		#endregion

		#region 嵌套子类
		public class EventCollectionView : IReadOnlyCollection<EventDescriptor>
		{
			#region 内部构造
			internal EventCollectionView() { }
			#endregion

			#region 公共属性
			public int Count => Components.Events.GetCount();
			public EventDescriptor this[string name] => Components.Events.GetEvent(name);
			#endregion

			#region 重写方法
			public IEnumerator<EventDescriptor> GetEnumerator() => Components.Events.GetEvents().GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			#endregion
		}
		#endregion
	}
}