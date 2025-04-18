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

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	internal class NodeManager : CustomNodeManager2
	{
		#region 构造函数
		public NodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, ["http://zongsoft.com/opc/ua", "http://zongsoft.com/opc-ua"])
		{
		}
		#endregion

		#region 公共方法
		public void AddNodes(OperationContext context, AddNodesItemCollection nodesToAdd, out AddNodesResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
		{
			// Validate nodesToAdd parameter
			if(nodesToAdd == null)
			{
				throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The nodesToAdd parameter is null.");
			}

			// Create result lists
			results = new AddNodesResultCollection(nodesToAdd.Count);
			diagnosticInfos = new DiagnosticInfoCollection(nodesToAdd.Count);

			for(int ii = 0; ii < nodesToAdd.Count; ii++)
			{
				// Call AddNode and update results
				AddNodesResult addResult;
				DiagnosticInfo diagnosticInfo;

				AddNode(context, nodesToAdd[ii], out addResult, out diagnosticInfo);

				results.Add(addResult);
				diagnosticInfos.Add(diagnosticInfo);
			}
		}

		private void AddNode(OperationContext context, AddNodesItem nodeToAdd, out AddNodesResult result, out DiagnosticInfo diagnosticInfo)
		{
			result = new AddNodesResult();
			diagnosticInfo = new DiagnosticInfo();

			try
			{
				// TODO: Add node in Address space
			}
			catch { }
		}
		#endregion
	}
}
