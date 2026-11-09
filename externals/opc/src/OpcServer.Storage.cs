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
using System.Collections.ObjectModel;

using Opc.Ua;
using Opc.Ua.Server;

namespace Zongsoft.Externals.Opc;

partial class OpcServer
{
	public class Storage
	{
		#region 内部字段
		internal readonly NodeManager Manager;
		#endregion

		#region 内部构造
		internal Storage(IServerInternal server, ApplicationConfiguration configuration, OpcServerOptions.StorageOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
			this.Manager = new NodeManager(server, configuration, options);
		}
		#endregion

		#region 公共属性
		public OpcServerOptions.StorageOptions Options { get; }
		#endregion
	}

	public class StorageCollection : KeyedCollection<string, Storage>
	{
		public Storage Find(int index)
		{
			if(index < 1)
				throw new ArgumentOutOfRangeException(nameof(index));

			foreach(var storage in this)
			{
				if(storage.Manager.NamespaceIndex == index)
					return storage;
			}

			return null;
		}

		public Storage Find(string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace))
				return null;

			foreach(var storage in this)
			{
				if(storage.Manager.NamespaceUris.Contains(@namespace))
					return storage;
			}

			return null;
		}

		protected override string GetKeyForItem(Storage storage) => storage.Options.Namespace;
	}
}
