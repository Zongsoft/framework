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
	public abstract class PredicationBase<T> : IPredication<T>, Services.IMatchable
	{
		#region 成员字段
		private readonly string _name;
		#endregion

		#region 构造函数
		protected PredicationBase(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
		}
		#endregion

		#region 公共属性
		public virtual string Name => _name;
		#endregion

		#region 断言方法
		public abstract bool Predicate(T parameter);
		bool IPredication.Predicate(object parameter) => this.Predicate(this.ConvertParameter(parameter));
		#endregion

		#region 虚拟方法
		protected virtual T ConvertParameter(object parameter) => Zongsoft.Common.Convert.ConvertValue<T>(parameter);
		#endregion

		#region 服务匹配
		public virtual bool Match(string parameter) => string.Equals(this.Name, parameter, StringComparison.OrdinalIgnoreCase);
		bool Services.IMatchable.Match(object parameter) => parameter != null && this.Match(parameter.ToString());
		#endregion

		#region 重写方法
		public override string ToString() => $"{this.Name} ({this.GetType().Name})";
		#endregion
	}
}
