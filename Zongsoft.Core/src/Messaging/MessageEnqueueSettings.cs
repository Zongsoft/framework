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
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	public class MessageEnqueueSettings
	{
		#region 成员字段
		private TimeSpan _delayTimeout;
		private byte _priority;
		#endregion

		#region 构造函数
		public MessageEnqueueSettings() : this(TimeSpan.Zero)
		{
		}

		public MessageEnqueueSettings(byte priority) : this(TimeSpan.Zero)
		{
		}

		public MessageEnqueueSettings(TimeSpan delayTimeout, byte priority = 6)
		{
			_delayTimeout = delayTimeout;
			_priority = priority;
		}
		#endregion

		#region 公共属性
		public TimeSpan DelayTimeout
		{
			get
			{
				return _delayTimeout;
			}
			set
			{
				_delayTimeout = value;
			}
		}

		public byte Priority
		{
			get
			{
				return _priority;
			}
			set
			{
				_priority = value;
			}
		}
		#endregion
	}
}
