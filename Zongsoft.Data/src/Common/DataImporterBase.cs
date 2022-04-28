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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public abstract class DataImporterBase : IDataImporter
	{
		#region 构造函数
		protected DataImporterBase(DataImportContextBase context)
		{
			static MemberInfo GetMemberInfo(Type type, string name) =>
				(MemberInfo)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) ??
				(MemberInfo)type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

			if(context.Members == null || context.Members.Length == 0)
			{
				var members = new List<MemberInfo>(context.Entity.Properties.Count);

				foreach(var property in context.Entity.Properties)
				{
					if(property.IsComplex)
						continue;

					var info = GetMemberInfo(context.ModelType, property.Name);
					if(info != null)
						members.Add(info);
				}

				this.Members = members.ToArray();
			}
			else
			{
				var members = new List<MemberInfo>(context.Members.Length);

				for(int i = 0; i < context.Members.Length; i++)
				{
					if(context.Entity.Properties.TryGet(context.Members[i], out var property))
					{
						if(property.IsComplex)
							throw new DataException($"The specified '{property.Name}' property cannot be a navigation property, only scalar field data can be import.");

						var info = GetMemberInfo(context.ModelType, property.Name);
						if(info != null)
							members.Add(info);
					}
				}

				this.Members = members.ToArray();
			}
		}
		#endregion

		#region 公共属性
		public MemberInfo[] Members { get; }
		#endregion

		#region 导入方法
		public abstract void Import(DataImportContext context);
		public abstract ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);

			if(disposed == 0)
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		private volatile int _disposed;
		protected virtual void Dispose(bool disposing) { }
		#endregion
	}
}
