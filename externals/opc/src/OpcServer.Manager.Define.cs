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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Server;

using Zongsoft.Common;
using Zongsoft.Reflection;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	partial class NodeManager
	{
		public BaseDataVariableState DefineVariable(string name, Type type, string label, string description = null) => this.DefineVariable(null, name, type, null, label, description);
		public BaseDataVariableState DefineVariable(string name, Type type, object value, string label, string description = null) => this.DefineVariable(null, name, type, value, label, description);
		public BaseDataVariableState DefineVariable(NodeState parent, string name, Type type, string label, string description = null) => this.DefineVariable(parent, name, type, null, label, description);
		public BaseDataVariableState DefineVariable(NodeState parent, string name, Type type, object value, string label, string description = null)
		{
			if(TypeExtension.IsNullable(type, out var underlyingType))
				type = underlyingType;

			var variable = this.CreateVariable(
				parent,
				name,
				value,
				label,
				Utility.GetBuiltinType(type, out var rank),
				rank,
				description);

			this.AddPredefinedNode(this.SystemContext, variable);
			return variable;
		}

		public BaseInstanceState DefineValue(object value, string name = null) => this.DefineValue(null, null, value, name);
		public BaseInstanceState DefineValue(NodeState parent, object value, string name = null) => this.DefineValue(parent, null, value, name);
		public BaseInstanceState DefineValue(NodeId identifier, object value, string name = null) => this.DefineValue(null, identifier, value, name);
		public BaseInstanceState DefineValue(NodeState parent, NodeId identifier, object value, string name = null)
		{
			if(value == null)
				return null;

			if(value is Type type)
				value = null;
			else
				type = value.GetType();

			type = Utility.DiscriminateType(type, out _);

			if(identifier == null || identifier.IsNullNodeId)
				identifier = this.GenerateId();

			if(string.IsNullOrEmpty(name))
				name = type.Name;

			var definition = this.DefineType(type, out var rank);

			if(definition is DataTypeState && definition.SuperTypeId == DataTypeIds.Enumeration)
			{
				var state = new DataItemState(parent)
				{
					NodeId = identifier,
					SymbolicName = name,
					BrowseName = new(name, this.NamespaceIndex),
					DisplayName = name,
					DataType = definition.NodeId,
					TypeDefinitionId = definition.NodeId,
					Value = value,
					ValueRank = rank,
				};

				return state;
			}

			BaseInstanceState instance = parent == null || parent is FolderState ?
				new BaseObjectState(parent)
				{
					NodeId = identifier,
					SymbolicName = name,
					BrowseName = new(name, this.NamespaceIndex),
					DisplayName = name,
					TypeDefinitionId = definition.NodeId,
					ReferenceTypeId = ReferenceTypeIds.HasComponent,
				} :
				new PropertyState(parent)
				{
					NodeId = identifier,
					SymbolicName = name,
					BrowseName = new(name, this.NamespaceIndex),
					DisplayName = name,
					DataType = definition.NodeId,
					TypeDefinitionId = VariableTypeIds.PropertyType,
					ReferenceTypeId = ReferenceTypeIds.HasProperty,
					ValueRank = rank,
					Value = value,
				};

			var children = new List<BaseInstanceState>();
			definition.GetChildren(this.SystemContext, children);

			if((children == null || children.Count == 0) && definition.SuperTypeId != DataTypeIds.Structure)
				return instance;

			//if(type.IsPrimitive || type.IsEnum || type == typeof(string) || type.IsScalarType())
			//{
			//	if(instance is BaseVariableState state)
			//		state.Value = value;
			//	return instance;
			//}

			foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				if(field.IsInitOnly)
					continue;

				SetMember(value, instance, new(field));
			}

			foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				if(!property.CanRead || property.IsIndexer())
					continue;

				SetMember(value, instance, new(property));
			}

			return instance;

			void SetMember(object target, BaseInstanceState instance, MemberToken member)
			{
				var value = target == null ? member.Type : Reflector.GetValue(ref target, member.Name);
				var state = this.DefineValue(instance, value ?? member.Type, member.Name);

				if(state is BaseVariableState variable)
				{
					variable.AccessLevel = variable.UserAccessLevel =
						member.IsReadOnly ? AccessLevels.CurrentRead : AccessLevels.CurrentReadOrWrite;
				}

				instance.AddChild(state);
			}
		}

		public BaseTypeState DefineType(Type type) => this.DefineType(type, out _);
		public BaseTypeState DefineType(Type type, out int rank)
		{
			if(type == null)
			{
				rank = 0;
				return null;
			}

			//鉴别并拆解指定的类型
			type = Utility.DiscriminateType(type, out rank);

			//获取指定类型对应的内置类型
			var nodeId = Utility.GetBuiltinTypeCore(type);

			//如果指定类型是内置类型，则获取并返回该内置类型的节点定义
			if(nodeId != null && this.Server.NodeManager.GetManagerHandle(nodeId, out _) is NodeHandle handle && handle.Node is BaseTypeState result)
				return result;

			lock(this.Lock)
			{
				if(_types.TryGetValue(type, out nodeId))
					return this.FindPredefinedNode(nodeId, null) as BaseTypeState;

				if(type.IsValueType)
					return type.IsEnum ? this.DefineEnumeration(type) : this.DefineDataType(type);

				return this.DefineObjectType(type);
			}
		}

		private DataTypeState DefineEnumeration(Type type)
		{
			EnumDefinition enumeration = new EnumDefinition();
			var entries = EnumUtility.GetEnumEntries(type, true);
			var enumNames = new LocalizedText[entries.Length];
			var enumValues = new EnumValueType[entries.Length];

			for(int i = 0; i < entries.Length; i++)
			{
				var value = (long)System.Convert.ChangeType(entries[i].Value, typeof(long));

				enumeration.Fields.Add(new()
				{
					Name = entries[i].Name,
					DisplayName = entries[i].Name,
					Description = entries[i].Description,
					Value = value,
				});

				enumNames[i] = new(entries[i].Name);
				enumValues[i] = new()
				{
					Value = value,
					DisplayName = entries[i].Name,
					Description = entries[i].Description,
				};
			}

			var definition = new DataTypeState()
			{
				NodeId = this.GenerateId(),
				SymbolicName = $"{type.Name}DataType",
				BrowseName = new QualifiedName($"{type.Namespace}.{type.Name}", this.NamespaceIndex),
				DisplayName = $"{type.Namespace}.{type.Name}",
				SuperTypeId = DataTypeIds.Enumeration,
				DataTypeDefinition = new(enumeration),
			};

			var enumStringsProperty = new PropertyState(definition)
			{
				NodeId = this.GenerateId(),
				BrowseName = new QualifiedName("EnumStrings", 0),
				DisplayName = "EnumStrings",
				DataType = DataTypeIds.LocalizedText,
				ValueRank = ValueRanks.OneDimension,
				ArrayDimensions = new ReadOnlyList<uint>([(uint)enumNames.Length]),
				TypeDefinitionId = VariableTypeIds.PropertyType,
				AccessLevel = AccessLevels.CurrentRead,
				UserAccessLevel = AccessLevels.CurrentRead,
				Value = enumNames,
				MinimumSamplingInterval = 0,
				Historizing = false
			};

			definition.AddChild(enumStringsProperty);
			definition.AddReference(ReferenceTypes.HasProperty, false, enumStringsProperty.NodeId);
			enumStringsProperty.AddReference(ReferenceTypes.HasProperty, true, definition.NodeId);

			var enumValuesProperty = new PropertyState(definition)
			{
				NodeId = this.GenerateId(),
				BrowseName = new QualifiedName("EnumValues", 0),
				DisplayName = "EnumValues",
				DataType = DataTypeIds.EnumValueType,
				ValueRank = ValueRanks.OneDimension,
				ArrayDimensions = new ReadOnlyList<uint>([(uint)enumValues.Length]),
				TypeDefinitionId = VariableTypeIds.PropertyType,
				AccessLevel = AccessLevels.CurrentRead,
				UserAccessLevel = AccessLevels.CurrentRead,
				Value = enumValues,
				MinimumSamplingInterval = 0,
				Historizing = false
			};

			definition.AddChild(enumValuesProperty);
			definition.AddReference(ReferenceTypes.HasProperty, false, enumValuesProperty.NodeId);
			enumValuesProperty.AddReference(ReferenceTypes.HasProperty, true, definition.NodeId);

			if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
				this.AddPredefinedNode(this.SystemContext, definition);

			_types.TryAdd(type, definition.NodeId);
			return definition;
		}

		private DataTypeState DefineDataType(Type type)
		{
			var structure = new StructureDefinition()
			{
				BaseDataType = DataTypeIds.Structure,
				StructureType = StructureType.Structure,
			};

			foreach(var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				switch(member)
				{
					case FieldInfo field:
						if(field.IsInitOnly)
							break;

						structure.Fields.Add(new()
						{
							Name = field.Name,
							DataType = Utility.GetBuiltinType(field.FieldType, out var fieldRank),
							ValueRank = fieldRank,
						});
						break;
					case PropertyInfo property:
						structure.Fields.Add(new()
						{
							Name = property.Name,
							DataType = Utility.GetBuiltinType(property.PropertyType, out var propertyRank),
							ValueRank = propertyRank,
						});
						break;
				}
			}

			var definition = new DataTypeState()
			{
				NodeId = this.GenerateId(),
				SymbolicName = $"{type.Name}DataType",
				BrowseName = new QualifiedName($"{type.Namespace}.{type.Name}", this.NamespaceIndex),
				DisplayName = $"{type.Namespace}.{type.Name}",
				SuperTypeId = DataTypeIds.Structure,
				DataTypeDefinition = new(structure),
			};

			if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
				this.AddPredefinedNode(this.SystemContext, definition);

			_types.TryAdd(type, definition.NodeId);
			return definition;
		}

		private BaseObjectTypeState DefineObjectType(Type type)
		{
			var definition = new BaseObjectTypeState()
			{
				NodeId = this.GenerateId(),
				IsAbstract = type.IsAbstract,
				SymbolicName = $"{type.Name}Type",
				BrowseName = new QualifiedName($"{type.Namespace}.{type.Name}", this.NamespaceIndex),
				DisplayName = $"{type.Namespace}.{type.Name}",
				SuperTypeId = ObjectTypeIds.BaseObjectType,
			};

			foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				if(!field.IsInitOnly)
					this.DefineProperty(definition, new(field));
			}

			foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				if(property.CanRead)
					this.DefineProperty(definition, new(property));
			}

			if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
				this.AddPredefinedNode(this.SystemContext, definition);

			_types.TryAdd(type, definition.NodeId);
			return definition;
		}

		private PropertyState DefineProperty(NodeState parent, MemberToken member)
		{
			var state = new PropertyState(parent)
			{
				NodeId = this.GenerateId(),
				ReferenceTypeId = ReferenceTypes.HasProperty,
				TypeDefinitionId = VariableTypeIds.PropertyType,
				SymbolicName = member.Name,
				BrowseName = new(member.Name, this.NamespaceIndex),
				DisplayName = member.Name,
				DataType = this.DefineType(member.Type, out var rank).NodeId,
				ValueRank = rank,
				AccessLevel = member.IsReadOnly ? AccessLevels.CurrentRead: AccessLevels.CurrentReadOrWrite,
				UserAccessLevel = member.IsReadOnly ? AccessLevels.CurrentRead : AccessLevels.CurrentReadOrWrite,
				MinimumSamplingInterval = MinimumSamplingIntervals.Indeterminate,
			};

			state.AddReference(ReferenceTypes.HasModellingRule, false, ObjectIds.ModellingRule_Mandatory);
			parent.AddChild(state);
			return state;
		}

		private FolderState CreateFolder(NodeState parent, ExpandedNodeId id, string name, string displayName, string description)
		{
			var nodeId = id == null || id.IsNull ? this.GenerateId() : (NodeId)id;

			var folder = new FolderState(parent)
			{
				SymbolicName = name,
				ReferenceTypeId = ReferenceTypes.Organizes,
				TypeDefinitionId = ObjectTypeIds.FolderType,
				NodeId = nodeId,
				BrowseName = new QualifiedName(name, this.NamespaceIndex),
				DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName,
				Description = description,
				WriteMask = AttributeWriteMask.None,
				UserWriteMask = AttributeWriteMask.None,
				EventNotifier = EventNotifiers.None
			};

			if(parent != null)
				parent.AddChild(folder);

			return folder;
		}

		private BaseDataVariableState CreateVariable(NodeState parent, string name, object value, string displayName, NodeId dataType, int valueRank, string description = null)
		{
			var nodeId = this.GenerateId(name, out name);

			if(string.IsNullOrEmpty(displayName))
				displayName = name;

			var variable = new BaseDataVariableState(parent)
			{
				SymbolicName = name,
				ReferenceTypeId = ReferenceTypes.Organizes,
				TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
				NodeId = nodeId,
				BrowseName = new QualifiedName(name, this.NamespaceIndex),
				DisplayName = displayName,
				Description = description,
				WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
				UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
				DataType = dataType,
				ValueRank = valueRank,
				AccessLevel = AccessLevels.CurrentReadOrWrite,
				UserAccessLevel = AccessLevels.CurrentReadOrWrite,
				Historizing = false,
				Value = value,
				StatusCode = StatusCodes.Good,
				Timestamp = DateTime.Now,
			};

			if(valueRank == ValueRanks.OneDimension)
			{
				variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
			}
			else if(valueRank == ValueRanks.TwoDimensions)
			{
				variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
			}

			parent?.AddChild(variable);
			return variable;
		}

		private readonly struct MemberToken
		{
			public MemberToken(FieldInfo field)
			{
				this.Name = field.Name;
				this.Type = field.FieldType;
				this.IsReadOnly = false;
			}
			public MemberToken(PropertyInfo property)
			{
				this.Name = property.Name;
				this.Type = property.PropertyType;
				this.IsReadOnly = !property.CanWrite;
			}
			public MemberToken(string name, Type type, bool isReadOnly)
			{
				this.Name = name;
				this.Type = type;
				this.IsReadOnly = isReadOnly;
			}

			public readonly string Name;
			public readonly Type Type;
			public readonly bool IsReadOnly;

			public override string ToString() => $"{this.Name}:{this.Type.FullName}({(this.IsReadOnly ? "ReadOnly" : "Read|Write")})";
		}
	}
}
