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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public async ValueTask<bool> CreateFolderAsync(string name, string description = null, CancellationToken cancellation = default)
	{
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var session = this.GetSession();
		var root = await session.NodeCache.FindAsync(ObjectIds.ObjectsFolder, cancellation);

		var folderNode = new AddNodesItem()
		{
			BrowseName = new QualifiedName(name),
			NodeClass = NodeClass.Object,
			ReferenceTypeId = ReferenceTypeIds.Organizes,
			TypeDefinition = ObjectTypeIds.FolderType,
			NodeAttributes = new ExtensionObject(new ObjectAttributes()
			{
				DisplayName = new LocalizedText(name),
				Description = new LocalizedText(description),
				EventNotifier = EventNotifiers.None,
				WriteMask = (uint)AttributeWriteMask.None,
				UserWriteMask = (uint)AttributeWriteMask.None,
				SpecifiedAttributes = (uint)(NodeAttributesMask.DisplayName | NodeAttributesMask.Description | NodeAttributesMask.EventNotifier | NodeAttributesMask.WriteMask | NodeAttributesMask.UserWriteMask),
			}),
		};

		var response = await session.AddNodesAsync(request, [folderNode], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException(string.Format(Properties.Resources.Opc_FolderCreationFailed_Message, response.ResponseHeader.ServiceResult));

		if(response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.ToArray().Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException(string.Format(Properties.Resources.Opc_FolderCreationFailed_Message, string.Join(',', failures)));
		}

		return true;
	}

	public async ValueTask<bool> CreateVariableAsync(string name, Type type, string label, string description = null, CancellationToken cancellation = default)
	{
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var variableNode = new AddNodesItem
		{
			ReferenceTypeId = ReferenceTypeIds.HasComponent,
			RequestedNewNodeId = default,
			BrowseName = new QualifiedName(name),
			NodeClass = NodeClass.Variable,
			TypeDefinition = VariableTypeIds.BaseDataVariableType,
			NodeAttributes = new ExtensionObject(new VariableAttributes()
			{
				DisplayName = new(label),
				Description = new(description),
				DataType = Utility.GetBuiltinType(type, out var rank),
				ValueRank = rank,
				ArrayDimensions = [],
				AccessLevel = AccessLevels.CurrentReadOrWrite,
				UserAccessLevel = AccessLevels.CurrentReadOrWrite,
				MinimumSamplingInterval = 0,
				Historizing = false,
				WriteMask = (uint)AttributeWriteMask.None,
				UserWriteMask = (uint)AttributeWriteMask.None,
				SpecifiedAttributes = (uint)NodeAttributesMask.All,
			}),
		};

		var session = this.GetSession();
		var response = await session.AddNodesAsync(request, [variableNode], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException(string.Format(Properties.Resources.Opc_VariableCreationFailed_Message, response.ResponseHeader.ServiceResult));

		if(response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.ToArray().Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException(string.Format(Properties.Resources.Opc_VariableCreationFailed_Message, string.Join(',', failures)));
		}

		return true;
	}
}
