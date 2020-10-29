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

namespace Zongsoft.Services
{
	public class ExecutionContext : IExecutionContext
	{
		#region 成员字段
		private IExecutor _executor;
		private object _data;
		private object _result;
		private Exception _exception;
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public ExecutionContext(object data = null, IDictionary<string, object> parameters = null)
		{
			_data = data;

			if(parameters != null && parameters.Count > 0)
				_parameters = new Dictionary<string, object>(parameters);
		}

		public ExecutionContext(IExecutor executor, object data = null, IDictionary<string, object> parameters = null)
		{
			_executor = executor ?? throw new ArgumentNullException(nameof(executor));
			_data = data;

			if(parameters != null && parameters.Count > 0)
				_parameters = new Dictionary<string, object>(parameters);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取处理本次执行请求的执行器。
		/// </summary>
		public virtual IExecutor Executor
		{
			get => _executor;
		}

		/// <summary>
		/// 获取处理本次执行请求的数据。
		/// </summary>
		public object Data
		{
			get => _data;
		}

		/// <summary>
		/// 获取本次执行中发生的异常。
		/// </summary>
		public virtual Exception Exception
		{
			get => _exception;
			internal protected set => _exception = value;
		}

		/// <summary>
		/// 获取扩展参数集是否有内容。
		/// </summary>
		/// <remarks>
		///		<para>在不确定扩展参数集是否含有内容之前，建议先使用该属性来检测。</para>
		/// </remarks>
		public virtual bool HasParameters
		{
			get => _parameters != null;
		}

		/// <summary>
		/// 获取扩展参数集。
		/// </summary>
		public virtual IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}

		/// <summary>
		/// 获取或设置本次执行的返回结果。
		/// </summary>
		public object Result
		{
			get => _result;
			set => _result = value;
		}
		#endregion
	}
}
