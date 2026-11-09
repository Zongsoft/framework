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
using System.Collections.ObjectModel;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Web;

public class ControllerServiceDescriptorCollection : KeyedCollection<string, ControllerServiceDescriptor>
{
	#region 构造函数
	public ControllerServiceDescriptorCollection(IEnumerable<ControllerModel> controllers) : base(StringComparer.OrdinalIgnoreCase)
	{
		foreach(var controller in controllers)
		{
			var qualifiedName = ControllerServiceDescriptor.GetQualifiedName(controller);

			if(this.TryGetValue(qualifiedName, out var descriptor))
				descriptor.Controllers.Add(new ControllerServiceDescriptor.ControllerDescriptor(controller));
			else
			{
				descriptor = ControllerServiceDescriptor.Create(controller);
				this.Add(descriptor);
				controller.SetService(descriptor);
			}
		}
	}
	#endregion

	#region 公共方法
	public bool TryGetValue(IApplicationModule module, string name, out ControllerServiceDescriptor value) => this.TryGetValue(module?.Name, name, out value);
	public bool TryGetValue(string module, string name, out ControllerServiceDescriptor value)
	{
		if(string.IsNullOrEmpty(name))
		{
			value = null;
			return false;
		}

		return string.IsNullOrEmpty(module) ?
			this.TryGetValue(name, out value) :
			this.TryGetValue($"{module}:{name}", out value);
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(ControllerServiceDescriptor descriptor) => descriptor.QualifiedName;
	#endregion
}
