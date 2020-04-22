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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public class BuiltinTypeConstructor : IEnumerable<BuiltinTypeConstructor.Parameter>
	{
		#region 成员变量
		private readonly BuiltinType _builtinType;
		private readonly IList<Parameter> _parameters;
		#endregion

		#region 构造函数
		internal BuiltinTypeConstructor(BuiltinType builtinType)
		{
			_builtinType = builtinType ?? throw new ArgumentNullException(nameof(builtinType));
			_parameters = new List<Parameter>();
		}
		#endregion

		#region 公共属性
		public Builtin Builtin
		{
			get => _builtinType.Builtin;
		}

		public BuiltinType BuiltinType
		{
			get => _builtinType;
		}

		/// <summary>
		/// 获取构造子参数的数量。
		/// </summary>
		public int Count
		{
			get => _parameters.Count;
		}

		public Parameter[] Parameters
		{
			get => _parameters.ToArray();
		}
		#endregion

		#region 内部方法
		internal Parameter Add(string parameterType, string rawValue)
		{
			var parameter = new Parameter(this, parameterType, rawValue);
			_parameters.Add(parameter);
			return parameter;
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<Parameter> GetEnumerator()
		{
			return _parameters.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion

		public class Parameter
		{
			#region 成员变量
			private BuiltinTypeConstructor _constructor;
			private string _rawValue;
			private string _parameterTypeName;
			private Type _parameterType;
			private object _value;
			#endregion

			#region 私有变量
			private int _evaluateValueRequired;
			#endregion

			#region 构造函数
			internal Parameter(BuiltinTypeConstructor constructor, string typeName, string rawValue)
			{
				_constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
				_parameterTypeName = typeName;
				_rawValue = rawValue;
				_evaluateValueRequired = 0;
			}
			#endregion

			#region 公共属性
			public Builtin Builtin
			{
				get => _constructor._builtinType.Builtin;
			}

			public BuiltinTypeConstructor Constructor
			{
				get => _constructor;
			}

			public Type ParameterType
			{
				get
				{
					if(_parameterType == null && (!string.IsNullOrEmpty(_parameterTypeName)))
						_parameterType = PluginUtility.GetType(_parameterTypeName);

					return _parameterType;
				}
			}

			public string ParameterTypeName
			{
				get => _parameterTypeName;
			}

			public string RawValue
			{
				get
				{
					return _rawValue;
				}
				internal set
				{
					if(string.Equals(_rawValue, value, StringComparison.Ordinal))
						return;

					_rawValue = value;

					//启用重新计算Value属性
					System.Threading.Interlocked.Exchange(ref _evaluateValueRequired, 0);
				}
			}

			public bool HasValue
			{
				get => _value != null;
			}

			public object Value
			{
				get => this.GetValue(this.ParameterType);
			}
			#endregion

			#region 公共方法
			public object GetValue(Type valueType)
			{
				var original = System.Threading.Interlocked.CompareExchange(ref _evaluateValueRequired, 1, 0);

				if(original == 0)
					_value = PluginUtility.ResolveValue(_constructor.Builtin, _rawValue, null, valueType ?? this.ParameterType, null, null);

				return _value;
			}
			#endregion
		}
	}
}
