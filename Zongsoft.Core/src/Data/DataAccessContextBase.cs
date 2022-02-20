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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问的上下文基类。
	/// </summary>
	public abstract class DataAccessContextBase<TOptions> : IDataAccessContextBase<TOptions>, IDisposable where TOptions : IDataOptions
	{
		#region 构造函数
		protected DataAccessContextBase(IDataAccess dataAccess, string name, DataAccessMethod method, TOptions options)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Method = method;
			this.DataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据访问的名称。</summary>
		public string Name { get; }

		/// <summary>获取数据访问的方法。</summary>
		public DataAccessMethod Method { get; }

		/// <summary>获取当前上下文关联的数据访问器。</summary>
		public IDataAccess DataAccess { get; }

		/// <summary>获取当前上下文关联的用户主体。</summary>
		public System.Security.Claims.ClaimsPrincipal Principal { get => Services.ApplicationContext.Current?.Principal; }

		/// <summary>获取当前数据访问操作的选项对象。</summary>
		public TOptions Options { get; }
		#endregion

		#region 处置方法
		public void Dispose() => this.Dispose(true);
		protected virtual void Dispose(bool disposing) { }
		#endregion

		#region 重写方法
		public override string ToString() => $"[{this.Method}] {this.Name}";
		#endregion
	}
}
