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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication
{
	public static class SendExtension
	{
		public static void Send(this ISender sender, byte[] data) => sender.Send(new ReadOnlySequence<byte>(data));
		public static void Send(this ISender sender, byte[] data, int offset) => sender.Send(new ReadOnlySequence<byte>(data, offset, data.Length - offset));
		public static void Send(this ISender sender, byte[] data, int offset, int count) => sender.Send(new ReadOnlySequence<byte>(data, offset, count));
		public static void Send(this ISender sender, string text, Encoding encoding = null)
		{
			if(sender == null)
				throw new ArgumentNullException(nameof(sender));

			if(text != null && text.Length > 0)
				sender.Send((encoding ?? Encoding.UTF8).GetBytes(text));
		}

		public static Task SendAsync(this ISender sender, byte[] data, CancellationToken cancellation = default) => sender.SendAsync(new ReadOnlySequence<byte>(data), cancellation);
		public static Task SendAsync(this ISender sender, byte[] data, int offset, CancellationToken cancellation = default) => sender.SendAsync(new ReadOnlySequence<byte>(data, offset, data.Length - offset), cancellation);
		public static Task SendAsync(this ISender sender, byte[] data, int offset, int count, CancellationToken cancellation = default) => sender.SendAsync(new ReadOnlySequence<byte>(data, offset, count), cancellation);
		public static Task SendAsync(this ISender sender, string text, Encoding encoding = null, CancellationToken cancellation = default)
		{
			if(sender == null)
				throw new ArgumentNullException(nameof(sender));

			return string.IsNullOrEmpty(text) ?
				Task.CompletedTask :
				sender.SendAsync((encoding ?? Encoding.UTF8).GetBytes(text), cancellation);
		}
	}
}
