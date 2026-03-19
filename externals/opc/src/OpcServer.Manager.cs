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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Server;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	internal partial class NodeManager : CustomNodeManager2
	{
		#region 私有字段
		private volatile uint _nodeId;
		private OpcServerOptions.StorageOptions _options;
		private IList<IReference> _objectReferences;
		private Dictionary<Type, NodeId> _types = new();
		#endregion

		#region 构造函数
		internal NodeManager(IServerInternal server, ApplicationConfiguration configuration, OpcServerOptions.StorageOptions options) : base(server, configuration)
		{
			this.SystemContext.NodeIdFactory = this;
			_options = options ?? throw new ArgumentNullException(nameof(options));
			this.SetNamespaces(options.Namespace);
		}
		#endregion

		#region 公共方法
		public (AddNodesResult result, DiagnosticInfo diagnostic) AddNode(OperationContext context, AddNodesItem node)
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
					var variable = this.CreateVariable(
						parent,
						node.BrowseName.Name,
						attributes.Value,
						attributes.DisplayName?.Text,
						attributes.DataType,
						attributes.ValueRank,
						attributes.Description?.Text);

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

		public bool TryGetValue(NodeId nodeId, out object value)
		{
			if(nodeId == null || nodeId.IsNullNodeId)
			{
				value = null;
				return false;
			}

			var request = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.Value,
			};

			var result = new DataValue[1];
			var errors = new ServiceResult[1];

			this.Read(this.SystemContext.OperationContext, 0, [request], result, errors);
			value = result[0].Value;
			return ServiceResult.IsGood(errors[0]);
		}

		public IEnumerable<KeyValuePair<NodeId, object>> GetValues(IEnumerable<NodeId> identifiers)
		{
			if(identifiers == null)
				yield break;

			var request = identifiers.Select(id => new ReadValueId
			{
				NodeId = id,
				AttributeId = Attributes.Value,
			}).ToArray();

			if(request.Length == 0)
				yield break;

			var result = new DataValue[request.Length];
			var errors = new ServiceResult[request.Length];

			this.Read(this.SystemContext.OperationContext, 0, request, result, errors);

			for(int i = 0; i < request.Length; i++)
			{
				if(result[i] == null || errors[i] == null) //NotFound
					yield return new(request[i].NodeId, null);
				else if(ServiceResult.IsGood(errors[i]))
					yield return new(request[i].NodeId, result[i].Value);
				else
					yield return new(request[i].NodeId, Failure.GetFailure(errors[i].StatusCode));
			}
		}

		public Type GetDataType(NodeId nodeId)
		{
			var metadata = this.GetNodeMetadata(
				this.SystemContext.OperationContext,
				this.GetManagerHandle(nodeId),
				BrowseResultMask.NodeClass | BrowseResultMask.TypeDefinition);

			return metadata == null ? null : Utility.GetDataType(metadata.DataType, metadata.ValueRank);
		}

		public bool SetValue<T>(NodeId nodeId, T value)
		{
			if(nodeId == null || nodeId.IsNullNodeId)
				return false;

			var request = new WriteValue()
			{
				NodeId = nodeId,
				AttributeId = Attributes.Value,
				Value = new DataValue(new Variant(value), StatusCodes.Good),
			};

			var errors = new ServiceResult[1];
			this.Write(this.SystemContext.OperationContext, [request], errors);
			return ServiceResult.IsGood(errors[0]);
		}

		public IEnumerable<Failure> SetValues(IEnumerable<KeyValuePair<NodeId, object>> entries)
		{
			if(entries == null)
				return [];

			var request = entries.Select(entry => new WriteValue
			{
				NodeId = entry.Key,
				AttributeId = Attributes.Value,
				Value = new DataValue(new Variant(entry.Value), StatusCodes.Good),
			}).ToArray();

			if(request.Length == 0)
				return [];

			var errors = new ServiceResult[request.Length];
			this.Write(this.SystemContext.OperationContext, request, errors);
			return errors.Where(ServiceResult.IsBad).Select(err => Failure.GetFailure(err.StatusCode));
		}
		#endregion

		#region 重写方法
		public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
		{
			if(!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out _objectReferences))
				externalReferences[ObjectIds.ObjectsFolder] = _objectReferences = new List<IReference>();

			base.CreateAddressSpace(externalReferences);
		}

		protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
		{
			var nodes = new NodeStateCollection(_options.Prefabs.Count);

			foreach(var prefab in _options.Prefabs)
				this.FillPrefabs(prefab, nodes);

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

		protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
		{
			return base.GetManagerHandle(context, nodeId, cache);
		}

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
		private void FillPrefabs(Prefab prefab, NodeStateCollection nodes)
		{
			switch(prefab)
			{
				case Prefab.TypePrefab type:
					prefab.Node = this.DefineType(type.Type);
					break;
				case Prefab.FolderPrefab folder:
					prefab.Node = this.CreateFolder(folder.Folder?.Node, null, folder.Name, folder.Label, folder.Description);
					break;
				case Prefab.ObjectPrefab target:
					prefab.Node = this.DefineValue(target.Folder?.Node, target.Value, target.Name);
					break;
				case Prefab.VariablePrefab variable:
					prefab.Node = this.DefineVariable(variable.Folder?.Node, variable.Name, variable.Type, variable.Value, variable.Label, variable.Description);
					break;
				default:
					prefab.Node = null;
					break;
			}

			if(prefab.Node != null)
				nodes.Add(prefab.Node);

			if(prefab.Kind == PrefabKind.Folder)
			{
				foreach(var child in ((Prefab.FolderPrefab)prefab).Children)
					this.FillPrefabs(child, nodes);
			}
		}

		private NodeId GenerateId() => new(Interlocked.Increment(ref _nodeId), this.NamespaceIndex);
		private NodeId GenerateId(out uint id)
		{
			id = Interlocked.Increment(ref _nodeId);
			return new(id, this.NamespaceIndex);
		}

		private NodeId GenerateId(string identifier, out string name)
		{
			if(string.IsNullOrWhiteSpace(identifier))
			{
				var result = this.GenerateId(out var id);
				name = id.ToString();
				return result;
			}

			if(identifier[0] == '#')
			{
				if(identifier.Length == 1)
				{
					var result = this.GenerateId(out var id);
					name = id.ToString();
					return result;
				}

				name = identifier[1..];

				if(uint.TryParse(identifier.AsSpan()[1..], out var integer))
					return new NodeId(integer, this.NamespaceIndex);
				if(Guid.TryParse(identifier.AsSpan()[1..], out var guid))
					return new NodeId(guid, this.NamespaceIndex);
			}

			if(identifier.Equals("guid()", StringComparison.OrdinalIgnoreCase) ||
			   identifier.Equals("uuid()", StringComparison.OrdinalIgnoreCase))
			{
				var guid = Guid.NewGuid();
				name = guid.ToString();
				return new NodeId(guid, this.NamespaceIndex);
			}

			name = identifier;
			return new NodeId(identifier, this.NamespaceIndex);
		}
		#endregion
	}
}
