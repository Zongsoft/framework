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

namespace Zongsoft.Common
{
	public class InstanceData
	{
		#region 私有变量
		private readonly object _source;
		private object _value;
		#endregion

		#region 构造函数
		public InstanceData(object source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_value = default;
		}
		#endregion

		#region 公共方法
		public T GetValue<T>(Func<object, T> resolve)
		{
			if(resolve == null)
				throw new ArgumentNullException(nameof(resolve));

			if(_value == null)
			{
				lock(this)
				{
					if(_value == null)
					{
						_value = resolve(_source);

						if(_source is IDisposable disposable)
							disposable.Dispose();
					}
				}
			}

			return (T)_value;
		}
		#endregion
	}
}
