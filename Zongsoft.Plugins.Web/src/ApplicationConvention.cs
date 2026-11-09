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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Services;

namespace Zongsoft.Web;

public class ApplicationConvention : IApplicationModelConvention
{
	public void Apply(ApplicationModel application)
	{
		ApplicationContext.Current.Properties[typeof(ControllerServiceDescriptorCollection)] = new ControllerServiceDescriptorCollection(application.Controllers);
	}
}
