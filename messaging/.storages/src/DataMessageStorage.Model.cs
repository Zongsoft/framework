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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
