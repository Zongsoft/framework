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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Security;

partial class Module
{
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
			public static readonly EventDescriptor<Privileges.AuthenticatedEventArgs> Authenticated = new(Predication.Predicate<Privileges.AuthenticatedEventArgs>(OnAuthenticated), $"{nameof(Privileges.Authentication)}.{nameof(Privileges.Authentication.Authenticated)}");
			public static readonly EventDescriptor<Privileges.AuthenticatingEventArgs> Authenticating = new(Predication.Predicate<Privileges.AuthenticatingEventArgs>(OnAuthenticating), $"{nameof(Privileges.Authentication)}.{nameof(Privileges.Authentication.Authenticating)}");
			#endregion

			#region 成员字段
			private readonly EventRegistry _registry;
			#endregion

			#region 构造函数
			internal AuthenticationEvent(EventRegistry registry)
			{
				_registry = registry;

				Authenticated.Bind(
					typeof(Privileges.Authentication),
					nameof(Privileges.Authentication.Authenticated));

				Authenticating.Bind(
					typeof(Privileges.Authentication),
					nameof(Privileges.Authentication.Authenticating));
			}
			#endregion

			#region 私有方法
			private static bool OnAuthenticated(Privileges.AuthenticatedEventArgs args)
			{
				if(args.HasException(out var exception))
					Current.Meter.Authentication.Authenticated.Add(1,
						new KeyValuePair<string, object>(nameof(args.Scheme), args.Scheme),
						new KeyValuePair<string, object>(nameof(args.Scenario), args.Scenario),
						new KeyValuePair<string, object>(nameof(args.Exception), GetTypeName(exception.GetType())),
						new KeyValuePair<string, object>(nameof(Privileges.AuthenticationException.Reason), GetReason(exception)));
				else
					Current.Meter.Authentication.Authenticated.Add(1,
						new KeyValuePair<string, object>(nameof(args.Scheme), args.Scheme),
						new KeyValuePair<string, object>(nameof(args.Scenario), args.Scenario),
						new KeyValuePair<string, object>(nameof(args.IsAuthenticated), args.IsAuthenticated));

				return true;

				static string GetTypeName(Type type)
				{
					if(type == null)
						return null;

					return string.IsNullOrEmpty(type.Namespace) ?
						type.Name : $"{type.Namespace}.{type.Name}";
				}

				static string GetReason(Exception exception) => (exception as Privileges.AuthenticationException)?.Reason;
			}

			private static bool OnAuthenticating(Privileges.AuthenticatingEventArgs args)
			{
				Current.Meter.Authentication.Authenticating.Add(1,
					new KeyValuePair<string, object>(nameof(args.Scheme), args.Scheme),
					new KeyValuePair<string, object>(nameof(args.Scenario), args.Scenario));

				return true;
			}
			#endregion
		}
	}
}
