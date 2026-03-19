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
			ReferenceTypeId = ReferenceTypes.Organizes,
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
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to create the folder node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to create the folder node.");
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
			ReferenceTypeId = ReferenceTypes.HasComponent,
			RequestedNewNodeId = null,
			BrowseName = new QualifiedName(name),
			NodeClass = NodeClass.Variable,
			TypeDefinition = VariableTypeIds.BaseDataVariableType,
			NodeAttributes = new ExtensionObject(new VariableAttributes()
			{
				DisplayName = label,
				Description = description,
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
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to create the variable node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0].StatusCode == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(result => StatusCode.IsBad(result.StatusCode));

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to create the variable node.");
		}

		return true;
	}
}
