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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

internal static class ServiceAssistant
{
	private static readonly ConditionalWeakTable<IServiceCollection, Dictionary<string, TaggedServiceDescriptor>> _collections = new();
	private static readonly ConditionalWeakTable<IServiceProvider, Dictionary<string, TaggedServiceDescriptor>> _providers = new();

	public static TaggedServiceDescriptor GetTagged(IServiceCollection collection, string tag)
	{
		if(collection == null)
			throw new ArgumentNullException(nameof(collection));

		var descriptors = _collections.GetValue(collection, static _ => new Dictionary<string, TaggedServiceDescriptor>(StringComparer.OrdinalIgnoreCase));

		lock(descriptors)
		{
			if(descriptors.TryGetValue(tag, out var descriptor))
				return descriptor;

			return descriptors[tag] = new TaggedServiceDescriptor(tag);
		}
	}

	public static TaggedServiceDescriptor GetTagged(IServiceProvider provider, string tag)
	{
		if(provider == null)
			throw new ArgumentNullException(nameof(provider));

		if(tag == null || !_providers.TryGetValue(provider, out var descriptors))
			return null;

		lock(descriptors)
			return descriptors.TryGetValue(tag, out var descriptor) ? descriptor : null;
	}

	public static void Make(IServiceCollection collection, IServiceProvider provider)
	{
		if(collection == null || provider == null || !_collections.TryGetValue(collection, out var descriptors))
			return;

		_collections.Remove(collection);

		lock(_providers)
		{
			_providers.Remove(provider);
			_providers.Add(provider, descriptors);
		}
	}

	public sealed class TaggedServiceDescriptor(string tag)
	{
		public readonly string Tag = tag;
		public readonly Dictionary<Type, HashSet<Type>> Services = new();

		public void SetService(Type serviceType, params IEnumerable<Type> contracts)
		{
			if(serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));

			lock(this.Services)
			{
				if(!this.Services.TryGetValue(serviceType, out var hashset))
					this.Services.Add(serviceType, hashset = [serviceType]);

				if(contracts != null)
					hashset.UnionWith(contracts);
			}
		}
	}
}
