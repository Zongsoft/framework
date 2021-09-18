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
		void Send(byte[] data, object parameter = null);
		void Send(byte[] data, int offset, object parameter = null);
		void Send(byte[] data, int offset, int count, object parameter = null);
		void Send(string text, Encoding encoding = null, object parameter = null);
		void Send(ReadOnlySpan<byte> data, object parameter = null);

		Task SendAsync(byte[] data, CancellationToken cancellation = default) => this.SendAsync(data, null, cancellation);
		Task SendAsync(byte[] data, int offset, CancellationToken cancellation = default) => this.SendAsync(data, offset, null, cancellation);
		Task SendAsync(byte[] data, int offset, int count, CancellationToken cancellation = default) => this.SendAsync(data, offset, count, null, cancellation);
		Task SendAsync(string text, Encoding encoding = null, CancellationToken cancellation = default) => this.SendAsync(text, encoding, null, cancellation);
		Task SendAsync(string text, Encoding encoding, object parameter, CancellationToken cancellation = default) => this.SendAsync((encoding ?? Encoding.UTF8).GetBytes(text), parameter, cancellation);
		Task SendAsync(ReadOnlySpan<byte> data, CancellationToken cancellation = default) => this.SendAsync(data, null, cancellation);

		Task SendAsync(byte[] data, object parameter, CancellationToken cancellation = default);
		Task SendAsync(byte[] data, int offset, object parameter, CancellationToken cancellation = default);
		Task SendAsync(byte[] data, int offset, int count, object parameter, CancellationToken cancellation = default);
		Task SendAsync(ReadOnlySpan<byte> data, object parameter, CancellationToken cancellation = default);
	}
}
