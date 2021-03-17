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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示查询条件实体的抽象基类。
	/// </summary>
	public abstract class CriteriaBase : IModel
	{
		#region 保护构造
		protected CriteriaBase() { }
		#endregion

		#region 抽象方法
		protected abstract int GetCount();
		protected abstract IDictionary<string, object> GetChanges();
		protected abstract bool HasChanges(params string[] names);
		protected abstract bool Reset(string name, out object value);
		protected abstract void Reset(params string[] names);
		protected abstract bool TryGetValue(string name, out object value);
		protected abstract bool TrySetValue(string name, object value);
		#endregion

		#region 显式实现
		int IModel.GetCount() => this.GetCount();
		IDictionary<string, object> IModel.GetChanges() => this.GetChanges();
		bool IModel.HasChanges(params string[] names) => this.HasChanges(names);
		bool IModel.Reset(string name, out object value) => this.Reset(name, out value);
		void IModel.Reset(params string[] names) => this.Reset(names);
		bool IModel.TryGetValue(string name, out object value) => this.TryGetValue(name, out value);
		bool IModel.TrySetValue(string name, object value) => this.TrySetValue(name, value);
		#endregion
	}
}
