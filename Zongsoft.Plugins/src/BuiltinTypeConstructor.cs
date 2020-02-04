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
		private BuiltinType _builtinType;
		private IList<Parameter> _parameters;
		private Parameter[] _parameterArray;
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
			get
			{
				return _builtinType.Builtin;
			}
		}

		public BuiltinType BuiltinType
		{
			get
			{
				return _builtinType;
			}
		}

		/// <summary>
		/// 获取构造子参数的数量。
		/// </summary>
		public int Count
		{
			get
			{
				return _parameterArray == null ? _parameters.Count : _parameterArray.Length;
			}
		}

		public Parameter[] Parameters
		{
			get
			{
				if(_parameterArray != null)
					return _parameterArray;

				if(_parameters.Count == 0)
					return Array.Empty<Parameter>();
				else
					return _parameters.ToArray();
			}
		}
		#endregion

		#region 内部方法
		internal Parameter Add(string parameterType, string rawValue)
		{
			var parameter = new Parameter(this, parameterType, rawValue);
			_parameters.Add(parameter);
			_parameterArray = null;
			return parameter;
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<Parameter> GetEnumerator()
		{
			Parameter[] values = _parameterArray;

			if(values == null)
				_parameterArray = values = new Parameter[_parameters.Count];

			foreach(var value in values)
			{
				yield return value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion

		[Serializable]
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
				if(constructor == null)
					throw new ArgumentNullException("constructor");

				_constructor = constructor;
				_parameterTypeName = typeName;
				_rawValue = rawValue;
				_evaluateValueRequired = 0;
			}
			#endregion

			#region 公共属性
			public Builtin Builtin
			{
				get
				{
					return _constructor._builtinType.Builtin;
				}
			}

			public BuiltinTypeConstructor Constructor
			{
				get
				{
					return _constructor;
				}
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
				get
				{
					return _parameterTypeName;
				}
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
				get
				{
					return _value != null;
				}
			}

			public object Value
			{
				get
				{
					return this.GetValue(this.ParameterType);
				}
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
