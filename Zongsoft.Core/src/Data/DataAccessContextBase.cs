﻿/*
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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问的上下文基类。
	/// </summary>
	public abstract class DataAccessContextBase : IDataAccessContextBase, IDisposable, INotifyPropertyChanged
	{
		#region 事件定义
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region 成员字段
		private IDictionary<string, object> _states;
		#endregion

		#region 构造函数
		protected DataAccessContextBase(IDataAccess dataAccess, string name, DataAccessMethod method, IDictionary<string, object> states)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Method = method;
			this.DataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
			_states = states;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问的名称。
		/// </summary>
		public string Name
		{
			get;
		}

		/// <summary>
		/// 获取数据访问的方法。
		/// </summary>
		public DataAccessMethod Method
		{
			get;
		}

		/// <summary>
		/// 获取当前上下文关联的数据访问器。
		/// </summary>
		public IDataAccess DataAccess
		{
			get;
		}

		/// <summary>
		/// 获取当前上下文关联的用户主体。
		/// </summary>
		public System.Security.Claims.ClaimsPrincipal Principal
		{
			get => Services.ApplicationContext.Current?.Principal;
		}

		/// <summary>
		/// 获取一个值，指示当前上下文是否含有附加的状态数据。
		/// </summary>
		public bool HasStates
		{
			get => _states != null && _states.Count > 0;
		}

		/// <summary>
		/// 获取当前上下文的附加状态数据集。
		/// </summary>
		public IDictionary<string, object> States
		{
			get
			{
				if(_states == null)
					System.Threading.Interlocked.CompareExchange(ref _states, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _states;
			}
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			var states = _states;

			if(states != null)
				states.Clear();
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return $"[{this.Method.ToString()}] {this.Name}";
		}
		#endregion
	}
}
