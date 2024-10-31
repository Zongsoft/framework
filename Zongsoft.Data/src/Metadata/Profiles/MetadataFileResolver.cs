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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles
{
	public class MetadataFileResolver
	{
		#region 静态字段
		private static MetadataFileResolver _default;
		#endregion

		#region 常量定义
		public const string XML_NAMESPACE_URI = "http://schemas.zongsoft.com/data";

		private const string XML_SCHEMA_ELEMENT = "schema";
		private const string XML_CONTAINER_ELEMENT = "container";
		private const string XML_ENTITY_ELEMENT = "entity";
		private const string XML_KEY_ELEMENT = "key";
		private const string XML_MEMBER_ELEMENT = "member";
		private const string XML_PROPERTY_ELEMENT = "property";
		private const string XML_COMPLEXPROPERTY_ELEMENT = "complexProperty";
		private const string XML_COMMAND_ELEMENT = "command";
		private const string XML_SCRIPT_ELEMENT = "script";
		private const string XML_PARAMETER_ELEMENT = "parameter";
		private const string XML_LINK_ELEMENT = "link";
		private const string XML_CONSTRAINT_ELEMENT = "constraint";
		private const string XML_CONSTRAINTS_ELEMENT = "constraints";

		private const string XML_NAME_ATTRIBUTE = "name";
		private const string XML_TYPE_ATTRIBUTE = "type";
		private const string XML_HINT_ATTRIBUTE = "hint";
		private const string XML_ROLE_ATTRIBUTE = "role";
		private const string XML_ALIAS_ATTRIBUTE = "alias";
		private const string XML_FIELD_ATTRIBUTE = "field";
		private const string XML_TABLE_ATTRIBUTE = "table";
		private const string XML_INHERITS_ATTRIBUTE = "inherits";
		private const string XML_LENGTH_ATTRIBUTE = "length";
		private const string XML_DEFAULT_ATTRIBUTE = "default";
		private const string XML_NULLABLE_ATTRIBUTE = "nullable";
		private const string XML_SORTABLE_ATTRIBUTE = "sortable";
		private const string XML_SEQUENCE_ATTRIBUTE = "sequence";
		private const string XML_PRECISION_ATTRIBUTE = "precision";
		private const string XML_SCALE_ATTRIBUTE = "scale";
		private const string XML_DIRECTION_ATTRIBUTE = "direction";
		private const string XML_IMMUTABLE_ATTRIBUTE = "immutable";
		private const string XML_MULTIPLICITY_ATTRIBUTE = "multiplicity";
		private const string XML_PORT_ATTRIBUTE = "port";
		private const string XML_ANCHOR_ATTRIBUTE = "anchor";
		private const string XML_ACTOR_ATTRIBUTE = "actor";
		private const string XML_VALUE_ATTRIBUTE = "value";
		private const string XML_PATH_ATTRIBUTE = "path";
		private const string XML_DRIVER_ATTRIBUTE = "driver";
		private const string XML_BEHAVIORS_ATTRIBUTE = "behaviors";
		private const string XML_MUTABILITY_ATTRIBUTE = "mutability";
		#endregion

		#region 构造函数
		protected MetadataFileResolver() { }
		#endregion

		#region 静态属性
		public static MetadataFileResolver Default
		{
			get
			{
				if(_default == null)
					System.Threading.Interlocked.CompareExchange(ref _default, new MetadataFileResolver(), null);

				return _default;
			}
			set
			{
				_default = value;
			}
		}
		#endregion

		#region 公共方法
		public MetadataFile Resolve(string filePath, string name)
		{
			using(var reader = this.CreateReader((settings, context) => XmlReader.Create(filePath, settings, context)))
			{
				return this.Resolve(reader, filePath, name);
			}
		}

		public MetadataFile Resolve(Stream stream, string name)
		{
			using(var reader = this.CreateReader((settings, context) => XmlReader.Create(stream, settings, context)))
			{
				return this.Resolve(reader, (stream is FileStream fs ? fs.Name : null), name);
			}
		}

		public MetadataFile Resolve(TextReader reader, string name)
		{
			using(var xmlReader = this.CreateReader((settings, context) => XmlReader.Create(reader, settings, context)))
			{
				return this.Resolve(xmlReader, null, name);
			}
		}

		public MetadataFile Resolve(XmlReader reader, string name) => this.Resolve(reader, null, name);
		public MetadataFile Resolve(XmlReader reader, string filePath, string name)
		{
			if(reader == null)
				throw new ArgumentNullException(nameof(reader));

			if(string.IsNullOrWhiteSpace(filePath))
				filePath = reader.BaseURI;

			try
			{
				reader.MoveToContent();
			}
			catch(Exception ex)
			{
				if(ex is MetadataFileException)
					throw;

				throw new MetadataFileException($"Invalid '{filePath}' mapping file.", ex);
			}

			if(reader.LocalName != XML_SCHEMA_ELEMENT)
				throw new MetadataFileException(string.Format("The root element must be '<{0}>' in this '{1}' file.", XML_SCHEMA_ELEMENT, filePath));

			//获取映射文件所属的应用名
			var applicationName = reader.GetAttribute(XML_NAME_ATTRIBUTE);

			if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(applicationName))
			{
				if(!string.Equals(name, applicationName, StringComparison.OrdinalIgnoreCase))
					return null;
			}

			//创建待返回的映射文件描述对象
			var file = new MetadataFile(filePath, applicationName);

			while(reader.Read() && reader.NodeType == XmlNodeType.Element)
			{
				if(!string.Equals(reader.LocalName, XML_CONTAINER_ELEMENT))
					throw new MetadataFileException();

				//如果是空元素则继续下一个
				if(reader.IsEmptyElement)
					continue;

				//获取当前容器的名称（即子元素的命名空间）
				var @namespace = reader.GetAttribute(XML_NAME_ATTRIBUTE);

				int depth = reader.Depth;

				while(reader.Read() && reader.Depth > depth)
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.LocalName)
					{
						case XML_ENTITY_ELEMENT:
							var entity = ResolveEntity(reader, file, @namespace, () => this.ProcessUnrecognizedElement(reader, file, @namespace));

							if(entity != null)
								file.Entities.Add(entity);

							break;
						case XML_COMMAND_ELEMENT:
							var command = ResolveCommand(reader, file, @namespace, () => this.ProcessUnrecognizedElement(reader, file, @namespace));

							if(command != null)
								file.Commands.Add(command);

							break;
						default:
							this.ProcessUnrecognizedElement(reader, file, @namespace);
							break;
					}
				}
			}

			return file;
		}
		#endregion

		#region 解析方法
		private static MetadataEntity ResolveEntity(XmlReader reader, MetadataFile provider, string @namespace, Action unrecognize)
		{
			//创建实体元素对象
			var entity = new MetadataEntity(@namespace,
				reader.GetAttribute(XML_NAME_ATTRIBUTE),
				GetFullName(reader.GetAttribute(XML_INHERITS_ATTRIBUTE), @namespace),
				GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false));

			//设置其他属性
			entity.Alias = reader.GetAttribute(XML_ALIAS_ATTRIBUTE) ?? reader.GetAttribute(XML_TABLE_ATTRIBUTE);
			entity.Driver = reader.GetAttribute(XML_DRIVER_ATTRIBUTE);

			var keys = new HashSet<string>();
			int depth = reader.Depth;

			while(reader.Read() && reader.Depth > depth)
			{
				if(reader.NodeType != XmlNodeType.Element)
					continue;

				switch(reader.LocalName)
				{
					case XML_KEY_ELEMENT:
						while(reader.Read() && reader.Depth > depth + 1)
						{
							if(reader.NodeType != XmlNodeType.Element)
								continue;

							if(reader.Name == XML_MEMBER_ELEMENT)
								keys.Add(reader.GetAttribute(XML_NAME_ATTRIBUTE));
							else
								unrecognize();
						}

						break;
					case XML_PROPERTY_ELEMENT:
						var property = new MetadataEntitySimplexProperty(entity,
						                   reader.GetAttribute(XML_NAME_ATTRIBUTE),
										   GetDbType(GetAttributeValue<string>(reader, XML_TYPE_ATTRIBUTE)),
										   GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false))
						{
							Hint = GetAttributeValue<string>(reader, XML_HINT_ATTRIBUTE),
							Alias = GetAttributeValue<string>(reader, XML_ALIAS_ATTRIBUTE) ?? GetAttributeValue<string>(reader, XML_FIELD_ATTRIBUTE),
							Length = GetAttributeValue<int>(reader, XML_LENGTH_ATTRIBUTE),
							Precision = GetAttributeValue<byte>(reader, XML_PRECISION_ATTRIBUTE),
							Scale = GetAttributeValue<byte>(reader, XML_SCALE_ATTRIBUTE),
							Nullable = GetAttributeValue(reader, XML_NULLABLE_ATTRIBUTE, true),
							Sortable = GetAttributeValue(reader, XML_SORTABLE_ATTRIBUTE, false),
						};

						//设置默认值的字面量
						property.SetDefaultValue(GetAttributeValue<string>(reader, XML_DEFAULT_ATTRIBUTE));

						//设置序号器元数据信息
						property.SetSequence(GetAttributeValue<string>(reader, XML_SEQUENCE_ATTRIBUTE));

						//将解析成功的属性元素加入到实体的属性集合
						entity.Properties.Add(property);

						break;
					case XML_COMPLEXPROPERTY_ELEMENT:
						var complexProperty = new MetadataEntityComplexProperty(entity,
							reader.GetAttribute(XML_NAME_ATTRIBUTE),
							reader.GetAttribute(XML_PORT_ATTRIBUTE),
							GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false))
						{
							Hint = GetAttributeValue<string>(reader, XML_HINT_ATTRIBUTE),
							Behaviors = GetAttributeValue(reader, XML_BEHAVIORS_ATTRIBUTE, DataEntityComplexPropertyBehaviors.None),
						};

						var multiplicity = reader.GetAttribute(XML_MULTIPLICITY_ATTRIBUTE);

						if(multiplicity != null && multiplicity.Length > 0)
						{
							complexProperty.Multiplicity = multiplicity switch
							{
								"*" => DataAssociationMultiplicity.Many,
								"1" or "!" => DataAssociationMultiplicity.One,
								"?" or "0..1" => DataAssociationMultiplicity.ZeroOrOne,
								_ => throw new DataException($"Invalid '{multiplicity}' value of the multiplicity attribute."),
							};
						}

						var links = new List<DataAssociationLink>();

						while(reader.Read() && reader.Depth > depth + 1)
						{
							if(reader.NodeType != XmlNodeType.Element)
								continue;

							if(reader.LocalName == XML_LINK_ELEMENT)
							{
								links.Add(new DataAssociationLink(complexProperty,
										GetAttributeValue<string>(reader, XML_PORT_ATTRIBUTE),
										GetAttributeValue<string>(reader, XML_ANCHOR_ATTRIBUTE)
								));
							}
							else if(reader.LocalName == XML_CONSTRAINTS_ELEMENT)
							{
								if(reader.IsEmptyElement)
									continue;

								var constraints = new List<DataAssociationConstraint>();

								while(reader.Read() && reader.Depth > depth + 2)
								{
									if(reader.NodeType != XmlNodeType.Element)
										continue;

									if(reader.LocalName == XML_CONSTRAINT_ELEMENT)
									{
										constraints.Add(
											new DataAssociationConstraint(
												GetAttributeValue<string>(reader, XML_NAME_ATTRIBUTE),
												GetAttributeValue(reader, XML_ACTOR_ATTRIBUTE,
													complexProperty.Multiplicity == DataAssociationMultiplicity.Many ?
													DataAssociationConstraintActor.Foreign :
													DataAssociationConstraintActor.Principal),
												GetAttributeValue<object>(reader, XML_VALUE_ATTRIBUTE)));
									}
									else
									{
										unrecognize();
									}
								}

								if(constraints.Count > 0)
									complexProperty.Constraints = constraints.ToArray();
							}
							else
								unrecognize();
						}

						if(links == null || links.Count == 0)
							throw new DataException($"Missing links of the '{complexProperty.Name}' complex property in the '{provider.FilePath}' mapping file.");

						//设置复合属性的链接集属性
						complexProperty.Links = links.ToArray();

						//将解析成功的属性元素加入到实体的属性集合
						entity.Properties.Add(complexProperty);

						break;
					default:
						unrecognize();
						break;
				}
			}

			//设置实体的主键
			if(keys.Count > 0)
				entity.SetKey(keys);

			return entity;
		}

		private static MetadataCommand ResolveCommand(XmlReader reader, MetadataFile provider, string @namespace, Action unrecognize)
		{
			//创建命令元素对象
			var command = new MetadataCommand(@namespace,
				reader.GetAttribute(XML_NAME_ATTRIBUTE),
				reader.GetAttribute(XML_ALIAS_ATTRIBUTE))
			{
				Type = GetAttributeValue(reader, XML_TYPE_ATTRIBUTE, DataCommandType.Text),
				Alias = GetAttributeValue<string>(reader, XML_ALIAS_ATTRIBUTE),
				Driver = GetAttributeValue<string>(reader, XML_DRIVER_ATTRIBUTE),
				Mutability = GetAttributeValue(reader, XML_MUTABILITY_ATTRIBUTE, CommandMutability.Delete | CommandMutability.Insert | CommandMutability.Update),
			};

			//注意：首先尝试从外部脚本文件中加载命令文本
			command.Scriptor.Load(Path.GetDirectoryName(provider.FilePath), Path.GetFileNameWithoutExtension(provider.FilePath));

			int depth = reader.Depth;

			while(reader.Read() && reader.Depth > depth)
			{
				if(reader.NodeType != XmlNodeType.Element)
					continue;

				switch(reader.LocalName)
				{
					case XML_PARAMETER_ELEMENT:
						var parameter = new MetadataCommandParameter(command, reader.GetAttribute(XML_NAME_ATTRIBUTE), GetDbType(GetAttributeValue<string>(reader, XML_TYPE_ATTRIBUTE)))
						{
							Direction = GetAttributeValue(reader, XML_DIRECTION_ATTRIBUTE, value => GetDirection(value)),
							Alias = GetAttributeValue<string>(reader, XML_ALIAS_ATTRIBUTE),
							Length = GetAttributeValue<int>(reader, XML_LENGTH_ATTRIBUTE),
							Value = GetAttributeValue<object>(reader, XML_VALUE_ATTRIBUTE),
						};

						//将解析成功的命令参数元素加入到命令的参数集合
						command.Parameters.Add(parameter);

						break;
					case XML_SCRIPT_ELEMENT:
						var driver = reader.GetAttribute(XML_DRIVER_ATTRIBUTE);

						if(reader.Read() && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
							command.Scriptor.SetScript(driver, reader.Value);

						break;
					default:
						unrecognize();
						break;
				}
			}

			static System.Data.ParameterDirection GetDirection(string value)
			{
				if(string.IsNullOrEmpty(value))
					return System.Data.ParameterDirection.Input;

				if(Enum.TryParse<System.Data.ParameterDirection>(value, true, out var direction))
					return direction;

				return value.ToLowerInvariant() switch
				{
					"in" => System.Data.ParameterDirection.Input,
					"out" => System.Data.ParameterDirection.Output,
					"both" => System.Data.ParameterDirection.InputOutput,
					"result" => System.Data.ParameterDirection.ReturnValue,
					"return" => System.Data.ParameterDirection.ReturnValue,
					_ => throw new MetadataFileException($"Invalid value '{value}' of '{XML_DIRECTION_ATTRIBUTE}' attribute in '{XML_PARAMETER_ELEMENT}' element."),
				};
			}

			return command;
		}
		#endregion

		#region 虚拟方法
		protected virtual XmlReader CreateReader(Func<XmlReaderSettings, XmlParserContext, XmlReader> createThunk)
		{
			var nameTable = CreateXmlNameTable();
			var settings = new XmlReaderSettings()
			{
				CloseInput = false,
				IgnoreComments = true,
				IgnoreWhitespace = true,
				NameTable = nameTable,
				ValidationType = ValidationType.None,
			};

			var namespaceManager = new XmlNamespaceManager(nameTable);
			namespaceManager.AddNamespace(string.Empty, XML_NAMESPACE_URI);

			return createThunk(settings, new XmlParserContext(nameTable, namespaceManager, XML_SCHEMA_ELEMENT, XmlSpace.None));
		}

		protected virtual bool OnUnrecognizedElement(XmlReader reader, MetadataFile file, object container) => false;
		#endregion

		#region 私有方法
		private static System.Data.DbType GetDbType(string type)
		{
			if(string.IsNullOrWhiteSpace(type))
				return System.Data.DbType.String;

			return type.ToLowerInvariant() switch
			{
				"string" or "nvarchar" => System.Data.DbType.String,
				"nchar" or "stringfixed" or "stringfixedlength" => System.Data.DbType.StringFixedLength,
				"varchar" or "ansistring" => System.Data.DbType.AnsiString,
				"char" or "ansistringfixed" or "ansistringfixedlength" => System.Data.DbType.AnsiStringFixedLength,
				"short" or "int16" or "smallint" => System.Data.DbType.Int16,
				"int" or "int32" or "integer" => System.Data.DbType.Int32,
				"long" or "int64" or "bigint" => System.Data.DbType.Int64,
				"ushort" or "uint16" => System.Data.DbType.UInt16,
				"uint" or "uint32" => System.Data.DbType.UInt32,
				"ulong" or "uint64" => System.Data.DbType.UInt64,
				"byte" or "tiny" or "tinyint" => System.Data.DbType.Byte,
				"sbyte" => System.Data.DbType.SByte,
				"binary" or "byte[]" or "varbinary" => System.Data.DbType.Binary,
				"bool" or "boolean" => System.Data.DbType.Boolean,
				"money" or "currency" => System.Data.DbType.Currency,
				"decimal" => System.Data.DbType.Decimal,
				"double" => System.Data.DbType.Double,
				"float" or "single" => System.Data.DbType.Single,
				"date" => System.Data.DbType.Date,
				"time" => System.Data.DbType.Time,
				"datetime" or "timestamp" => System.Data.DbType.DateTime,
				"datetimeoffset" => System.Data.DbType.DateTimeOffset,
				"guid" or "uuid" => System.Data.DbType.Guid,
				"xml" => System.Data.DbType.Xml,
				"varnumeric" => System.Data.DbType.VarNumeric,
				"object" => System.Data.DbType.Object,
				_ => throw new DataException($"Invalid '{type}' type of the property or parameter."),
			};
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetFullName(string name, string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(name) || name.Contains('.'))
				return name;

			return $"{@namespace}.{name}";
		}

		private static T GetAttributeValue<T>(XmlReader reader, string name, T defaultValue = default)
		{
			string elementName = reader.NodeType == XmlNodeType.Element ? reader.Name : string.Empty;

			if(reader.MoveToAttribute(name))
			{
				//首先获取当前特性的文本值
				object attributeValue = reader.Value.Trim();

				//将读取器的指针移到当前特性所属的元素
				reader.MoveToElement();

				if(typeof(T) == typeof(string))
					return (T)attributeValue;

				//为指定名称的特性值做类型转换，如果转换失败则抛出异常
				if(!Zongsoft.Common.Convert.TryConvertValue<T>(attributeValue, out var result))
					throw new MetadataFileException(string.Format("Invalid value '{0}' of '{1}' attribute in '{2}' element.", attributeValue, name, elementName));

				return result;
			}

			//返回默认值
			return defaultValue;
		}

		private static T GetAttributeValue<T>(XmlReader reader, string name, Func<string, T> converter)
		{
			if(reader.MoveToAttribute(name))
			{
				//首先获取当前特性的文本值
				var attributeValue = reader.Value.Trim();

				//将读取器的指针移到当前特性所属的元素
				reader.MoveToElement();

				if(typeof(T) == typeof(string))
					return (T)(object)attributeValue;

				return converter(attributeValue);
			}

			//返回默认值
			return converter(null);
		}

		private void ProcessUnrecognizedElement(XmlReader reader, MetadataFile file, object container)
		{
			if(reader == null)
				throw new ArgumentNullException(nameof(reader));

			string elementName = null;

			if(reader.ReadState == ReadState.Initial && reader.Read())
				elementName = reader.Name;
			else if(reader.NodeType == XmlNodeType.Element)
				elementName = reader.Name;

			if(!this.OnUnrecognizedElement(reader, file, container))
			{
				var filePath = file != null ? file.FilePath : string.Empty;

				if(string.IsNullOrWhiteSpace(elementName))
					throw new MetadataFileException(string.Format("Contains unrecognized element(s) in the '{0}' file.", filePath));
				else
					throw new MetadataFileException(string.Format("Found a unrecognized '{0}' element in the '{1}' file.", elementName, filePath));
			}
		}

		private static NameTable CreateXmlNameTable()
		{
			var nameTable = new NameTable();

			nameTable.Add(XML_SCHEMA_ELEMENT);
			nameTable.Add(XML_CONTAINER_ELEMENT);
			nameTable.Add(XML_ENTITY_ELEMENT);
			nameTable.Add(XML_KEY_ELEMENT);
			nameTable.Add(XML_MEMBER_ELEMENT);
			nameTable.Add(XML_PROPERTY_ELEMENT);
			nameTable.Add(XML_COMPLEXPROPERTY_ELEMENT);
			nameTable.Add(XML_COMMAND_ELEMENT);
			nameTable.Add(XML_PARAMETER_ELEMENT);
			nameTable.Add(XML_SCRIPT_ELEMENT);
			nameTable.Add(XML_LINK_ELEMENT);
			nameTable.Add(XML_CONSTRAINTS_ELEMENT);
			nameTable.Add(XML_CONSTRAINT_ELEMENT);

			nameTable.Add(XML_NAME_ATTRIBUTE);
			nameTable.Add(XML_TYPE_ATTRIBUTE);
			nameTable.Add(XML_HINT_ATTRIBUTE);
			nameTable.Add(XML_ROLE_ATTRIBUTE);
			nameTable.Add(XML_ALIAS_ATTRIBUTE);
			nameTable.Add(XML_FIELD_ATTRIBUTE);
			nameTable.Add(XML_TABLE_ATTRIBUTE);
			nameTable.Add(XML_INHERITS_ATTRIBUTE);
			nameTable.Add(XML_LENGTH_ATTRIBUTE);
			nameTable.Add(XML_DEFAULT_ATTRIBUTE);
			nameTable.Add(XML_NULLABLE_ATTRIBUTE);
			nameTable.Add(XML_SORTABLE_ATTRIBUTE);
			nameTable.Add(XML_SEQUENCE_ATTRIBUTE);
			nameTable.Add(XML_PRECISION_ATTRIBUTE);
			nameTable.Add(XML_SCALE_ATTRIBUTE);
			nameTable.Add(XML_DIRECTION_ATTRIBUTE);
			nameTable.Add(XML_IMMUTABLE_ATTRIBUTE);
			nameTable.Add(XML_MULTIPLICITY_ATTRIBUTE);
			nameTable.Add(XML_PORT_ATTRIBUTE);
			nameTable.Add(XML_ANCHOR_ATTRIBUTE);
			nameTable.Add(XML_ACTOR_ATTRIBUTE);
			nameTable.Add(XML_VALUE_ATTRIBUTE);
			nameTable.Add(XML_PATH_ATTRIBUTE);
			nameTable.Add(XML_DRIVER_ATTRIBUTE);
			nameTable.Add(XML_BEHAVIORS_ATTRIBUTE);
			nameTable.Add(XML_MUTABILITY_ATTRIBUTE);

			return nameTable;
		}
		#endregion
	}
}
