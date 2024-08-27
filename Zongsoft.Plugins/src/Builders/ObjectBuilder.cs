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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Zongsoft.Plugins.Builders
{
	public class ObjectBuilder : BuilderBase, IAppender
	{
		#region 构造函数
		public ObjectBuilder() { }
		public ObjectBuilder(IEnumerable<string> ignoredProperties) : base(ignoredProperties) { }
		#endregion

		#region 重写方法
		public override Type GetValueType(Builtin builtin)
		{
			var type = base.GetValueType(builtin) ??
				PluginUtility.GetOwnerElementType(builtin.Node);

			if(type == null)
			{
				//尝试获取value属性值的类型
				if(builtin.Properties.TryGetValue("value", out var property) && Parsers.Parser.CanParse(property.RawValue))
					type = Parsers.Parser.GetValueType(property.RawValue, builtin);
			}

			return type;
		}

		public override object Build(BuilderContext context)
		{
			//如果定义了类型（或简单的type或完整的constructor）
			if(context.Builtin.BuiltinType != null)
				return base.Build(context);

			//如果定义了value属性，则采用该属性值作为构建结果
			if(context.Builtin.Properties.TryGetValue("value", out var property))
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

			//第一步：尝试容器是否实现泛型字典接口
			var invoker = GetGenericDictionaryAdder(container, child, out var convertedChild);
			if(invoker != null && convertedChild != null)
			{
				invoker.DynamicInvoke(key, convertedChild);
				return true;
			}

			//第二步：尝试容器是否实现泛型集合接口
			invoker = GetGenericCollectionAdder(container, child, out convertedChild, out var elementType);
			if(invoker != null && convertedChild != null)
			{
				if(elementType == null)
					invoker.DynamicInvoke(convertedChild);
				else
				{
					foreach(var element in (IEnumerable)convertedChild)
						invoker.DynamicInvoke(element);
				}

				return true;
			}

			//第三步：尝试容器是否实现传统的非泛型字典接口
			if(container is IDictionary dictionary && !dictionary.IsReadOnly)
			{
				dictionary.Add(key, child);
				return true;
			}

			//第四步：尝试容器是否实现传统的非泛型列表接口
			if(container is IList list && !list.IsReadOnly)
			{
				list.Add(child);
				return true;
			}

			//第五步：尝试获取容器对象的默认属性，如果获取成功则以其默认属性作为容器
			var defaultMember = GetDefaultMemberName(container.GetType());
			if(defaultMember != null && defaultMember.Length > 0 && this.Append(Reflection.Reflector.GetValue(ref container, defaultMember), child, key))
				return true;

			//第六步：尝试使用容器中的特定(Add/Register)方法进行追加操作
			var methods = container.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
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

			//上述所有步骤均未完成则返回失败
			return false;
		}

		private static string GetDefaultMemberName(Type type)
		{
			var attribute = Attribute.GetCustomAttribute(type, typeof(DefaultMemberAttribute), true);
			if(attribute != null)
				return ((DefaultMemberAttribute)attribute).MemberName;

			attribute = Attribute.GetCustomAttribute(type, typeof(System.ComponentModel.DefaultPropertyAttribute), true);
			if(attribute != null)
				return ((System.ComponentModel.DefaultPropertyAttribute)attribute).Name;

			return null;
		}

		private static Delegate GetGenericDictionaryAdder(object container, object child, out object convertedChild)
		{
			convertedChild = null;

			foreach(var contract in container.GetType().GetInterfaces().Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
			{
				MethodInfo method = null;
				var mapping = container.GetType().GetInterfaceMap(contract);

				for(int i = 0; i < mapping.InterfaceMethods.Length; i++)
				{
					if(mapping.InterfaceMethods[i].Name == "Add")
					{
						var parameters = mapping.InterfaceMethods[i].GetParameters();

						if(parameters.Length == 2 && (parameters[0].ParameterType == typeof(string) || parameters[0].ParameterType == typeof(object)))
						{
							if(Zongsoft.Common.Convert.TryConvertValue(child, parameters[1].ParameterType, out convertedChild))
							{
								method = mapping.TargetMethods[i];
								break;
							}
						}
					}
				}

				if(method != null)
					return method.CreateDelegate(typeof(Action<,>).MakeGenericType(method.GetParameters()[0].ParameterType, method.GetParameters()[1].ParameterType), container);
			}

			return null;
		}

		private static Delegate GetGenericCollectionAdder(object container, object child, out object convertedChild, out Type elementType)
		{
			convertedChild = null;
			elementType = null;

			foreach(var contract in container.GetType().GetInterfaces().Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(ICollection<>)))
			{
				MethodInfo method = null;
				var mapping = container.GetType().GetInterfaceMap(contract);

				for(int i = 0; i < mapping.InterfaceMethods.Length; i++)
				{
					if(mapping.InterfaceMethods[i].Name == "Add")
					{
						var parameters = mapping.InterfaceMethods[i].GetParameters();

						if(parameters.Length == 1)
						{
							if(Zongsoft.Common.Convert.TryConvertValue(child, parameters[0].ParameterType, out convertedChild))
							{
								method = mapping.TargetMethods[i];
								break;
							}

							elementType = child is string ? null : Zongsoft.Common.TypeExtension.GetElementType(child.GetType());
							if(elementType != null && CanConvertTo(elementType, parameters[0].ParameterType))
							{
								convertedChild = child;
								method = mapping.TargetMethods[i];
								break;
							}
						}
					}
				}

				if(method != null)
					return method.CreateDelegate(typeof(Action<>).MakeGenericType(method.GetParameters()[0].ParameterType), container);
			}

			return null;
		}

		private static bool CanConvertTo(Type instanceType, Type contractType)
		{
			if(contractType == null || instanceType == null)
				return false;

			if(contractType.IsAssignableFrom(instanceType))
				return true;

			var converter = TypeDescriptor.GetConverter(instanceType);
			if(converter != null && converter.CanConvertTo(contractType))
				return true;

			converter = TypeDescriptor.GetConverter(contractType);
			if(converter != null && converter.CanConvertFrom(instanceType))
				return true;

			return false;
		}

		[Obsolete]
		private bool AppendObsoleted(object container, object child, string key)
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

			if(defaultMember != null && defaultMember.Length > 0 && this.Append(Reflection.Reflector.GetValue(ref container, defaultMember), child, key))
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

		[Obsolete]
		private static Delegate GetDictionaryAddMethod(object container, Type childType, out Type valueType)
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

		[Obsolete]
		private static Delegate GetCollectionAddMethod(object container, Type childType, out Type valueType)
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
