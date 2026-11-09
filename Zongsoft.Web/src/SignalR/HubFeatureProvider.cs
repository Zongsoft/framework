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