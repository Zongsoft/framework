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
		private readonly ParameterCollection _parameters;
		#endregion

		#region 构造函数
		internal BuiltinTypeConstructor(BuiltinType builtinType)
		{
			_builtinType = builtinType ?? throw new ArgumentNullException(nameof(builtinType));
			_parameters = new ParameterCollection(this);
		}
		#endregion

		#region 公共属性
		public Builtin Builtin => _builtinType.Builtin;
		public BuiltinType BuiltinType => _builtinType;
		public int Count => _parameters.Count;
		public ParameterCollection Parameters => _parameters;
		#endregion

		#region 枚举遍历
		public IEnumerator<Parameter> GetEnumerator() => _parameters.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		public sealed class ParameterCollection(BuiltinTypeConstructor constructor) : IReadOnlyCollection<Parameter>
		{
			#region 成员字段
			private readonly BuiltinTypeConstructor _constructor = constructor;
			private readonly List<Parameter> _parameters = new();
			#endregion

			#region 公共属性
			public int Count => _parameters.Count;
			public Parameter this[int index] => _parameters[index];
			public Parameter this[string name] => string.IsNullOrEmpty(name) ? null : _parameters.Find(parameter => string.Equals(parameter.Name, name));
			public Parameter this[Type type] => type == null ? null : _parameters.Find(parameter => parameter.ParameterType == type);
			#endregion

			#region 公共方法
			public bool TryGet(string name, out Parameter result)
			{
				if(string.IsNullOrEmpty(name))
				{
					result = null;
					return false;
				}

				result = _parameters.Find(parameter => string.Equals(parameter.Name, name));
				return result != null;
			}

			public bool TryGet(Type type, out Parameter result)
			{
				if(type == null)
				{
					result = null;
					return false;
				}

				result = _parameters.Find(parameter => parameter.ParameterType == type);
				return result != null;
			}
			#endregion

			#region 内部方法
			internal Parameter Add(string name, string parameterType, string rawValue)
			{
				var parameter = new Parameter(_constructor, name, parameterType, rawValue);
				_parameters.Add(parameter);
				return parameter;
			}
			#endregion

			#region 枚举遍历
			public IEnumerator<Parameter> GetEnumerator() => _parameters.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
			#endregion
		}

		public sealed class Parameter
		{
			#region 成员变量
			private BuiltinTypeConstructor _constructor;
			private string _rawValue;
			private string _parameterTypeName;
			private Type _parameterType;
			private string _name;
			private object _value;
			#endregion

			#region 私有变量
			private int _evaluateValueRequired;
			#endregion

			#region 构造函数
			internal Parameter(BuiltinTypeConstructor constructor, string name, string typeName, string rawValue)
			{
				_constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
				_name = name;
				_parameterTypeName = typeName;
				_rawValue = rawValue;
				_evaluateValueRequired = 0;
			}
			#endregion

			#region 公共属性
			public Builtin Builtin => _constructor._builtinType.Builtin;
			public BuiltinTypeConstructor Constructor => _constructor;
			public string Name => _name;
			public Type ParameterType
			{
				get
				{
					if(_parameterType == null && (!string.IsNullOrEmpty(_parameterTypeName)))
						_parameterType = PluginUtility.GetType(_parameterTypeName, this.Builtin);

					return _parameterType;
				}
			}

			public string ParameterTypeName => _parameterTypeName;
			public string RawValue
			{
				get => _rawValue;
				internal set
				{
					if(string.Equals(_rawValue, value, StringComparison.Ordinal))
						return;

					_rawValue = value;

					//启用重新计算Value属性
					System.Threading.Interlocked.Exchange(ref _evaluateValueRequired, 0);
				}
			}

			public bool HasValue => _value != null;
			public object Value => this.GetValue(this.ParameterType);
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
