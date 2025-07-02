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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Zongsoft.Web.SignalR;

public class HubFeatureProvider : IApplicationFeatureProvider<HubFeature>
{
	public void PopulateFeature(IEnumerable<ApplicationPart> parts, HubFeature feature)
	{
		foreach(var part in parts.OfType<IApplicationPartTypeProvider>())
		{
			foreach(var type in part.Types)
			{
				if(this.IsHub(type) && !feature.Contains(type))
					feature.Hubs.Add(new(type));
			}
		}
	}

	protected virtual bool IsHub(TypeInfo type)
	{
		if(!type.IsClass)
			return false;

		if(type.IsAbstract)
			return false;

		if(!type.IsPublic)
			return false;

		if(type.ContainsGenericParameters)
			return false;

		return type.IsAssignableTo(typeof(Hub));
	}
}