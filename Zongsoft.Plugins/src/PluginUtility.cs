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
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Reflection;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Plugins
{
	public static class PluginUtility
	{
		#region 委托定义
		private delegate bool ParameterResolver(ParameterInfo parameter, out object result);
		#endregion

		#region 私有变量
		private static volatile int _anonymousId;
		#endregion

		#region 获取类型
		public static Type GetType(string typeFullName, PluginElement element)
		{
			if(string.IsNullOrEmpty(typeFullName))
				return null;

			var type = Zongsoft.Common.TypeAlias.Parse(typeFullName, assemblyName =>
			{
				var assembly = ResolveAssembly(assemblyName);

				if(assembly == null)
					assembly = LoadAssembly(assemblyName);

				return assembly;
			}, (assembly, typeName, ignoreCase) =>
			{
				if(assembly == null)
					return Type.GetType(typeName, false, ignoreCase);
				else
					return assembly.GetType(typeName, false, ignoreCase);
			}, false);

			if(type == null)
				throw new PluginException($"The '{typeFullName}' type resolve failed in {element}.");

			return type;
		}

		public static Type GetType(Builtin builtin)
		{
			if(builtin == null)
				return null;

			if(builtin.BuiltinType != null)
				return builtin.BuiltinType.Type;
			else
				return GetType(builtin.Properties.GetValue<string>("type"), builtin);
		}
		#endregion

		#region 构建构件
		public static object BuildBuiltin(Builtin builtin, Builders.BuilderSettings settings, IEnumerable<string> ignoredProperties)
		{
			if(builtin == null)
				throw new ArgumentNullException(nameof(builtin));

			object result;

			if(builtin.BuiltinType != null)
			{
				result = BuildType(builtin.BuiltinType);
			}
			else
			{
				//获取所有者元素的类型，如果所有者不是泛型集合则返回空
				var type = GetOwnerElementType(builtin.Node) ?? settings?.TargetType;

				if(type == null)
					throw new PluginException($"Unable to determine the target type of the '{builtin}' builtin.");

				result = BuildType(type, builtin);
			}

			//设置更新目标对象的属性集
			if(result != null)
				UpdateProperties(result, builtin, ignoredProperties);

			return result;
		}

		internal static void UpdateProperties(object target, Builtin builtin, IEnumerable<string> ignoredProperties)
		{
			if(target == null || builtin == null)
				return;

			foreach(var property in builtin.GetProperties())
			{
				//如果当前属性名为忽略属性则忽略设置
				if(ignoredProperties != null && ignoredProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
					continue;

				try
				{
					var memberExpression = MemberExpression.Parse(property.Name);

					MemberExpressionEvaluator.Default.SetValue(memberExpression, target, ctx =>
					{
						//更新扩展属性的类型
						if(property.Type == null)
						{
							property.Type = ctx.Member switch
							{
								PropertyInfo propertyInfo => propertyInfo.PropertyType,
								FieldInfo fieldInfo => fieldInfo.FieldType,
								MethodInfo methodInfo => methodInfo.ReturnType,
								_ => null,
							};
						}

						var converterTypeName = ctx.Member.GetCustomAttribute<TypeConverterAttribute>(true)?.ConverterTypeName;

						//更新扩展属性的类型转换器
						if(converterTypeName != null && converterTypeName.Length > 0)
						{
							property.Converter = Activator.CreateInstance(GetType(converterTypeName, builtin)) as TypeConverter ??
							                     throw new InvalidOperationException($"The '{converterTypeName}' type declared by the '{property.Name}' member of type '{target.GetType().FullName}' is not a type converter.");
						}

						return property.Value;
					});
				}
				catch(Exception ex)
				{
					if(System.Diagnostics.Debugger.IsAttached)
						throw;

					var message = new StringBuilder();

					message.AppendFormat("{0}[{1}]", ex.Message, ex.Source);
					message.AppendLine();

					if(ex.InnerException != null)
					{
						message.AppendFormat("\t{0}: {1}[{2}]", ex.GetType().FullName, ex.Message, ex.Source);
						message.AppendLine();
					}

					message.AppendFormat("\tOccurred an error on set '{1}' property of '{0}' builtin, it's raw value is \"{2}\", The target type of builtin is '{3}'.",
											builtin.ToString(),
											property.Name,
											builtin.Properties.GetRawValue(property.Name),
											target.GetType().AssemblyQualifiedName);

					throw new PluginException(message.ToString(), ex);
				}
			}
		}

		internal static object BuildType(BuiltinType builtinType)
		{
			if(builtinType == null)
				throw new ArgumentNullException(nameof(builtinType));

			if(builtinType.Constructor == null || builtinType.Constructor.Count == 0)
				return BuildType(builtinType.TypeName, builtinType.Builtin);

			object result;
			(var info, var values) = MatchConstructor(builtinType.Constructor);

			if(info == null || values == null)
				throw new PluginException($"Missing matching constructor for builtin '{builtinType.Builtin}'.");

			try
			{
				result = info.Invoke(values);
			}
			catch(Exception ex)
			{
				if(System.Diagnostics.Debugger.IsAttached)
					throw;

				throw new PluginException($"Create object of '{builtinType.TypeName}' type faild, The constructor is ({info.GetParameters()}).", ex);
			}

			//注入依赖属性
			InjectProperties(result, builtinType.Builtin);

			return result;

			static (ConstructorInfo info, object[] values) MatchConstructor(BuiltinTypeConstructor constructor)
			{
				var type = constructor.BuiltinType.Type;
				var values = new List<object>(constructor.Count * 2);

				//优先匹配参数数量相同的构造函数
				foreach(ConstructorInfo constructorInfo in type.GetConstructors().Where(ctor => ctor.GetParameters().Length == constructor.Count))
				{
					var parameters = constructorInfo.GetParameters();

					for(int i = 0; i < parameters.Length; i++)
					{
						if(string.Equals(constructor.Parameters[i].Name, parameters[i].Name))
							values.Add(constructor.Parameters[i].GetValue(parameters[i].ParameterType));
						else if(constructor.Parameters[i].ParameterType == parameters[i].ParameterType)
							values.Add(constructor.Parameters[i].GetValue(parameters[i].ParameterType));
						else
							break;
					}

					//如果匹配成功的参数数量与当前构造函数的参数数量相等则表示匹配成功
					if(values.Count == parameters.Length)
						return (constructorInfo, values.ToArray());
				}

				//按照构造函数参数数量多少依次匹配
				foreach(ConstructorInfo constructorInfo in type.GetConstructors().OrderByDescending(ctor => ctor.GetParameters().Length))
				{
					var parameters = constructorInfo.GetParameters();

					//如果构造函数的参数数量小于声明的参数数量则匹配失败
					if(parameters.Length < constructor.Count)
						continue;

					for(int i = 0; i < parameters.Length; i++)
					{
						if(constructor.Parameters.TryGet(parameters[i].Name, out var parameter))
							values.Add(parameter.GetValue(parameters[i].ParameterType));
						else if(constructor.Parameters.TryGet(parameters[i].ParameterType, out parameter))
							values.Add(parameter.GetValue(parameters[i].ParameterType));
						else if(GetParameterValue(constructor.Builtin, parameters[i], out var value))
							values.Add(value);
						else
							break;
					}

					//如果匹配成功的参数数量与当前构造函数的参数数量相等则表示匹配成功
					if(values.Count == parameters.Length)
						return (constructorInfo, values.ToArray());
				}

				return default;
			}
		}

		internal static object BuildType(string typeName, Builtin builtin)
		{
			Type type = PluginUtility.GetType(typeName, builtin);

			if(type == null)
				throw new PluginException(string.Format("Can not get type from '{0}' text for '{1}' builtin.", typeName, builtin));

			return BuildType(type, builtin);
		}

		internal static object BuildType(Type type, PluginElement element)
		{
			object target;

			try
			{
				target = BuildType(type, (ParameterInfo parameter, out object value) => GetParameterValue(element, parameter, out value));

				if(target == null)
					throw new PluginException(string.Format("Can not build instance of '{0}' type, Maybe that's cause type-generator not found matched constructor with parameters. in '{1}'.", type.FullName, element));
			}
			catch(Exception ex)
			{
				if(System.Diagnostics.Debugger.IsAttached)
					throw;

				throw new PluginException(string.Format("Occurred an exception on create a builtin instance of '{0}' type, at '{1}' builtin.", type.FullName, element), ex);
			}

			//注入依赖属性
			InjectProperties(target, element);

			return target;
		}

		private static object BuildType(Type type, ParameterResolver parameterResolver)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(parameterResolver == null)
				throw new ArgumentNullException(nameof(parameterResolver));

			if(type.IsInterface || type.IsAbstract)
				throw new ArgumentException($"Unable to create an instance of the specified '{type.FullName}' type because it is an interface or an abstract class.");

			var constructors = type.GetConstructors();

			foreach(ConstructorInfo constructor in constructors.OrderByDescending(ctor => ctor.GetParameters().Length))
			{
				var parameters = constructor.GetParameters();

				if(parameters.Length == 0)
					return Activator.CreateInstance(type);

				bool matched = false;
				object[] values = new object[parameters.Length];

				for(int i = 0; i < parameters.Length; i++)
				{
					//依次获取当前构造函数的参数值
					matched = parameterResolver(parameters[i], out values[i]);

					//如果获取参数值失败，则当前构造函数匹配失败
					if(!matched)
						break;
				}

				if(matched)
					return Activator.CreateInstance(type, values);
			}

			return null;
		}
		#endregion

		#region 参数回调
		private static bool GetParameterValue(PluginElement element, ParameterInfo parameter, out object result)
		{
			if(parameter.ParameterType == typeof(string) && string.Equals(parameter.Name, "name", StringComparison.OrdinalIgnoreCase))
			{
				result = element.Name;
				return true;
			}

			if(parameter.ParameterType == typeof(Assembly))
			{
				result = GetPluginAssembly(element.Plugin);
				return result != null;
			}

			if(parameter.ParameterType == typeof(Builtin))
			{
				result = element as Builtin;
				return result != null;
			}

			if(parameter.ParameterType == typeof(PluginTreeNode))
			{
				result = element.GetNode();
				return result != null;
			}

			if(parameter.ParameterType == typeof(Plugin))
			{
				result = element.Plugin;
				return result != null;
			}

			if(parameter.ParameterType == typeof(PluginTree))
			{
				result = element.Plugin?.PluginTree;
				return result != null;
			}

			if(typeof(IApplicationContext).IsAssignableFrom(parameter.ParameterType))
			{
				result = ApplicationContext.Current;
				return true;
			}

			if(typeof(IApplicationModule).IsAssignableFrom(parameter.ParameterType))
			{
				result = FindApplicationModule(element);
				return result != null;
			}

			if(typeof(IServiceProvider).IsAssignableFrom(parameter.ParameterType))
			{
				result = FindServiceProvider(element);
				return result != null;
			}

			if(element is Builtin builtin && builtin.HasProperties && builtin.Properties.TryGetValue(parameter.Name, out var property) && property.Value != null)
			{
				var converter = TypeDescriptor.GetConverter(parameter);
				var propertyType = property.Type ?? property.Value?.GetType();

				if(converter != null && propertyType != null && converter.CanConvertFrom(propertyType))
				{
					result = converter.ConvertFrom(property.Value);
					return true;
				}

				if(Common.Convert.TryConvertValue(property.Value, parameter.ParameterType, out result))
					return true;
			}

			var services = FindServiceProvider(element);
			if(services != null)
			{
				result = services.GetService(parameter.ParameterType);
				return result != null || parameter.HasDefaultValue;
			}

			if(parameter.HasDefaultValue)
			{
				result = parameter.DefaultValue;
				return true;
			}

			result = null;
			return false;
		}
		#endregion

		#region 其他方法
		internal static Assembly GetPluginAssembly(Plugin plugin)
		{
			if(plugin == null)
				return null;

			if(plugin.Manifest.Assemblies.Count == 0)
				return GetPluginAssembly(plugin.Parent);

			foreach(var assembly in plugin.Manifest.Assemblies)
			{
				if(string.Equals(plugin.Name, assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
					return assembly;
			}

			return plugin.Manifest.Assemblies[0];
		}

		internal static IApplicationModule FindApplicationModule(PluginElement element) => FindApplicationModule(element, out _);
		internal static IApplicationModule FindApplicationModule(PluginElement element, out PluginTreeNode node)
		{
			node = element.GetNode();

			while(node != null)
			{
				var valueType = node.ValueType;

				if(valueType == null || typeof(IApplicationModule).IsAssignableFrom(valueType))
				{
					var value = node.UnwrapValue(ObtainMode.Auto, Builders.BuilderSettings.Create(Builders.BuilderSettingsFlags.IgnoreChildren));

					if(value != null && value is IApplicationModule module)
						return module;
				}

				node = node.Parent;
			}

			return null;
		}

		internal static IServiceProvider FindServiceProvider(PluginElement element)
		{
			var module = FindApplicationModule(element, out var node);

			if(module != null && module.Services != null)
				return module.Services;

			if(node != null && node.Parent != null && ApplicationContext.Current.Modules.TryGetValue(node.Parent.Name, out module))
				return module.Services;

			return ApplicationContext.Current.Services;
		}

		internal static PluginTreeNode GetNode(this PluginElement element) => element switch
		{
			PluginTreeNode node => node,
			Builtin builtin => builtin.Node,
			_ => null,
		};

		internal static int GetAnonymousId()
		{
			return System.Threading.Interlocked.Increment(ref _anonymousId);
		}

		internal static object ResolveValue(PluginElement element, string text, string memberName, Type memberType, TypeConverter converter, object defaultValue)
		{
			if(element == null)
				throw new ArgumentNullException(nameof(element));

			if(string.IsNullOrWhiteSpace(text))
				return Zongsoft.Common.Convert.ConvertValue(text, memberType, () => converter, defaultValue);

			object result = text;

			//进行解析器处理，如果解析器无法处理将会返回传入的原始值
			if(Parsers.Parser.CanParse(text))
			{
				if(element is Builtin)
					result = Parsers.Parser.Parse(text, (Builtin)element, memberName, memberType);
				else if(element is PluginTreeNode)
					result = Parsers.Parser.Parse(text, (PluginTreeNode)element, memberName, memberType);
				else
					throw new NotSupportedException(string.Format("Can not support the '{0}' element type.", element.GetType()));
			}

			//对最后的结果进行类型转换，如果指定的类型为空，该转换操作不会执行任何动作
			if(memberType == null)
				return result;
			else
				return Zongsoft.Common.Convert.ConvertValue(result, memberType, () => converter, defaultValue);
		}

		internal static Assembly LoadAssembly(AssemblyName assemblyName)
		{
			if(assemblyName == null)
				return null;

			return AppDomain.CurrentDomain.Load(assemblyName);
		}

		internal static Assembly ResolveAssembly(AssemblyName assemblyName)
		{
			if(assemblyName == null)
				return null;

			var token = assemblyName.GetPublicKeyToken();
			var assemblies = new List<Assembly>();

			foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var matched = string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase);

				if(token != null && token.Length > 0)
				{
					var pk = assembly.GetName().GetPublicKeyToken();
					matched = matched && pk != null && Enumerable.SequenceEqual(token, pk);
				}

				if(matched)
					assemblies.Add(assembly);
			}

			if(assemblies.Count < 1)
			{
				if(assemblyName.Name.StartsWith("System."))
					return AppDomain.CurrentDomain.Load(assemblyName.Name);

				return null;
			}

			if(assemblies.Count == 1)
				return assemblies[0];

			var maxAssembly = assemblies[0];

			foreach(Assembly assembly in assemblies)
			{
				if(assembly.GetName().Version == null)
					continue;

				if(assembly.GetName().Version.CompareTo(maxAssembly.GetName().Version) > 0)
					maxAssembly = assembly;
			}

			return maxAssembly;
		}

		internal static MemberInfo GetStaticMember(string qualifiedName)
		{
			if(string.IsNullOrWhiteSpace(qualifiedName))
				return null;

			var parts = qualifiedName.Split(',');

			if(parts.Length != 2)
				throw new ArgumentException(string.Format("Invalid qualified name '{0}'.", qualifiedName));

			var assemblyName = parts[1].Trim();

			if(string.IsNullOrWhiteSpace(assemblyName))
				throw new ArgumentException(string.Format("Missing assembly name in the qualified name '{0}'.", qualifiedName));

			//根据指定程序集名称获取对应的程序集
			var assembly = ResolveAssembly(new AssemblyName(assemblyName));

			if(assembly == null)
				throw new InvalidOperationException(string.Format("Not found '{0}' assembly in the runtimes, for '{1}' qualified type name.", assemblyName, qualifiedName));

			//分解类型成员的完整路径
			parts = parts[0].Split('.');

			//不能小于三个部分，因为「Namespace.Type.Member」至少包含三个部分
			if(parts.Length < 3)
				return null;

			var typeFullName = string.Join(".", parts, 0, parts.Length - 1);
			var type = assembly.GetType(typeFullName, false);

			if(type == null)
				throw new ArgumentException(string.Format("Cann't obtain the type by '{0}' type-name in the '{1}' assembly.", typeFullName, assembly.FullName));

			//获取指定的成员信息
			return type.GetMember(parts[parts.Length - 1], (MemberTypes.Field | MemberTypes.Property), BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
		}

		private static void InjectProperties(object target, PluginElement element)
		{
			if(target == null || element == null)
				return;

			//使用服务注入器进行注入处理
			ServiceInjector.Inject(FindServiceProvider(element), target);
		}

		internal static Type GetOwnerElementType(PluginTreeNode node)
		{
			var ownerNode = node.Tree.GetOwnerNode(node);

			if(ownerNode == null)
				return null;

			var ownerType = ownerNode.ValueType;

			if(ownerType == null)
			{
				var owner = ownerNode.UnwrapValue(ObtainMode.Never);

				if(owner == null)
					return null;

				ownerType = owner.GetType();
			}

			var elementType = GetImplementedCollectionElementType(ownerType);

			if(elementType != null)
				return elementType;

			MemberInfo[] members = null;
			var memberAttribute = ownerType.GetCustomAttribute<DefaultMemberAttribute>(true);

			if(memberAttribute != null)
				members = ownerType.GetMember(memberAttribute.MemberName, BindingFlags.Public | BindingFlags.Instance);
			else
			{
				var propertyAttribute = ownerType.GetCustomAttribute<DefaultPropertyAttribute>(true);
				if(propertyAttribute != null)
					members = ownerType.GetMember(propertyAttribute.Name, BindingFlags.Public | BindingFlags.Instance);
			}

			if(members != null && members.Length == 1)
			{
				switch(members[0].MemberType)
				{
					case MemberTypes.Field:
						return GetImplementedCollectionElementType(((FieldInfo)members[0]).FieldType);
					case MemberTypes.Property:
						return GetImplementedCollectionElementType(((PropertyInfo)members[0]).PropertyType);
				}
			}

			return null;
		}

		private static Type GetImplementedCollectionElementType(Type instanceType)
		{
			if(instanceType == null)
				return null;

			if(instanceType.IsGenericType)
			{
				var prototype = instanceType.GetGenericTypeDefinition();

				if(prototype == typeof(IDictionary<,>))
					return instanceType.GenericTypeArguments[1];
				if(prototype == typeof(ICollection<>))
					return instanceType.GenericTypeArguments[0];
			}

			var contracts = instanceType.GetInterfaces();

			foreach(var contract in contracts)
			{
				if(contract.IsGenericType)
				{
					var prototype = contract.GetGenericTypeDefinition();

					if(prototype == typeof(IDictionary<,>))
						return contract.GenericTypeArguments[1];
					if(prototype == typeof(ICollection<>))
						return contract.GenericTypeArguments[0];
				}
			}

			return null;
		}

		internal static Type GetMemberType(IMemberExpression expression, Type originType)
		{
			if(originType == null)
				throw new ArgumentNullException(nameof(originType));

			if(expression == null)
				return originType;

			var element = expression;
			var elementType = originType;

			while(element != null)
			{
				switch(element.ExpressionType)
				{
					case MemberExpressionType.Constant:
						elementType = ((ConstantExpression)element).Value?.GetType();
						break;
					case MemberExpressionType.Identifier:
						var identifier = (IdentifierExpression)element;

						elementType = elementType.GetProperty(identifier.Name)?.PropertyType ??
									  elementType.GetField(identifier.Name)?.FieldType;
						break;
					case MemberExpressionType.Indexer:
						var memberName = elementType.GetCustomAttribute<DefaultMemberAttribute>()?.MemberName;

						if(string.IsNullOrEmpty(memberName))
							throw new InvalidOperationException("");

						elementType = elementType.GetProperty(memberName)?.PropertyType;
						break;
					case MemberExpressionType.Method:
						var method = (MethodExpression)element;

						if(method.HasArguments)
						{
							var parameterTypes = new Type[method.Arguments.Count];

							for(var i = 0; i < method.Arguments.Count; i++)
							{
								parameterTypes[i] = GetMemberType(method.Arguments[i], originType);
							}

							elementType = elementType.GetMethod(method.Name, parameterTypes)?.ReturnType;
						}
						else
							elementType = elementType.GetMethod(method.Name)?.ReturnType;
						break;
				}

				element = element.Next;
			}

			return elementType;
		}
		#endregion
	}
}
