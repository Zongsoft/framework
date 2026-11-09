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
using System.Linq.Expressions;

namespace Zongsoft.Data;

public interface IDataDictionary<T> : IDataDictionary
{
	#region 公共方法
	T AsModel();
	bool Contains<TMember>(Expression<Func<T, TMember>> expression);
	bool Reset<TValue>(Expression<Func<T, TValue>> expression, out TValue value);

	TValue GetValue<TValue>(Expression<Func<T, TValue>> expression);
	TValue GetValue<TValue>(Expression<Func<T, TValue>> expression, TValue defaultValue);

	void SetValue<TValue>(Expression<Func<T, TValue>> expression, TValue value, Func<TValue, bool> predicate = null);
	void SetValue<TValue>(Expression<Func<T, TValue>> expression, Func<string, TValue> valueFactory, Func<TValue, bool> predicate = null);

	bool TryGetValue<TValue>(Expression<Func<T, TValue>> expression, out TValue value);
	bool TryGetValue<TValue>(Expression<Func<T, TValue>> expression, Action<string, TValue> got);

	bool TrySetValue<TValue>(Expression<Func<T, TValue>> expression, TValue value, Func<TValue, bool> predicate = null);
	bool TrySetValue<TValue>(Expression<Func<T, TValue>> expression, Func<string, TValue> valueFactory, Func<TValue, bool> predicate = null);
	#endregion
}
