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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public class Subscriber : IEquatable<Subscriber>
	{
		#region 构造函数
		internal Subscriber(Subscription subscription)
		{
			this.Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			this.Identifier = subscription.Id.ToString();

			if(subscription.MonitoredItemCount > 0)
				this.Entries = [.. subscription.MonitoredItems.Select(item => new Entry(item.StartNodeId.ToString(), null, item.DisplayName))];
			else
				this.Entries = [];
		}
		#endregion

		#region 公共属性
		public object Subscription { get; }
		public string Identifier { get; }
		public EntryCollection Entries { get; }
		#endregion

		#region 重写方法
		public bool Equals(Subscriber other) => other is not null && string.Equals(this.Identifier, other.Identifier, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => this.Equals(obj as Subscriber);
		public override int GetHashCode() => HashCode.Combine(this.Identifier);
		public override string ToString() => this.Identifier;
		#endregion

		public class Entry : IEquatable<Entry>
		{
			#region 构造函数
			public Entry(string name, Type type, string label, string description = null)
			{
				this.Name = name;
				this.Type = type;
				this.Label = label;
				this.Description = description;
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			public Type Type { get; set; }
			public string Label { get; set; }
			public string Description { get; set; }
			#endregion

			#region 重写方法
			public bool Equals(Entry other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => this.Equals(obj as Entry);
			public override int GetHashCode() => HashCode.Combine(this.Name);
			public override string ToString() => this.Name;
			#endregion
		}

		public class EntryCollection : KeyedCollection<string, Entry>
		{
			#region 构造函数
			public EntryCollection(params IEnumerable<Entry> monitors) : base(StringComparer.OrdinalIgnoreCase)
			{
				if(monitors == null)
				{
					foreach(var monitor in monitors)
						this.Add(monitor);
				}
			}
			#endregion

			#region 公共方法
			public bool TryRemove(string key, out Entry monitor)
			{
				if(key != null && this.TryGetValue(key, out monitor))
					return this.Remove(key);

				monitor = null;
				return false;
			}
			#endregion

			#region 重写方法
			protected override string GetKeyForItem(Entry item) => item.Name;
			#endregion
		}
	}

	public class SubscriberCollection : KeyedCollection<string, Subscriber>
	{
		public bool TryRemove(string key, out Subscriber subscriber)
		{
			if(key != null && this.TryGetValue(key, out subscriber))
				return this.Remove(key);

			subscriber = null;
			return false;
		}

		protected override string GetKeyForItem(Subscriber item) => item.Identifier;
	}
}
