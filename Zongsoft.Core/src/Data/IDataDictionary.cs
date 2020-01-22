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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public interface IDataDictionary : IDictionary, IDictionary<string, object>
	{
		object Data
		{
			get;
		}

		bool Contains(string name);

		bool HasChanges(params string[] names);
		void Reset(params string[] names);
		bool Reset(string name, out object value);

		object GetValue(string name);
		TValue GetValue<TValue>(string name, TValue defaultValue);

		void SetValue<TValue>(string name, TValue value, Func<TValue, bool> predicate = null);
		void SetValue<TValue>(string name, Func<TValue> valueFactory, Func<TValue, bool> predicate = null);

		bool TryGetValue<TValue>(string name, out TValue value);
		bool TryGetValue<TValue>(string name, Action<TValue> got);

		bool TrySetValue<TValue>(string name, TValue value, Func<TValue, bool> predicate = null);
		bool TrySetValue<TValue>(string name, Func<TValue> valueFactory, Func<TValue, bool> predicate = null);
	}
}
