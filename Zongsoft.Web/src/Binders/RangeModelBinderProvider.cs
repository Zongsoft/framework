/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zongsoft.Web.Binders
{
	[Zongsoft.Services.Service]
	public class RangeModelBinderProvider : IModelBinderProvider
	{
		private static readonly ConcurrentDictionary<Type, IModelBinder> _binders = new ConcurrentDictionary<Type, IModelBinder>();

		public IModelBinder GetBinder(ModelBinderProviderContext context)
		{
			var modelType = context.Metadata.UnderlyingOrModelType;

			if(modelType.IsGenericType && modelType.GenericTypeArguments.Length == 1 && modelType.GetGenericTypeDefinition() == typeof(Zongsoft.Data.Range<>))
				return _binders.GetOrAdd(modelType.GenericTypeArguments[0], type => (IModelBinder)Activator.CreateInstance(typeof(RangeModelBinder<>).MakeGenericType(type)));

			return null;
		}
	}
}
