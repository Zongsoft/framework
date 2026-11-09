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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public interface IDataDictionary : IDictionary, IDictionary<string, object>
{
	#region 公共属性
	object Data { get; }
	bool IsEmpty { get; }
	#endregion

	#region 公共方法
	T AsModel<T>();

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
	#endregion
}
