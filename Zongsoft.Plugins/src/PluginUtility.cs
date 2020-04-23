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
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zongsoft.Services;
using Zongsoft.Reflection;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Plugins
{
	public static class PluginUtility
	{
		#region 私有变量
		private static int _anonymousId;
		#endregion

		#region 获取类型
		/// <summary>
		/// 根据指定的类型限定名动态加载并返回对应的<seealso cref="System.Type"/>，如果查找失败亦不会抛出异常。
		/// </summary>
		/// <param name="typeFullName">要获取的类型限定名称。</param>
		/// <returns>返回加载成功的类型对象，如果加载失败则返回空(null)。</returns>
		public static Type GetType(string typeFullName)
		{
			if(string.IsNullOrWhiteSpace(typeFullName))
				return null;

			Type type = GetTypeFromAlias(typeFullName);

			if(type != null)
				return type;

			type = Type.GetType(typeFullName, assemblyName =>
			{
				Assembly assembly = ResolveAssembly(assemblyName);

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
				throw new PluginException(string.Format("The '{0}' type resolve failed.", typeFullName));

			return type;
		}

		public static Type GetType(Builtin builtin)
		{
			if(builtin == null)
				return null;

			if(builtin.BuiltinType != null)
				return builtin.BuiltinType.Type;
			else
				return GetType(builtin.Properties.GetValue<string>("type"));
		}

		private static Type GetTypeFromAlias(string typeName)
		{
			if(string.IsNullOrEmpty(typeName))
				return null;

			switch(typeName.Replace(" ", "").ToLowerInvariant())
			{
				case "string":
					return typeof(string);
				case "string[]":
					return typeof(string[]);

				case "int":
					return typeof(int);
				case "int?":
					return typeof(int?);
				case "int[]":
					return typeof(int[]);

				case "long":
					return typeof(long);
				case "long?":
					return typeof(long?);
				case "long[]":
					return typeof(long[]);

				case "short":
					return typeof(short);
				case "short?":
					return typeof(short?);
				case "short[]":
					return typeof(short[]);

				case "byte":
					return typeof(byte);
				case "byte?":
					return typeof(byte?);
				case "byte[]":
					return typeof(byte[]);

				case "bool":
				case "boolean":
					return typeof(bool);
				case "bool?":
				case "boolean?":
					return typeof(bool?);
				case "bool[]":
				case "boolean[]":
					return typeof(bool[]);

				case "money":
				case "number":
				case "numeric":
				case "decimal":
					return typeof(decimal);
				case "money?":
				case "number?":
				case "numeric?":
				case "decimal?":
					return typeof(decimal?);
				case "money[]":
				case "number[]":
				case "numeric[]":
				case "decimal[]":
					return typeof(decimal[]);

				case "float":
				case "single":
					return typeof(float);
				case "float?":
				case "single?":
					return typeof(float?);
				case "float[]":
				case "single[]":
					return typeof(float[]);

				case "double":
					return typeof(double);
				case "double?":
					return typeof(double?);
				case "double[]":
					return typeof(double[]);

				case "uint":
					return typeof(uint);
				case "uint?":
					return typeof(uint?);
				case "uint[]":
					return typeof(uint[]);

				case "ulong":
					return typeof(ulong);
				case "ulong?":
					return typeof(ulong?);
				case "ulong[]":
					return typeof(ulong[]);

				case "ushort":
					return typeof(ushort);
				case "ushort?":
					return typeof(ushort?);
				case "ushort[]":
					return typeof(ushort[]);

				case "sbyte":
					return typeof(sbyte);
				case "sbyte?":
					return typeof(sbyte?);
				case "sbyte[]":
					return typeof(sbyte[]);

				case "char":
					return typeof(char);
				case "char?":
					return typeof(char?);
				case "char[]":
					return typeof(char[]);

				case "date":
				case "time":
				case "datetime":
					return typeof(DateTime);
				case "date?":
				case "time?":
				case "datetime?":
					return typeof(DateTime?);
				case "date[]":
				case "time[]":
				case "datetime[]":
					return typeof(DateTime[]);

				case "timespan":
					return typeof(TimeSpan);
				case "timespan?":
					return typeof(TimeSpan?);
				case "timespan[]":
					return typeof(TimeSpan[]);

				case "guid":
					return typeof(Guid);
				case "guid?":
					return typeof(Guid?);
				case "guid[]":
					return typeof(Guid[]);
			}

			return null;
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
					throw new PluginException($"Unable to determine the target type of the '{builtin.ToString()}' builtin.");

				result = BuildType(type, builtin);
			}

			//设置更新目标对象的属性集
			if(result != null)
				UpdateProperties(result, builtin, ignoredProperties);

			return result;
		}

		internal static void UpdateProperties(object target, Builtin builtin, IEnumerable<string> ignoredProperties)
		{
			if(target == null || builtin == null || !builtin.HasProperties)
				return;

			foreach(var property in builtin.Properties)
			{
				//如果当前属性名为忽略属性则忽略设置
				if(ignoredProperties != null && ignoredProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
					continue;

				try
				{
					string converterTypeName = null;
					//Type propertyType;

					//如果构件中当前属性名在目标对象中不存在则记录告警日志
					//注意：对于不存在的集合成员的获取类型可能会失败，但是下面的设置操作可能会成功，因此这里不能直接返回。
					//if(!Reflection.MemberAccess.TryGetMemberType(target, property.Name, out propertyType, out converterTypeName))
					//	Zongsoft.Diagnostics.Logger.Warn($"The '{property.Name}' property of '{builtin}' builtin is not existed on '{target.GetType().FullName}' target.");

					//更新属性类型
					//property.Type = propertyType;

					var propertyInfo = target.GetType().GetProperty(property.Name);

					if(propertyInfo != null)
					{
						property.Type = propertyInfo.PropertyType;
						converterTypeName = propertyInfo.GetCustomAttribute<TypeConverterAttribute>()?.ConverterTypeName;
					}
					else
					{
						var fieldInfo = target.GetType().GetField(property.Name);

						if(fieldInfo != null)
						{
							property.Type = fieldInfo.FieldType;
							converterTypeName = fieldInfo.GetCustomAttribute<TypeConverterAttribute>()?.ConverterTypeName;
						}
					}

					//更新属性的类型转换器
					if(converterTypeName != null && converterTypeName.Length > 0)
					{
						property.Converter = Activator.CreateInstance(GetType(converterTypeName)) as TypeConverter ??
						                     throw new InvalidOperationException($"The '{converterTypeName}' type declared by the '{property.Name}' member of type '{target.GetType().FullName}' is not a type converter.");
					}

					//将构件中当前属性值更新目标对象的对应属性中
					Reflector.TrySetValue(target, property.Name, property.Value);
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

			object target;

			if(builtinType.Constructor == null || builtinType.Constructor.Count < 1)
			{
				target = BuildType(builtinType.TypeName, builtinType.Builtin);
			}
			else
			{
				object[] values = new object[builtinType.Constructor.Count];

				for(int i = 0; i < values.Length; i++)
				{
					values[i] = builtinType.Constructor.Parameters[i].GetValue(null);
				}

				try
				{
					target = Activator.CreateInstance(builtinType.Type, values);
				}
				catch(Exception ex)
				{
					if(System.Diagnostics.Debugger.IsAttached)
						throw;

					throw new PluginException(string.Format("Create object of '{0}' type faild, The parameters count of constructor is {1}.", builtinType.TypeName, values.Length), ex);
				}

				//注入依赖属性
				InjectProperties(target, builtinType.Builtin);
			}

			return target;
		}

		internal static object BuildType(string typeName, Builtin builtin)
		{
			Type type = PluginUtility.GetType(typeName);

			if(type == null)
				throw new PluginException(string.Format("Can not get type from '{0}' text for '{1}' builtin.", typeName, builtin));

			return BuildType(type, builtin);
		}

		internal static object BuildType(Type type, Builtin builtin)
		{
			object target;

			try
			{
				target = BuildType(type, (Type parameterType, string parameterName, out object parameterValue) =>
				{
					if(parameterType == typeof(Builtin))
					{
						parameterValue = builtin;
						return true;
					}

					if(parameterType == typeof(PluginTreeNode))
					{
						parameterValue = builtin.Node;
						return true;
					}

					if(parameterType == typeof(string) && string.Equals(parameterName, "name", StringComparison.OrdinalIgnoreCase))
					{
						parameterValue = builtin.Name;
						return true;
					}

					if(typeof(System.IServiceProvider).IsAssignableFrom(parameterType))
					{
						parameterValue = FindServiceProvider(builtin);
						return true;
					}

					if(typeof(IApplicationContext).IsAssignableFrom(parameterType))
					{
						parameterValue = ApplicationContext.Current;
						return true;
					}

					if(typeof(IApplicationModule).IsAssignableFrom(parameterType))
					{
						parameterValue = FindApplicationModule(builtin);
						return true;
					}

					return ObtainParameter(builtin.Plugin, parameterType, parameterName, out parameterValue);
				});

				if(target == null)
					throw new PluginException(string.Format("Can not build instance of '{0}' type, Maybe that's cause type-generator not found matched constructor with parameters. in '{1}' builtin.", type.FullName, builtin));
			}
			catch(Exception ex)
			{
				if(System.Diagnostics.Debugger.IsAttached)
					throw;

				throw new PluginException(string.Format("Occurred an exception on create a builtin instance of '{0}' type, at '{1}' builtin.", type.FullName, builtin), ex);
			}

			//注入依赖属性
			InjectProperties(target, builtin);

			return target;
		}

		internal static object BuildType(Type type, ObtainParameterCallback obtainParameter)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(obtainParameter == null)
				throw new ArgumentNullException(nameof(obtainParameter));

			if(type.IsInterface || type.IsAbstract)
				throw new ArgumentException($"Unable to create an instance of the specified '{type.FullName}' type because it is an interface or an abstract class.");

			ConstructorInfo[] constructors = type.GetConstructors();

			foreach(ConstructorInfo constructor in constructors.OrderByDescending(ctor => ctor.GetParameters().Length))
			{
				ParameterInfo[] parameters = constructor.GetParameters();

				if(parameters.Length == 0)
					return Activator.CreateInstance(type);

				bool matched = false;
				object[] values = new object[parameters.Length];

				for(int i = 0; i < parameters.Length; i++)
				{
					//依次获取当前构造函数的参数值
					matched = obtainParameter(parameters[i].ParameterType, parameters[i].Name, out values[i]);

					//如果获取参数值失败，则当前构造函数匹配失败
					if(!matched)
						break;
				}

				if(matched)
					return Activator.CreateInstance(type, values);
			}

			return null;
		}

		#region 委托定义
		internal delegate bool ObtainParameterCallback(Type parameterType, string parameterName, out object parameterValue);
		#endregion

		internal static bool ObtainParameter(Plugin plugin, Type parameterType, string parameterName, out object parameterValue)
		{
			if(parameterType == typeof(Plugin))
			{
				parameterValue = plugin;
				return true;
			}

			if(parameterType == typeof(PluginTree))
			{
				parameterValue = plugin.PluginTree;
				return true;
			}

			if(typeof(IApplicationContext).IsAssignableFrom(parameterType))
			{
				parameterValue = ApplicationContext.Current;
				return true;
			}

			if(typeof(IServiceProviderFactory).IsAssignableFrom(parameterType))
			{
				parameterValue = Services.ServiceProviderFactory.Instance;
				return true;
			}

			if(typeof(System.IServiceProvider).IsAssignableFrom(parameterType))
			{
				parameterValue = ApplicationContext.Current.Services;
				return true;
			}

			parameterValue = null;
			return false;
		}
		#endregion

		internal static IApplicationModule FindApplicationModule(Builtin builtin)
		{
			if(builtin == null || builtin.Node == null || builtin.Node.Parent == null)
				return null;

			var node = builtin.Node;

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

		internal static System.IServiceProvider FindServiceProvider(Builtin builtin)
		{
			if(builtin == null)
				return null;

			var module = FindApplicationModule(builtin);

			if(module != null && module.Services != null)
				return module.Services;

			if(builtin.Node != null && builtin.Node.Parent != null)
				return ServiceProviderFactory.Instance.GetProvider(builtin.Node.Parent.Name) ?? ApplicationContext.Current.Services;

			return ApplicationContext.Current.Services;
		}

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
					matched &= CompareBytes(token, assembly.GetName().GetPublicKeyToken());

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

		private static void InjectProperties(object target, Builtin builtin)
		{
			if(target == null || builtin == null)
				return;

			//查找指定目标对象需要注入的属性和字段集(支持对非公共成员的注入)
			var members = target.GetType()
			                    .FindMembers(MemberTypes.Field | MemberTypes.Property,
			                    	BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			                    	(m, t) => m.GetCustomAttribute((Type)t, true) != null,
			                    	typeof(Zongsoft.Services.ServiceDependencyAttribute));

			if(members == null || members.Length < 1)
				return;

			//定义成员值变量
			object memberValue;

			//获取当前构件所属的服务容器（后备服务容器）
			var reservedProvider = FindServiceProvider(builtin);

			//定义成员对应的服务容器（默认为后备服务容器）
			var serviceProvider = reservedProvider;

			foreach(var member in members)
			{
				//如果当前成员已经在构件属性集中显式存在则跳过
				if(builtin.HasProperties && builtin.Properties.Contains(member.Name))
					continue;

				//获取需注入成员的注入标记
				var attribute = member.GetCustomAttribute<ServiceDependencyAttribute>(true);

				if(attribute == null || string.IsNullOrWhiteSpace(attribute.Provider))
					serviceProvider = reservedProvider;
				else
					serviceProvider = ServiceProviderFactory.Instance.GetProvider(attribute.Provider) ?? reservedProvider;

				switch(member.MemberType)
				{
					case MemberTypes.Field:
						if(attribute.Parameter == null)
							memberValue = serviceProvider.GetService(attribute.ServiceType ?? ((FieldInfo)member).FieldType);
						else
							memberValue = serviceProvider.Match(attribute.ServiceType ?? ((FieldInfo)member).FieldType, attribute.Parameter);

						if(memberValue == null && attribute != null && attribute.IsRequired)
							throw new InvalidOperationException($"The injected {((PropertyInfo)member).Name} field value is null when building the '{builtin}' plugin builtin.");

						((FieldInfo)member).SetValue(target, memberValue);
						break;
					case MemberTypes.Property:
						if(((PropertyInfo)member).CanWrite)
						{
							if(attribute.Parameter == null)
								memberValue = serviceProvider.GetService(attribute.ServiceType ?? ((PropertyInfo)member).PropertyType);
							else
								memberValue = serviceProvider.Match(attribute.ServiceType ?? ((PropertyInfo)member).PropertyType, attribute.Parameter);

							if(memberValue == null && attribute != null && attribute.IsRequired)
								throw new InvalidOperationException($"The injected {((PropertyInfo)member).Name} property value is null when building the '{builtin}' plugin builtin.");

							((PropertyInfo)member).SetValue(target, memberValue);
						}

						break;
				}
			}
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

			var types = instanceType.GetInterfaces();

			foreach(var type in types)
			{
				if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
					return type.GetGenericArguments()[0];
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

		private static bool CompareBytes(byte[] a, byte[] b)
		{
			if(a == null && b == null)
				return true;
			if(a == null || b == null)
				return false;
			if(a.Length != b.Length)
				return false;

			for(int i = 0; i < a.Length; i++)
			{
				if(a[i] != b[i])
					return false;
			}

			return true;
		}
	}
}
