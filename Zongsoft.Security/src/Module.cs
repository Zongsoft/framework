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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

[assembly: ApplicationModule(Zongsoft.Security.Module.NAME)]

namespace Zongsoft.Security;

public sealed class Module : ApplicationModule<Module.EventRegistry>
{
	#region 常量定义
	/// <summary>表示安全模块的名称常量值。</summary>
	public const string NAME = nameof(Security);
	#endregion

	#region 单例字段
	/// <summary>表示安全模块的单例字段。</summary>
	public static readonly Module Current = new();
	#endregion

	#region 私有构造
	private Module() : base(NAME, Zongsoft.Security.Properties.Resources.Security_Title, Zongsoft.Security.Properties.Resources.Security_Description) { }
	#endregion

	#region 公共属性
	private IDataAccess _accessor;
	public IDataAccess Accessor => _accessor ??= this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
	#endregion

	#region 嵌套子类
	[System.Reflection.DefaultMember(nameof(Events))]
	public sealed class EventRegistry : EventRegistryBase
	{
		#region 构造函数
		public EventRegistry() : base(NAME)
		{
			this.Event(AuthenticationEvent.Authenticated);
			this.Event(AuthenticationEvent.Authenticating);

			this.Authentication = new(this);
		}
		#endregion

		#region 公共属性
		public AuthenticationEvent Authentication { get; }
		#endregion

		public class AuthenticationEvent
		{
			#region 静态字段
			public static readonly EventDescriptor<Privileges.AuthenticatedEventArgs> Authenticated = new($"{nameof(Privileges.Authentication)}.{nameof(Privileges.Authentication.Authenticated)}");
			public static readonly EventDescriptor<Privileges.AuthenticatingEventArgs> Authenticating = new($"{nameof(Privileges.Authentication)}.{nameof(Privileges.Authentication.Authenticating)}");
			#endregion

			#region 成员字段
			private readonly EventRegistry _registry;
			#endregion

			#region 构造函数
			internal AuthenticationEvent(EventRegistry registry) => _registry = registry;
			#endregion
		}
	}
	#endregion
}