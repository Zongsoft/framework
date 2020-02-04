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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins.Builders
{
	public class ObjectBuilder : BuilderBase, IAppender
	{
		#region 构造函数
		public ObjectBuilder()
		{
		}

		public ObjectBuilder(IEnumerable<string> ignoredProperties) : base(ignoredProperties)
		{
		}
		#endregion

		#region 重写方法
		public override Type GetValueType(Builtin builtin)
		{
			//调用基类同名方法
			var type = base.GetValueType(builtin);

			if(type == null)
			{
				//尝试获取value属性值的类型
				if(builtin.Properties.TryGet("value", out var property) && Parsers.Parser.CanParse(property.RawValue))
					type = Parsers.Parser.GetValueType(property.RawValue, builtin);

				//获取所有者元素类型（如果所有者如果是一个泛型集合的话，否则返回空）
				if(type == null)
					type = PluginUtility.GetOwnerElementType(builtin.Node);
			}

			return type;
		}

		public override object Build(BuilderContext context)
		{
			//如果定义了类型（或简单的type或完整的constructor）
			if(context.Builtin.BuiltinType != null)
				return base.Build(context);

			//如果定义了value属性，则采用该属性值作为构建结果
			if(context.Builtin.Properties.TryGet("value", out var property))
			{
				if(Parsers.Parser.CanParse(property.RawValue))
				{
					var result = Parsers.Parser.Parse(property.RawValue, context.Builtin, "value", this.GetValueType(context.Builtin));

					if(result != null)
					{
						//必须将value自身作为忽略属性项
						var ignoredProperties = this.IgnoredProperties == null ?
							new HashSet<string>(new[] { "value" }, StringComparer.OrdinalIgnoreCase) :
							new HashSet<string>(this.IgnoredProperties.Concat(new[] { "value" }), StringComparer.OrdinalIgnoreCase);

						//更新构件属性到目标对象的属性中
						PluginUtility.UpdateProperties(result, context.Builtin, ignoredProperties);
					}

					return result;
				}

				return property.RawValue;
			}

			//调用基类同名方法
			return base.Build(context);
		}
		#endregion

		#region 显式实现
		bool IAppender.Append(AppenderContext context)
		{
			if(context.Container == null || context.Value == null)
				return false;

			return this.Append(context.Container, context.Value, context.Node.Name);
		}
		#endregion

		#region 私有方法
		private bool Append(object container, object child, string key)
		{
			if(container == null || child == null)
				return false;

			Type containerType = container.GetType();

			//第一步：确认容器对象实现的各种字典接口
			var add = GetDictionaryAddMethod(container, child.GetType(), out var valueType);

			if(add != null && Common.Convert.TryConvertValue(child, valueType, out var value))
			{
				add.DynamicInvoke(key, value);
				return true;
			}

			//第二步(a)：确认容器对象实现的各种集合或列表接口
			add = GetCollectionAddMethod(container, child.GetType(), out valueType);

			if(add != null && Common.Convert.TryConvertValue(child, valueType, out value))
			{
				add.DynamicInvoke(value);
				return true;
			}

			//第二步(b)：如果子对象是容器集元素的可遍历集，则遍历该子对象，将其各元素加入到容器集中
			var elementType = child is string ? null : Common.TypeExtension.GetElementType(child.GetType());

			//如果元素类型不为空，则表示子对象是一个可遍历集
			if(elementType != null)
			{
				add = GetCollectionAddMethod(container, elementType, out valueType);

				if(add != null)
				{
					int count = 0;

					foreach(var entry in (IEnumerable)child)
					{
						add.DynamicInvoke(entry);
						count++;
					}

					return count > 0;
				}
			}

			//第三步：尝试获取容器对象的默认属性标签
			var defaultMember = GetDefaultMemberName(containerType);

			if(defaultMember != null && defaultMember.Length > 0 && this.Append(Reflection.Reflector.GetValue(container, defaultMember), child, key))
				return true;

			//第四步：进行特定方法绑定
			var methods = containerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
						  .Where(method => method.Name == "Add" || method.Name == "Register")
						  .OrderByDescending(method => method.GetParameters().Length);

			foreach(var method in methods)
			{
				var parameters = method.GetParameters();

				if(parameters.Length == 2)
				{
					if(parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType.IsAssignableFrom(child.GetType()))
					{
						method.Invoke(container, new object[] { key, child });
						return true;
					}
				}
				else if(parameters.Length == 1)
				{
					if(parameters[0].ParameterType.IsAssignableFrom(child.GetType()))
					{
						method.Invoke(container, new object[] { child });
						return true;
					}
				}
			}

			//如果上述所有步骤均未完成则返回失败
			return false;
		}

		private string GetDefaultMemberName(Type type)
		{
			var attribute = Attribute.GetCustomAttribute(type, typeof(DefaultMemberAttribute), true);

			if(attribute != null)
				return ((DefaultMemberAttribute)attribute).MemberName;

			attribute = Attribute.GetCustomAttribute(type, typeof(System.ComponentModel.DefaultPropertyAttribute), true);

			if(attribute != null)
				return ((System.ComponentModel.DefaultPropertyAttribute)attribute).Name;

			return null;
		}

		private Delegate GetDictionaryAddMethod(object container, Type childType, out Type valueType)
		{
			//设置输出参数默认值
			valueType = null;

			//获取容器类型实现的所有接口
			var interfaces = container.GetType().GetInterfaces();

			//确保泛型字典接口在非泛型字典接口之前
			var contracts = interfaces.Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				.Concat(interfaces.Where(p => p == typeof(IDictionary)));

			foreach(var contract in contracts)
			{
				MethodInfo method = null;
				var mapping = container.GetType().GetInterfaceMap(contract);

				for(int i = 0; i < mapping.InterfaceMethods.Length; i++)
				{
					if(mapping.InterfaceMethods[i].Name == "Add")
					{
						var parameters = mapping.InterfaceMethods[i].GetParameters();

						if(parameters.Length == 2 && (parameters[0].ParameterType == typeof(string) || parameters[0].ParameterType == typeof(object)) &&
						   parameters[1].ParameterType.IsAssignableFrom(childType))
							method = mapping.TargetMethods[i];

						break;
					}
				}

				if(method != null)
				{
					valueType = method.GetParameters()[1].ParameterType;
					return method.CreateDelegate(typeof(Action<,>).MakeGenericType(method.GetParameters()[0].ParameterType, valueType), container);
				}
			}

			return null;
		}

		private Delegate GetCollectionAddMethod(object container, Type childType, out Type valueType)
		{
			//设置输出参数默认值
			valueType = null;

			//获取容器类型实现的所有接口
			var interfaces = container.GetType().GetInterfaces();

			//确保泛型集合接口在非泛型集合接口之前
			var contracts = interfaces.Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(ICollection<>))
				.Concat(interfaces.Where(p => p == typeof(IList)));

			foreach(var contract in contracts)
			{
				MethodInfo method = null;
				var mapping = container.GetType().GetInterfaceMap(contract);

				for(int i = 0; i < mapping.InterfaceMethods.Length; i++)
				{
					if(mapping.InterfaceMethods[i].Name == "Add")
					{
						var parameters = mapping.InterfaceMethods[i].GetParameters();

						if(parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(childType))
							method = mapping.TargetMethods[i];

						break;
					}
				}

				if(method != null)
				{
					valueType = method.GetParameters()[0].ParameterType;

					//注意：IList接口的Add方法有返回值(int)。
					if(method.ReturnType == null || method.ReturnType == typeof(void))
						return method.CreateDelegate(typeof(Action<>).MakeGenericType(valueType), container);
					else
						return method.CreateDelegate(typeof(Func<,>).MakeGenericType(valueType, method.ReturnType), container);
				}
			}

			return null;
		}
		#endregion
	}
}
