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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web.Filters;

public class GlobalFilterConvention : IApplicationModelConvention
{
	public void Apply(ApplicationModel application)
	{
		var provider = Zongsoft.Services.ApplicationContext.Current?.Services;
		var filters = (IEnumerable<IFilterMetadata>)provider.GetService(typeof(IEnumerable<IFilterMetadata>));

		if(filters != null)
		{
			foreach(var filter in filters)
				application.Filters.Add(filter);
		}
	}
}
