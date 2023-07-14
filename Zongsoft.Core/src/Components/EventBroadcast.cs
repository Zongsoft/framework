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

using Zongsoft.Messaging;
using Zongsoft.Serialization;

namespace Zongsoft.Components
{
	public class EventBroadcast : IFilter<EventContextBase>
	{
		#region 常量定义
		private const string DEFAULT_TOPIC = "Events";
		private static readonly TextSerializationOptions _options = new();
		#endregion

		#region 成员字段
		private string _topic;
		#endregion

		#region 构造函数
		public EventBroadcast() => _topic = DEFAULT_TOPIC;
		public EventBroadcast(IMessageQueue queue, string topic = null)
		{
			this.Queue = queue;
			this.Topic = topic;
		}
		#endregion

		#region 公共属性
		public IMessageQueue Queue { get; set; }
		public string Topic
		{
			get => _topic;
			set => _topic = string.IsNullOrEmpty(value) ? DEFAULT_TOPIC : value;
		}
		#endregion

		#region 过滤方法
		public async ValueTask OnFiltered(EventContextBase context, CancellationToken cancellation)
		{
			var queue = this.Queue;
			if(queue == null || context == null)
				return;

			var json = await Serializer.Json.SerializeAsync(context, _options, cancellation);
			if(string.IsNullOrEmpty(json))
				return;

			await queue.ProduceAsync(this.Topic, context.QualifiedName, json.AsMemory(), MessageEnqueueOptions.Default, cancellation);
		}

		public ValueTask OnFiltering(EventContextBase context, CancellationToken cancellation) => ValueTask.CompletedTask;
		#endregion
	}
}