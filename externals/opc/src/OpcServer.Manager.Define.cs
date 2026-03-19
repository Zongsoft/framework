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
				Utility.GetDataType(type, out var rank),
				rank,
				description);

			this.AddPredefinedNode(this.SystemContext, variable);
			return variable;
		}

		public BaseInstanceState DefineObject(object instance, string name = null) => this.DefineObject(null, null, instance, name);
		public BaseInstanceState DefineObject(NodeState parent, object instance, string name = null) => this.DefineObject(parent, null, instance, name);
		public BaseInstanceState DefineObject(NodeId identifier, object instance, string name = null) => this.DefineObject(null, identifier, instance, name);
		public BaseInstanceState DefineObject(NodeState parent, NodeId identifier, object instance, string name = null)
		{
			if(instance == null)
				return null;

			if(string.IsNullOrEmpty(name))
				name = instance.GetType().Name;

			var typeDefinition = this.DefineType(instance.GetType());

			if(instance.GetType().IsEnum)
			{
				var state = new DataItemState(parent)
				{
					NodeId = identifier,
					SymbolicName = name,
					BrowseName = new(name, this.NamespaceIndex),
					DisplayName = name,
					DataType = typeDefinition.NodeId,
					TypeDefinitionId = typeDefinition.NodeId,
					Value = instance,
					ValueRank = ValueRanks.Scalar,
				};

				return state;
			}

			var instanceDefinition = new BaseObjectState(parent)
			{
				NodeId = identifier,
				SymbolicName = name,
				BrowseName = new(name, this.NamespaceIndex),
				DisplayName = name,
				TypeDefinitionId = typeDefinition.NodeId,
				ReferenceTypeId = ReferenceTypeIds.HasComponent,
			};

			this.GenerateProperties(instanceDefinition, instance);
			return instanceDefinition;
		}

		public BaseTypeState DefineType(Type type)
		{
			if(type == null)
				return null;

			var elementType = TypeExtension.GetElementType(type);
			type = TypeExtension.IsNullable(elementType ?? type, out var underlyingType) ? underlyingType : (elementType ?? type);

			lock(this.Lock)
			{
				if(_types.TryGetValue(type, out var identifer))
					return this.FindPredefinedNode(identifer, null) as BaseTypeState;

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
							DataType = Utility.GetDataType(field.FieldType, out var fieldRank),
							ValueRank = fieldRank,
						});
						break;
					case PropertyInfo property:
						structure.Fields.Add(new()
						{
							Name = property.Name,
							DataType = Utility.GetDataType(property.PropertyType, out var propertyRank),
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

			foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
			{
				this.DefineProperty(definition, property);
			}

			if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
				this.AddPredefinedNode(this.SystemContext, definition);

			_types.TryAdd(type, definition.NodeId);
			return definition;
		}

		private PropertyState DefineProperty(NodeState parent, PropertyInfo property)
		{
			var propertyType = TypeExtension.IsNullable(property.PropertyType, out var underlyingType) ? underlyingType : property.PropertyType;
			var elementType = TypeExtension.GetCollectionElementType(propertyType);

			var propertyState = new PropertyState(parent)
			{
				NodeId = this.GenerateId(),
				ReferenceTypeId = ReferenceTypes.HasProperty,
				TypeDefinitionId = VariableTypeIds.PropertyType,
				SymbolicName = property.Name,
				BrowseName = new(property.Name, this.NamespaceIndex),
				DisplayName = property.Name,
				DataType = Utility.GetDataType(elementType ?? propertyType, out var rank),
				ValueRank = rank,
				AccessLevel = property.CanWrite ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead,
				UserAccessLevel = property.CanWrite ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead,
				MinimumSamplingInterval = MinimumSamplingIntervals.Indeterminate,
			};

			if(propertyState.DataType == DataTypeIds.Enumeration)
			{
				var typeState = this.DefineType(elementType ?? propertyType);
				propertyState.DataType = typeState.NodeId;
				propertyState.TypeDefinitionId = typeState.NodeId;
			}
			else if(propertyState.DataType == DataTypeIds.ObjectTypeNode)
			{
				var typeState = this.DefineType(elementType ?? propertyType);
				propertyState.DataType = typeState.NodeId;
				propertyState.TypeDefinitionId = typeState.NodeId;
			}

			propertyState.AddReference(ReferenceTypes.HasModellingRule, false, ObjectIds.ModellingRule_Mandatory);
			parent.AddChild(propertyState);
			return propertyState;
		}

		private void GenerateProperties(BaseObjectState instanceDefinition, object instance)
		{
			var typeDefinition = this.FindPredefinedNode(instanceDefinition.TypeDefinitionId, typeof(BaseTypeState));
			if(typeDefinition == null)
				return;

			if(typeDefinition is DataTypeState dataType)
			{
				if(dataType.DataTypeDefinition.Body is StructureDefinition structure)
				{
					foreach(var field in structure.Fields)
					{
						var propertyState = new PropertyState(instanceDefinition)
						{
							BrowseName = field.Name,
							DisplayName = field.Name,
							Description = field.Description,
							SymbolicName = field.Name,
							TypeDefinitionId = (NodeId)field.TypeId,
							ReferenceTypeId = ReferenceTypeIds.HasProperty,
							DataType = field.DataType,
							ValueRank = field.ValueRank,
						};

						if(Reflection.Reflector.TryGetValue(ref instance, field.Name, out var value))
							propertyState.Value = value;

						instanceDefinition.AddChild(propertyState);
					}
				}
				else if(dataType.DataTypeDefinition.Body is EnumDefinition enumeration)
				{
				}

				return;
			}

			var children = new List<BaseInstanceState>();
			typeDefinition.GetChildren(this.SystemContext, children);

			foreach(var child in children)
			{
				var propertyState = new PropertyState(instanceDefinition)
				{
					BrowseName = child.BrowseName,
					DisplayName = child.DisplayName,
					Description = child.Description,
					SymbolicName = child.SymbolicName,
					TypeDefinitionId = child.NodeId,
					ModellingRuleId = child.ModellingRuleId,
					ReferenceTypeId = ReferenceTypeIds.HasProperty,
					WriteMask = child.WriteMask,
					UserWriteMask = child.UserWriteMask,
				};

				if(child is BaseVariableState state)
				{
					propertyState.DataType = state.DataType;
					propertyState.ValueRank = state.ValueRank;
					propertyState.Value = state.Value;
					propertyState.AccessLevel = state.AccessLevel;
					propertyState.UserAccessLevel = state.UserAccessLevel;
					propertyState.Historizing = state.Historizing;
					propertyState.ArrayDimensions = state.ArrayDimensions;
					propertyState.IsValueType = state.IsValueType;
					propertyState.MinimumSamplingInterval = state.MinimumSamplingInterval;

					if(Utility.GetDataType(propertyState.DataType, propertyState.ValueRank) == typeof(object))
					{
						var propertyType = this.FindPredefinedNode(propertyState.DataType, typeof(BaseTypeState));

						if(Reflection.Reflector.TryGetValue(ref instance, child.SymbolicName, out var value))
						{
							var childInstance = this.DefineObject(instanceDefinition, value);

							if(childInstance != null)
							{
								var list = new List<BaseInstanceState>();
								childInstance.GetChildren(this.SystemContext, list);

								foreach(var item in list)
									propertyState.AddChild(item);
							}

							//propertyState.Value = value;
						}
					}
					else if(Reflection.Reflector.TryGetValue(ref instance, child.SymbolicName, out var value))
						propertyState.Value = value;
				}

				instanceDefinition.AddChild(propertyState);
			}
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
	}
}
