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
		private const string XML_PARAMETER_ELEMENT = "parameter";
		private const string XML_TEXT_ELEMENT = "text";
		private const string XML_LINK_ELEMENT = "link";
		private const string XML_CONSTRAINT_ELEMENT = "constraint";
		private const string XML_CONSTRAINTS_ELEMENT = "constraints";

		private const string XML_NAME_ATTRIBUTE = "name";
		private const string XML_TYPE_ATTRIBUTE = "type";
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
		private const string XML_ACTOR_ATTRIBUTE = "actor";
		private const string XML_VALUE_ATTRIBUTE = "value";
		private const string XML_TEXT_ATTRIBUTE = "text";
		#endregion

		#region 构造函数
		protected MetadataFileResolver()
		{
		}
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

		public MetadataFile Resolve(XmlReader reader, string name)
		{
			return this.Resolve(reader, null, name);
		}

		public MetadataFile Resolve(XmlReader reader, string filePath, string name)
		{
			if(reader == null)
				throw new ArgumentNullException(nameof(reader));

			if(string.IsNullOrWhiteSpace(filePath))
				filePath = reader.BaseURI;

			if(reader.MoveToContent() == XmlNodeType.Element)
			{
				if(reader.LocalName != XML_SCHEMA_ELEMENT)
					throw new MetadataFileException(string.Format("The root element must be '<{0}>' in this '{1}' file.", XML_SCHEMA_ELEMENT, filePath));
			}

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
							var entity = this.ResolveEntity(reader, file, @namespace, () => this.ProcessUnrecognizedElement(reader, file, @namespace));

							if(entity != null)
								file.Entities.Add(entity);

							break;
						case XML_COMMAND_ELEMENT:
							var command = this.ResolveCommand(reader, file, @namespace, () => this.ProcessUnrecognizedElement(reader, file, @namespace));

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
		private IDataEntity ResolveEntity(XmlReader reader, MetadataFile provider, string @namespace, Action unrecognize)
		{
			//创建实体元素对象
			var entity = new MetadataEntity(provider,
				this.GetFullName(reader.GetAttribute(XML_NAME_ATTRIBUTE), @namespace),
				this.GetFullName(reader.GetAttribute(XML_INHERITS_ATTRIBUTE), @namespace),
				this.GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false));

			//设置其他属性
			entity.Alias = reader.GetAttribute(XML_ALIAS_ATTRIBUTE) ?? reader.GetAttribute(XML_TABLE_ATTRIBUTE);

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
						                   this.GetFieldType(this.GetAttributeValue<string>(reader, XML_TYPE_ATTRIBUTE)),
						                   this.GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false))
						{
							Alias = this.GetAttributeValue<string>(reader, XML_ALIAS_ATTRIBUTE) ?? this.GetAttributeValue<string>(reader, XML_FIELD_ATTRIBUTE),
							Length = this.GetAttributeValue<int>(reader, XML_LENGTH_ATTRIBUTE),
							Precision = this.GetAttributeValue<byte>(reader, XML_PRECISION_ATTRIBUTE),
							Scale = this.GetAttributeValue<byte>(reader, XML_SCALE_ATTRIBUTE),
							Nullable = this.GetAttributeValue<bool>(reader, XML_NULLABLE_ATTRIBUTE, true),
							Sortable = this.GetAttributeValue<bool>(reader, XML_SORTABLE_ATTRIBUTE, false),
						};

						//设置默认值的字面量
						property.SetDefaultValue(this.GetAttributeValue<string>(reader, XML_DEFAULT_ATTRIBUTE));

						//设置序号器元数据信息
						property.SetSequence(this.GetAttributeValue<string>(reader, XML_SEQUENCE_ATTRIBUTE));

						//将解析成功的属性元素加入到实体的属性集合
						entity.Properties.Add(property);

						break;
					case XML_COMPLEXPROPERTY_ELEMENT:
						var complexProperty = new MetadataEntityComplexProperty(entity,
						                          reader.GetAttribute(XML_NAME_ATTRIBUTE),
						                          this.GetRoleName(reader.GetAttribute(XML_ROLE_ATTRIBUTE), @namespace),
						                          this.GetAttributeValue(reader, XML_IMMUTABLE_ATTRIBUTE, false));

						var multiplicity = reader.GetAttribute(XML_MULTIPLICITY_ATTRIBUTE);

						if(multiplicity != null && multiplicity.Length > 0)
						{
							switch(multiplicity)
							{
								case "*":
									complexProperty.Multiplicity = DataAssociationMultiplicity.Many;
									break;
								case "1":
								case "!":
									complexProperty.Multiplicity = DataAssociationMultiplicity.One;
									break;
								case "?":
								case "0..1":
									complexProperty.Multiplicity = DataAssociationMultiplicity.ZeroOrOne;
									break;
								default:
									throw new DataException($"Invalid '{multiplicity}' value of the multiplicity attribute.");
							}
						}

						var links = new List<DataAssociationLink>();

						while(reader.Read() && reader.Depth > depth + 1)
						{
							if(reader.NodeType != XmlNodeType.Element)
								continue;

							if(reader.LocalName == XML_LINK_ELEMENT)
							{
								links.Add(
									new DataAssociationLink(complexProperty,
										this.GetAttributeValue<string>(reader, XML_NAME_ATTRIBUTE),
										this.GetAttributeValue<string>(reader, XML_ROLE_ATTRIBUTE)));
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
												this.GetAttributeValue<string>(reader, XML_NAME_ATTRIBUTE),
												this.GetAttributeValue(reader, XML_ACTOR_ATTRIBUTE,
													complexProperty.Multiplicity == DataAssociationMultiplicity.Many ?
													DataAssociationConstraintActor.Foreign :
													DataAssociationConstraintActor.Principal),
												this.GetAttributeValue<object>(reader, XML_VALUE_ATTRIBUTE)));
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

			//确认实体的主键信息
			if(keys.Count > 0)
			{
				var array = new IDataEntitySimplexProperty[keys.Count];
				var index = 0;

				foreach(var key in keys)
				{
					if(!entity.Properties.TryGet(key, out var property))
						throw new MetadataFileException($"The '{key}' primary key in the '{entity.Name}' entity is undefined.");
					if(property.IsComplex)
						throw new MetadataFileException($"The '{key}' primary key in the '{entity.Name}' entity cannot be a complex(navigation) property.");

					//将主键属性的是否主键开关打开
					((MetadataEntitySimplexProperty)property).SetPrimaryKey();

					array[index++] = (IDataEntitySimplexProperty)property;
				}

				entity.Key = array;
			}

			return entity;
		}

		private IDataCommand ResolveCommand(XmlReader reader, MetadataFile provider, string @namespace, Action unrecognize)
		{
			//创建命令元素对象
			var command = new MetadataCommand(provider,
				this.GetFullName(reader.GetAttribute(XML_NAME_ATTRIBUTE), @namespace),
				reader.GetAttribute(XML_ALIAS_ATTRIBUTE))
			{
				Type = this.GetAttributeValue(reader, XML_TYPE_ATTRIBUTE, DataCommandType.Procedure),
				Text = this.GetAttributeValue<string>(reader, XML_TEXT_ATTRIBUTE),
			};

			int depth = reader.Depth;

			while(reader.Read() && reader.Depth > depth)
			{
				if(reader.NodeType != XmlNodeType.Element)
					continue;

				switch(reader.LocalName)
				{
					case XML_PARAMETER_ELEMENT:
						var parameter = new MetadataCommandParameter(command, reader.GetAttribute(XML_NAME_ATTRIBUTE), this.GetAttributeValue<Type>(reader, XML_TYPE_ATTRIBUTE))
						{
							Direction = this.GetAttributeValue(reader, XML_DIRECTION_ATTRIBUTE, System.Data.ParameterDirection.Input),
							Alias = this.GetAttributeValue<string>(reader, XML_ALIAS_ATTRIBUTE),
							Length = this.GetAttributeValue<int>(reader, XML_LENGTH_ATTRIBUTE),
							Value = this.GetAttributeValue<object>(reader, XML_VALUE_ATTRIBUTE),
						};

						//将解析成功的命令参数元素加入到命令的参数集合
						command.Parameters.Add(parameter);

						break;
					case XML_TEXT_ELEMENT:
						if(reader.NodeType == XmlNodeType.CDATA || reader.NodeType == XmlNodeType.Text)
							command.Text = reader.Value;

						break;
					default:
						unrecognize();
						break;
				}
			}

			return command;
		}
		#endregion

		#region 虚拟方法
		protected virtual XmlReader CreateReader(Func<XmlReaderSettings, XmlParserContext, XmlReader> createThunk)
		{
			var nameTable = this.CreateXmlNameTable();

			var settings = new XmlReaderSettings()
			{
				CloseInput = false,
				IgnoreComments = true,
				IgnoreWhitespace = true,
				NameTable = nameTable,
				ValidationType = ValidationType.None,
			};

			/* 如果需要启用数据映射文件的XMLSchema验证则打开下面代码 */
			//settings.Schemas = new System.Xml.Schema.XmlSchemaSet(nameTable);
			//settings.Schemas.Add(XML_NAMESPACE_URI, @"/Zongsoft/Zongsoft.Data/Zongsoft.Data.xsd");
			//settings.ValidationType = ValidationType.Schema;
			//settings.ValidationEventHandler += XmlSchema_ValidationEventHandler;

			var namespaceManager = new XmlNamespaceManager(nameTable);
			namespaceManager.AddNamespace(string.Empty, XML_NAMESPACE_URI);

			return createThunk(settings, new XmlParserContext(nameTable, namespaceManager, XML_SCHEMA_ELEMENT, XmlSpace.None));
		}

		protected virtual bool OnUnrecognizedElement(XmlReader reader, MetadataFile file, object container)
		{
			return false;
		}
		#endregion

		#region 事件处理
		private void XmlSchema_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
		{
			throw new MetadataFileException(e.Message, e.Exception);
		}
		#endregion

		#region 私有方法
		private System.Data.DbType GetFieldType(string type)
		{
			if(string.IsNullOrWhiteSpace(type))
				return System.Data.DbType.String;

			switch(type.ToLowerInvariant())
			{
				case "string":
				case "nvarchar":
					return System.Data.DbType.String;
				case "nchar":
				case "stringfixed":
				case "stringfixedlength":
					return System.Data.DbType.StringFixedLength;
				case "varchar":
				case "ansistring":
					return System.Data.DbType.AnsiString;
				case "char":
				case "ansistringfixed":
				case "ansistringfixedlength":
					return System.Data.DbType.AnsiStringFixedLength;
				case "short":
				case "int16":
				case "smallint":
					return System.Data.DbType.Int16;
				case "int":
				case "int32":
				case "integer":
					return System.Data.DbType.Int32;
				case "long":
				case "int64":
				case "bigint":
					return System.Data.DbType.Int64;
				case "ushort":
				case "uint16":
					return System.Data.DbType.UInt16;
				case "uint":
				case "uint32":
					return System.Data.DbType.UInt32;
				case "ulong":
				case "uint64":
					return System.Data.DbType.UInt64;
				case "byte":
				case "tiny":
				case "tinyint":
					return System.Data.DbType.Byte;
				case "sbyte":
					return System.Data.DbType.SByte;
				case "binary":
				case "byte[]":
				case "varbinary":
					return System.Data.DbType.Binary;
				case "bool":
				case "boolean":
					return System.Data.DbType.Boolean;
				case "money":
				case "currency":
					return System.Data.DbType.Currency;
				case "decimal":
					return System.Data.DbType.Decimal;
				case "double":
					return System.Data.DbType.Double;
				case "float":
				case "single":
					return System.Data.DbType.Single;
				case "date":
					return System.Data.DbType.Date;
				case "time":
					return System.Data.DbType.Time;
				case "datetime":
				case "timestamp":
					return System.Data.DbType.DateTime;
				case "datetimeoffset":
					return System.Data.DbType.DateTimeOffset;
				case "guid":
				case "uuid":
					return System.Data.DbType.Guid;
				case "xml":
					return System.Data.DbType.Xml;
				case "varnumeric":
					return System.Data.DbType.VarNumeric;
				case "object":
					return System.Data.DbType.Object;
			}

			throw new DataException($"Invalid '{type}' type of the property.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetFullName(string name, string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(name) || name.Contains("."))
				return name;

			return @namespace + "." + name;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetRoleName(string role, string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(role))
				return role;

			var colonIndex = -1;

			for(int i = 0; i < role.Length; i++)
			{
				if(role[i] == '.' && i < colonIndex)
					return role;
			}

			return @namespace + "." + role;
		}

		private T GetAttributeValue<T>(XmlReader reader, string name, T defaultValue = default(T))
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

				T result;

				//为指定名称的特性值做类型转换，如果转换失败则抛出异常
				if(!Zongsoft.Common.Convert.TryConvertValue<T>(attributeValue, out result))
					throw new MetadataFileException(string.Format("Invalid value '{0}' of '{1}' attribute in '{2}' element.", attributeValue, name, elementName));

				return result;
			}

			//返回默认值
			return defaultValue;
		}

		private void ProcessUnrecognizedElement(XmlReader reader, MetadataFile file, object container)
		{
			if(reader == null)
				throw new ArgumentNullException("reader");

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

				throw new MetadataFileException(string.Format("Found a unrecognized '{0}' element in the '{1}' file.", elementName, filePath));
			}
		}

		private XmlNameTable CreateXmlNameTable()
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
			nameTable.Add(XML_TEXT_ELEMENT);
			nameTable.Add(XML_LINK_ELEMENT);
			nameTable.Add(XML_CONSTRAINTS_ELEMENT);
			nameTable.Add(XML_CONSTRAINT_ELEMENT);

			nameTable.Add(XML_NAME_ATTRIBUTE);
			nameTable.Add(XML_TYPE_ATTRIBUTE);
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
			nameTable.Add(XML_ACTOR_ATTRIBUTE);
			nameTable.Add(XML_VALUE_ATTRIBUTE);
			nameTable.Add(XML_TEXT_ATTRIBUTE);

			return nameTable;
		}
		#endregion
	}
}
