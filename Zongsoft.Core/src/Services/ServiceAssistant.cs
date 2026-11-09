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
