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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication
{
	public interface ISender
	{
		void Send(ReadOnlySpan<byte> data);
		void Send(byte[] data) => this.Send(data.AsSpan());
		void Send(byte[] data, int offset) => this.Send(data.AsSpan(offset));
		void Send(byte[] data, int offset, int count) => this.Send(data.AsSpan(offset, count));
		void Send(string text, Encoding encoding = null)
		{
			if(text != null && text.Length > 0)
				this.Send((encoding ?? Encoding.UTF8).GetBytes(text).AsSpan());
		}

		Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default);
		Task SendAsync(byte[] data, CancellationToken cancellation = default) => this.SendAsync(data.AsSpan(), cancellation);
		Task SendAsync(byte[] data, int offset, CancellationToken cancellation = default) => this.SendAsync(data.AsSpan(offset), cancellation);
		Task SendAsync(byte[] data, int offset, int count, CancellationToken cancellation = default) => this.SendAsync(data.AsSpan(offset, count), cancellation);
		Task SendAsync(string text, Encoding encoding = null, CancellationToken cancellation = default)
		{
			return string.IsNullOrEmpty(text) ?
				Task.CompletedTask :
				this.SendAsync((encoding ?? Encoding.UTF8).GetBytes(text).AsSpan(), cancellation);
		}
	}
}
