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
using Opc.Ua.Configuration;
using Org.BouncyCastle.Tls.Crypto;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	internal class NodeManager : CustomNodeManager2
	{
		#region 私有字段
		private uint _nodeId;
		private IList<IReference> _references; //外部引用
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

					var root = this.Server.CoreNodeManager.Find(ObjectIds.ObjectsFolder, null);
					var folder = this.AddPredefinedFolder(null, null, node.BrowseName.Name, displayName, description);
					//this.AddPredefinedFolder(null, null, "MyFirstFolder100", "My First Folder100", "This is my first folder node(100).");

					var found = this.PredefinedNodes.Values.FirstOrDefault(node => string.Equals(node.SymbolicName, "MyFirstFolderEx"));
					var variable = this.CreateVariable(found, "variable1", Random.Shared.NextDouble(), "My Variable #1", DataTypeIds.Double, 0);
					this.AddPredefinedNode(this.SystemContext, variable);
					found.ClearChangeMasks(this.SystemContext, false);

					//var rootNode = this.FindNodeInAddressSpace(ObjectIds.ObjectsFolder);
					//rootNode.AddReference(folder.TypeDefinitionId, false, folder.NodeId);

					//rootNode?.ClearChangeMasks(this.SystemContext, true);
					//folder.ClearChangeMasks(this.SystemContext, true);

					return (new AddNodesResult()
					{
						StatusCode = StatusCodes.Good,
						AddedNodeId = folder.NodeId,
					}, null);
				}
				else
				{
					throw new ServiceResultException(StatusCodes.BadTypeDefinitionInvalid);
				}
			}

			throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
		}
		#endregion

		#region 重写方法
		public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
		{
			if(!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out _references))
				externalReferences[ObjectIds.ObjectsFolder] = _references = new List<IReference>();

			base.CreateAddressSpace(externalReferences);
		}

		protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
		{
			var nodes = new NodeStateCollection
			{
				this.AddPredefinedFolder(null, null, "MyFirstFolderEx", "My First Folder Ex", "This is my first folder node(Ex).")
			};

			return nodes;
		}

		public override NodeId New(ISystemContext context, NodeState node)
		{
			if(node == null || node.NodeId.IsNullNodeId)
				return new NodeId(++_nodeId, this.NamespaceIndex);

			return base.New(context, node);
		}

		public override void Write(OperationContext context, IList<WriteValue> nodes, IList<ServiceResult> errors)
		{
			base.Write(context, nodes, errors);
		}
		#endregion

		#region 私有方法
		private FolderState AddPredefinedFolder(FolderState parent, ExpandedNodeId id, string name, string displayName, string description)
		{
			var folder = this.CreateFolder(parent, id, name, displayName, description);

			folder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
			_references?.Add(new NodeStateReference(ReferenceTypes.Organizes, false, folder.NodeId));

			folder.EventNotifier = EventNotifiers.SubscribeToEvents;
			this.AddRootNotifier(folder);

			this.AddPredefinedNode(this.SystemContext, folder);

			return folder;
		}

		private FolderState CreateFolder(FolderState parent, ExpandedNodeId id, string name, string displayName, string description)
		{
			var nodeId = id == null || id.IsNull ? new NodeId(Guid.NewGuid(), this.NamespaceIndex) : (NodeId)id;

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
				SymbolicName = displayName,
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
				OnWriteValue = OnWriteDataValue
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

		private ServiceResult OnWriteDataValue(
			ISystemContext context,
			NodeState node,
			NumericRange indexRange,
			QualifiedName dataEncoding,
			ref object value,
			ref StatusCode statusCode,
			ref DateTime timestamp)
		{
			var variable = node as BaseDataVariableState;

			try
			{
				//验证数据类型
				TypeInfo typeInfo = TypeInfo.IsInstanceOfDataType(
					value,
					variable.DataType,
					variable.ValueRank,
					context.NamespaceUris,
					context.TypeTable);

				if(typeInfo == null || typeInfo == TypeInfo.Unknown)
					return StatusCodes.BadTypeMismatch;

				if(typeInfo.BuiltInType == BuiltInType.Double)
				{
					double number = Convert.ToDouble(value);
					value = TypeInfo.Cast(number, typeInfo.BuiltInType);
				}

				return ServiceResult.Good;
			}
			catch(Exception)
			{
				return StatusCodes.BadTypeMismatch;
			}
		}
		#endregion
	}
}
