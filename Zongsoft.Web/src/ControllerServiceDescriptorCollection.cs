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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web;

public class ControllerServiceDescriptorCollection : KeyedCollection<string, ControllerServiceDescriptor>
{
	#region 构造函数
	public ControllerServiceDescriptorCollection(IEnumerable<ControllerModel> controllers) : base(StringComparer.OrdinalIgnoreCase)
	{
		foreach(var controller in controllers)
		{
			var descriptor = ControllerServiceDescriptor.Get(controller);
			this.Add(descriptor);
			controller.SetService(descriptor);
		}
	}
	#endregion

	#region 公共方法
	public bool TryGetValue(Services.IApplicationModule module, string name, out ControllerServiceDescriptor value) => this.TryGetValue(module?.Name, name, out value);
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
