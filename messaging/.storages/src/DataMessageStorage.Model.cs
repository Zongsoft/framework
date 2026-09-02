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
 * This file is part of Zongsoft.Messaging.Storages.Data library.
 *
 * The Zongsoft.Messaging.Storages.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Storages.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Storages.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Messaging.Storages;

partial class DataMessageStorage
{
	internal sealed class MessageModel
	{
		#region 公共属性
		public string Identifier { get; set; }
		public string Identity { get; set; }
		public string Topic { get; set; }
		public byte[] Data { get; set; }
		public string Tags { get; set; }
		public DateTime Timestamp { get; set; }
		#endregion

		#region 公共方法
		public Message ToMessage() => new(this.Identifier, this.Topic ?? string.Empty, this.Data == null ? null : (byte[])this.Data.Clone())
		{
			Identity = this.Identity,
			Tags = this.Tags,
			Timestamp = Normalize(this.Timestamp),
		};
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrEmpty(this.Tags) ?
			$"[{this.Identifier}@{this.Timestamp}]{this.Topic}":
			$"[{this.Identifier}@{this.Timestamp}]{this.Topic}({this.Tags})";
		#endregion
	}
}
