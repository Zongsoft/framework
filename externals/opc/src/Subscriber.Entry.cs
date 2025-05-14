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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class Subscriber
{
	public class Entry : IEquatable<Entry>
	{
		#region 构造函数
		public Entry(string name) : this(name, null, null, null) { }
		public Entry(string name, Type type, string label = null, string description = null)
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

		#region 内部属性
		internal MonitoredItem Monitor { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Entry other) => other is not null && string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => this.Equals(obj as Entry);
		public override int GetHashCode() => HashCode.Combine(this.Name);
		public override string ToString() => this.Type == null ? this.Name : $"{this.Name}@{Common.TypeAlias.GetAlias(this.Type)}";
		#endregion
	}

	public class EntryCollection : KeyedCollection<string, Entry>
	{
		#region 成员字段
		private readonly Subscriber _subscriber;
		private readonly Subscription _subscription;
		#endregion

		#region 构造函数
		public EntryCollection(Subscriber subscriber, params IEnumerable<Entry> monitors) : base(StringComparer.OrdinalIgnoreCase)
		{
			_subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
			_subscription = (Subscription)subscriber.Subscription;

			if(monitors == null)
			{
				foreach(var monitor in monitors)
					this.Add(monitor);
			}
		}
		#endregion

		#region 公共方法
		public bool Add(string name)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(this.Contains(name))
				return false;

			this.Add(new Entry(name));
			return true;
		}

		public bool TryRemove(string name, out Entry result)
		{
			if(name != null && this.TryGetValue(name, out result))
				return this.Remove(name);

			result = null;
			return false;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Entry entry) => entry.Name;
		protected override void SetItem(int index, Entry item) => throw new NotSupportedException();

		protected override void InsertItem(int index, Entry item)
		{
			if(item == null || string.IsNullOrEmpty(item.Name))
				throw new ArgumentNullException(nameof(item));

			var monitored = new MonitoredItem(_subscription.DefaultItem)
			{
				Handle = item,
				StartNodeId = NodeId.Parse(item.Name),
				AttributeId = Attributes.Value,
				SamplingInterval = _subscriber.Options.GetSamplingInterval(),
				DiscardOldest = true,
				QueueSize = (uint)_subscriber.Options.GetQueueSize(),
				DisplayName = string.IsNullOrEmpty(item.Label) ? item.Name : item.Label,
			};

			item.Monitor = monitored;
			base.InsertItem(index, item);

			monitored.Notification += _subscriber.OnNotification;
			_subscription.AddItem(monitored);

			//从服务器中执行订阅变更操作
			if(_subscription.Created)
				_subscription.ApplyChanges();
		}

		protected override void RemoveItem(int index)
		{
			var item = this[index];

			if(item != null && item.Monitor != null)
			{
				_subscription.RemoveItem(item.Monitor);
				item.Monitor.Notification -= _subscriber.OnNotification;
				item.Monitor = null;
			}

			//调用基类同名方法
			base.RemoveItem(index);

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				_subscription.DeleteItems();
		}

		protected override void ClearItems()
		{
			foreach(var item in this.Items)
			{
				if(item == null || item.Monitor == null)
					continue;

				_subscription.RemoveItem(item.Monitor);
				item.Monitor.Notification -= _subscriber.OnNotification;
				item.Monitor = null;
			}

			//调用基类同名方法
			base.ClearItems();

			//从服务器中删除已取消的订阅项目
			if(_subscription.Created)
				_subscription.DeleteItems();
		}
		#endregion
	}
}
