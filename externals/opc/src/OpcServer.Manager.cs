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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Server;

using Zongsoft.Common;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	internal class NodeManager : CustomNodeManager2
	{
		#region 私有字段
		private volatile uint _nodeId;
		private IList<IReference> _objectReferences;
		private IList<IReference> _serverReferences;
		private Dictionary<Type, NodeId> _types = new();
		#endregion

		#region 构造函数
		public NodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, ["http://zongsoft.com/opc/ua", "http://zongsoft.com/opc-ua"])
		{
			this.SystemContext.NodeIdFactory = this;
		}
		#endregion

		#region 公共方法
		public void AddNodes(OperationContext context, AddNodesItemCollection nodes, out AddNodesResultCollection results, out DiagnosticInfoCollection diagnostics)
		{
			if(nodes == null)
				throw new ServiceResultException(StatusCodes.BadInvalidArgument);

			results = new AddNodesResultCollection(nodes.Count);
			diagnostics = new DiagnosticInfoCollection(nodes.Count);

			for(int i = 0; i < nodes.Count; i++)
			{
				(var result, var diagnostic) = this.AddNode(context, nodes[i]);

				if(result != null)
					results.Add(result);
				if(diagnostic != null)
					diagnostics.Add(diagnostic);
			}
		}

		private (AddNodesResult result, DiagnosticInfo diagnostic) AddNode(OperationContext context, AddNodesItem node)
		{
			var parent = node.ParentNodeId == null || node.ParentNodeId.IsNull ? null : this.FindPredefinedNode((NodeId)node.ParentNodeId, null);

			if(parent == null)
			{
				var index = node.BrowseName.Name.LastIndexOf('/');
				if(index > 0 && index < node.BrowseName.Name.Length - 1)
				{
					parent = this.Find(node.BrowseName.Name[..index]);
					node.BrowseName = new QualifiedName(node.BrowseName.Name[(index + 1)..], this.NamespaceIndex);
				}

				parent ??= this.FindNodeInAddressSpace(Objects.ObjectsFolder);
			}

			if(node.NodeClass == NodeClass.Object)
			{
				if(node.TypeDefinition == ObjectTypeIds.FolderType)
				{
					var displayName = node.BrowseName.Name;
					var description = (string)null;

					if(node.NodeAttributes?.Body is ObjectAttributes attributes)
					{
						displayName = attributes.DisplayName?.Text;
						description = attributes.Description?.Text;
					}

					var folder = this.CreateFolder(parent, null, node.BrowseName.Name, displayName, description);
					this.AddPredefinedNode(this.SystemContext, folder);

					return (new AddNodesResult()
					{
						StatusCode = StatusCodes.Good,
						AddedNodeId = folder.NodeId,
					}, null);
				}
				else
					throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid);
			}
			else if(node.NodeClass == NodeClass.Variable)
			{
				if(node.NodeAttributes?.Body is VariableAttributes attributes)
				{
					var variable = this.CreateVariable(parent, node.BrowseName.Name, attributes.Value, attributes.DisplayName.Text, attributes.DataType, attributes.ValueRank);
					variable.Historizing = attributes.Historizing;
					variable.AccessLevel = attributes.AccessLevel;
					variable.UserAccessLevel = attributes.UserAccessLevel;
					variable.WriteMask = (AttributeWriteMask)attributes.WriteMask;
					variable.UserWriteMask = (AttributeWriteMask)attributes.UserWriteMask;
					variable.ArrayDimensions = attributes.ArrayDimensions?.ToArray();
					variable.Description = attributes.Description;

					this.AddPredefinedNode(this.SystemContext, variable);

					if(parent != null)
						parent.ClearChangeMasks(this.SystemContext, true);

					return (new AddNodesResult()
					{
						StatusCode = StatusCodes.Good,
						AddedNodeId = variable.NodeId,
					}, null);
				}
				else
					throw new ServiceResultException(StatusCodes.BadNodeAttributesInvalid);
			}

			throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
		}

		public NodeState Find(string path)
		{
			foreach(var node in this.PredefinedNodes)
			{
				if(node.Value is BaseInstanceState state)
				{
					var found = string.Equals(state.SymbolicName, path, StringComparison.OrdinalIgnoreCase) ? state : state.FindChildBySymbolicName(this.SystemContext, path);

					if(found != null)
						return found;
				}
			}

			return null;
		}

		public bool Write<T>(NodeId nodeId, T value)
		{
			var item = new WriteValue()
			{
				NodeId = nodeId,
				AttributeId = Attributes.Value,
				Value = new DataValue(new Variant(value), StatusCodes.Good),
			};

			var errors = new ServiceResult[1];
			this.Write(this.SystemContext.OperationContext, [item], errors);
			return ServiceResult.IsGood(errors[0]);
		}
		#endregion

		#region 重写方法
		public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
		{
			if(!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out _objectReferences))
				externalReferences[ObjectIds.ObjectsFolder] = _objectReferences = new List<IReference>();

			if(!externalReferences.TryGetValue(ObjectIds.Server, out _serverReferences))
				externalReferences[ObjectIds.Server] = _serverReferences = new List<IReference>();

			base.CreateAddressSpace(externalReferences);
		}

		protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
		{
			var trigger = new BaseObjectState(null)
			{
				SymbolicName = "Trigger",
				BrowseName = new QualifiedName("Trigger", this.NamespaceIndex),
				DisplayName = "TriggerTitle",
				TypeDefinitionId = ObjectTypeIds.BaseObjectType,
				EventNotifier = EventNotifiers.SubscribeToEvents,
			};

			var property = new PropertyState(trigger)
			{
				BrowseName = new QualifiedName("MyProperty", this.NamespaceIndex),
				DisplayName = "MyPropertyLable",
				TypeDefinitionId = VariableTypeIds.PropertyType,
				ReferenceTypeId = ReferenceTypeIds.HasProperty,
				DataType = DataTypeIds.Int32,
				ValueRank = ValueRanks.TwoDimensions,
				ArrayDimensions = new ReadOnlyList<uint>([2, 2])
			};

			trigger.AddChild(property);

			//var personType = this.GenerateType(typeof(Person));
			//var children = new List<BaseInstanceState>();
			//personType.GetChildren(context, children);

			//var personState = new BaseObjectState(null)
			//{
			//	SymbolicName = "Person1",
			//	BrowseName = new QualifiedName("Person1", this.NamespaceIndex),
			//	DisplayName = "Person1",
			//	ReferenceTypeId = personType.NodeId,
			//	TypeDefinitionId = personType.NodeId,
			//};

			//foreach(var child in children)
			//{
			//	this.AppendChildren(personState, child);
			//}

			//var definedType = this.DefineType(typeof(Person));
			var person = this.DefineObject(new Person()
			{
				Name = "Popeye Zhong",
				Gender = true,
				Birthday = new DateTime(1980, 10, 15),
			});

			var root = this.FindNodeInAddressSpace(Objects.ObjectsFolder);
			var folder = this.CreateFolder(null, null, "MyFolder", "My Folder", "This is my first folder node.");
			var variable = this.CreateVariable(folder, "MyVariable", 123.50, "My Variable", DataTypeIds.Double, ValueRanks.Scalar);

			var nodes = new NodeStateCollection
			{
				trigger,
				//this.GenerateType(typeof(Person)),
				//personState,
				//definedType,
				folder,
				variable,
				person,
			};

			return nodes;
		}

		protected override void AddPredefinedNode(ISystemContext context, NodeState node)
		{
			if(node.NodeId == null || node.NodeId.IsNullNodeId)
				node.NodeId = this.New(context, node);

			switch(node)
			{
				case BaseObjectState instance:
					if(instance.Parent == null)
						instance.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);

					if(instance.EventNotifier != EventNotifiers.None)
						this.AddRootNotifier(instance);

					if(node.SymbolicName != null)
					{
						var referenceType = new ReferenceTypeState
						{
							NodeId = new NodeId(Guid.NewGuid(), node.NodeId.NamespaceIndex),
							SymbolicName = $"{node.SymbolicName}Type",
							BrowseName = new QualifiedName($"{node.BrowseName}Type", node.NodeId.NamespaceIndex),
							DisplayName = $"{node.DisplayName}Type",
							InverseName = new LocalizedText($"IsTypeOf{node.BrowseName}"),
							SuperTypeId = ReferenceTypeIds.NonHierarchicalReferences,
						};

						instance.AddReference(referenceType.NodeId, false, ObjectIds.Server);
						this.AddPredefinedNode(context, referenceType);
					}

					break;
			}

			if(node is BaseObjectState instanceState && instanceState.EventNotifier != EventNotifiers.None)
				this.AddRootNotifier(instanceState);

			base.AddPredefinedNode(context, node);
		}

		public override void AddReferences(IDictionary<NodeId, IList<IReference>> references) => base.AddReferences(references);

		protected override void AddReverseReferences(IDictionary<NodeId, IList<IReference>> externalReferences) => base.AddReverseReferences(externalReferences);

		public override NodeId New(ISystemContext context, NodeState node)
		{
			if(node.NodeId == null || node.NodeId.IsNullNodeId)
				return this.GenerateId();

			return base.New(context, node);
		}

		public override void Write(OperationContext context, IList<WriteValue> nodesToWrite, IList<ServiceResult> errors)
		{
			base.Write(context, nodesToWrite, errors);
		}
		#endregion

		#region 私有方法
		public BaseDataVariableState DefineVariable(string name, Type type, string label, string description = null)
		{
			if(TypeExtension.IsNullable(type, out var underlyingType))
				type = underlyingType;

			var elementType = TypeExtension.GetElementType(type);
			var variable = this.CreateVariable(null, name, null, label, Utility.GetDataType(elementType ?? type), elementType == null ? ValueRanks.Scalar : ValueRanks.OneOrMoreDimensions);
			this.AddPredefinedNode(this.SystemContext, variable);
			return variable;
		}

		public BaseObjectState DefineObject(object instance, string name = null) => this.DefineObject(null, instance, name);
		public BaseObjectState DefineObject(NodeId identifier, object instance, string name = null)
		{
			if(instance == null)
				return null;

			if(string.IsNullOrEmpty(name))
				name = instance.GetType().Name;

			var type = this.DefineType(instance.GetType());
			var state = new BaseObjectState(null)
			{
				NodeId = identifier,
				SymbolicName = name,
				BrowseName = new QualifiedName(name, this.NamespaceIndex),
				DisplayName = name,
				ReferenceTypeId = type.NodeId,
				TypeDefinitionId = type.NodeId,
			};

			var children = new List<BaseInstanceState>();
			type.GetChildren(this.SystemContext, children);

			foreach(var child in children)
			{
				this.AppendChildren(state, child);
			}

			return state;
		}

		public BaseObjectTypeState DefineType(Type type)
		{
			lock(this.Lock)
			{
				if(_types.TryGetValue(type, out var identifer))
					return this.FindPredefinedNode(identifer, null) as BaseObjectTypeState;

				var definition = new BaseObjectTypeState()
				{
					NodeId = new NodeId(++_nodeId, this.NamespaceIndex),
					IsAbstract = type.IsAbstract,
					SymbolicName = $"{type.Name}Type",
					BrowseName = new QualifiedName($"{type.Namespace}.{type.Name}", this.NamespaceIndex),
					DisplayName = $"{type.Namespace}.{type.Name}",
					SuperTypeId = ReferenceTypeIds.HasSubtype,
				};

				foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
				{
					this.DefineProperty(definition, property);
				}

				definition.AddReference(definition.NodeId, true, ObjectIds.ObjectTypesFolder);
				if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
					this.AddPredefinedNode(this.SystemContext, definition);

				_types.TryAdd(type, definition.NodeId);
				return definition;
			}
		}

		private PropertyState DefineProperty(BaseObjectTypeState type, PropertyInfo property)
		{
			var propertyType = TypeExtension.IsNullable(property.PropertyType, out var underlyingType) ? underlyingType : property.PropertyType;
			var elementType = TypeExtension.GetCollectionElementType(propertyType);

			var propertyState = new PropertyState(type)
			{
				NodeId = new NodeId(Guid.NewGuid(), this.NamespaceIndex),
				ReferenceTypeId = ReferenceTypes.HasProperty,
				TypeDefinitionId = VariableTypeIds.PropertyType,
				SymbolicName = property.Name,
				BrowseName = property.Name,
				DisplayName = property.Name,
				DataType = Utility.GetDataType(elementType ?? propertyType),
				ValueRank = elementType == null ? ValueRanks.Scalar : ValueRanks.OneOrMoreDimensions,
				AccessLevel = AccessLevels.CurrentRead,
				UserAccessLevel = AccessLevels.CurrentRead,
				MinimumSamplingInterval = MinimumSamplingIntervals.Indeterminate,
				OnWriteValue = this.OnWriteValue,
			};

			if(propertyState.DataType == DataTypeIds.ObjectTypeNode)
			{
				var definition = this.DefineType(elementType ?? propertyType);
				propertyState.DataType = definition.NodeId;
				//propertyState.TypeDefinitionId = definition.NodeId;
				//propertyState.ReferenceTypeId = definition.NodeId;

				propertyState.AddReference(propertyState.NodeId, true, definition.NodeId);

				if(!this.PredefinedNodes.ContainsKey(definition.NodeId))
					this.AddPredefinedNode(this.SystemContext, definition);
			}

			type.AddChild(propertyState);
			return propertyState;
		}

		private void AppendChildren(BaseInstanceState owner, BaseInstanceState child)
		{
			var replica = child.Clone();

			if(replica is BaseInstanceState instance)
			{
				instance.BrowseName = child.BrowseName;
				instance.DisplayName = child.DisplayName;
				instance.Description = child.Description;

				owner.AddChild(instance);

				var children = new List<BaseInstanceState>();
				child.GetChildren(this.SystemContext, children);

				foreach(var node in children)
				{
					this.AppendChildren(instance, node);
				}
			}
		}

		private NodeId GenerateId() => new(Interlocked.Increment(ref _nodeId), this.NamespaceIndex);
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
				DisplayName = displayName,
				Description = description,
				WriteMask = AttributeWriteMask.None,
				UserWriteMask = AttributeWriteMask.None,
				EventNotifier = EventNotifiers.None
			};

			if(parent != null)
				parent.AddChild(folder);

			return folder;
		}

		private BaseDataVariableState CreateVariable(NodeState parent, string name, object value, string displayName, NodeId dataType, int valueRank)
		{
			var variable = new BaseDataVariableState(parent)
			{
				SymbolicName = name,
				ReferenceTypeId = ReferenceTypes.Organizes,
				TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
				NodeId = new NodeId(name, this.NamespaceIndex),
				BrowseName = new QualifiedName(name, this.NamespaceIndex),
				DisplayName = new LocalizedText("en", displayName),
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
				OnWriteValue = this.OnWriteValue
			};

			if(valueRank == ValueRanks.OneDimension)
			{
				variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
			}
			else if(valueRank == ValueRanks.TwoDimensions)
			{
				variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
			}

			if(parent != null)
				parent.AddChild(variable);

			return variable;
		}
		#endregion

		#region 写值处理
		ServiceResult OnWriteValue(ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
			ref object value,
			ref StatusCode statusCode,
			ref DateTime timestamp)
		{
			if(node is BaseVariableState variable)
			{
				return ServiceResult.Good;
			}

			return new ServiceResult(StatusCodes.BadNodeClassInvalid);
		}
		#endregion
	}

	public class Person
	{
		public string Name { get; set; }
		public bool? Gender { get; set; }
		public DateTime? Birthday { get; set; }
		public Address? HomeAddress { get; set; }
		public Address? OfficeAddress { get; set; }
	}

	public struct Address
	{
		public int Country { get; set; }
		public string Province { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public string Detail { get; set; }
		public string PostalCode { get; set; }
	}
}
