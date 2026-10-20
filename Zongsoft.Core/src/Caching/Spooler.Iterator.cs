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
using System.Threading.Channels;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Caching;

partial class Spooler<T>
{
	#region 消费枚举
	//第一次枚举复用当前批次，避免额外分配枚举器。
	private sealed class Iterable(ChannelReader<T> reader, int limit) : Iterator(reader, limit), IEnumerable<T>
	{
		private int _enumerated;
		private readonly int _limit = limit;
		private readonly ChannelReader<T> _reader = reader;

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator()
		{
			if(Interlocked.Exchange(ref _enumerated, 1) == 0)
				return this;

			return new Iterator(_reader, _limit, this);
		}
	}

	private class Iterator(ChannelReader<T> reader, int limit, Iterator batch = null) : IEnumerator<T>
	{
		private T _value;
		private int _count;
		private bool _consumed;
		private ChannelReader<T> _reader = reader;
		private readonly int _limit = limit;
		private readonly Iterator _batch = batch;

		public T Current => _value;
		object IEnumerator.Current => _value;
		public bool Consumed => Volatile.Read(ref _consumed);

		public void Reset() { }
		public void Dispose() => _reader = null;
		public bool MoveNext()
		{
			if(_reader == null)
				return false;
			if(_limit > 0 && _count >= _limit)
				return false;
			if(!_reader.TryRead(out _value))
				return false;

			_count = _limit > 0 ? _count + 1 : 1;
			var batch = _batch ?? this;
			Volatile.Write(ref batch._consumed, true);
			return true;
		}
	}
	#endregion
}
