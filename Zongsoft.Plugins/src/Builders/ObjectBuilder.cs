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

using Zongsoft.Reflection;

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
			//如果定义了value属性，则优先采用该属性值作为构建结果
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

			//如果定义了类型（或简单的type或完整的constructor）
			if(context.Builtin.BuiltinType != null)
				return base.Build(context);

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

			//优先确认容器的子集对象
			if(container is Components.IDiscriminator discriminator)
			{
				var host = discriminator.Discriminate(child);

				if(host != null)
					return this.Append(host, child, key);
			}

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
				try
				{
					list.Add(child);
					return true;
				}
				catch { }
			}

			//第五步：尝试获取容器对象的默认属性，如果获取成功则以其默认属性作为容器
			var defaultMemberValue = GetDefaultMemberValue(ref container);
			if(defaultMemberValue != null && this.Append(defaultMemberValue, child, key))
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
						method.Invoke(container, [key, child]);
						return true;
					}
				}
				else if(parameters.Length == 1)
				{
					if(parameters[0].ParameterType.IsAssignableFrom(child.GetType()))
					{
						method.Invoke(container, [child]);
						return true;
					}
				}
			}

			//上述所有步骤均未完成则返回失败
			return false;
		}

		private static object GetDefaultMemberValue(ref object target)
		{
			if(target == null)
				return null;

			var member = GetDefaultMember(target.GetType());
			if(member == null || (member.IsProperty(out var property) && property.IsIndexer()))
				return null;

			return Reflector.GetValue(member, ref target);
		}

		private static MemberInfo GetDefaultMember(Type type)
		{
			var name = GetDefaultMemberName(type);
			if(string.IsNullOrEmpty(name))
				return null;

			var members = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			return members != null && members.Length > 0 ? members[0] : null;
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

			var converter = System.ComponentModel.TypeDescriptor.GetConverter(instanceType);
			if(converter != null && converter.CanConvertTo(contractType))
				return true;

			converter = System.ComponentModel.TypeDescriptor.GetConverter(contractType);
			if(converter != null && converter.CanConvertFrom(instanceType))
				return true;

			return false;
		}
		#endregion
	}
}
