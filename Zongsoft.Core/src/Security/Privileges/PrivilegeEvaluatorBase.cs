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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class PrivilegeEvaluatorBase : IPrivilegeEvaluator, IMatchable, IMatchable<ClaimsPrincipal>
{
	#region 保护属性
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 公共方法
	public IAsyncEnumerable<IPrivilegeEvaluatorResult> EvaluateAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
		{
			var principal = this.Principal;

			if(principal == null || principal.Identity.IsAnonymous())
				return Zongsoft.Collections.Enumerable.Empty<IPrivilegeEvaluatorResult>();

			identifier = new Identifier(typeof(IUser), principal.Identity.GetIdentifier());
		}

		var context = this.CreateContext(identifier, parameters);
		return this.OnEvaluateAsync(context, cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual Context CreateContext(Identifier identifier, Parameters parameters) => new(identifier, parameters);
	protected virtual IPrivilegeEvaluatorResult OnResult(Context context, string privilege) => new PrivilegeEvaluatorResult(privilege);
	protected virtual IAsyncEnumerable<IPrivilegeEvaluatorResult> OnEvaluateAsync(Context context, CancellationToken cancellation)
	{
		if(context.Statements == null || context.Statements.Count == 0)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilegeEvaluatorResult>();

		var privileges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		//上下文中的权限声明集已确保按层级由远及近的访问方式
		foreach(var statements in context.Statements)
		{
			//同级拒绝优先
			foreach(var statement in statements)
			{
				switch(statement.PrivilegeMode)
				{
					case PrivilegeMode.Denied:
						privileges.Remove(statement.PrivilegeName);
						break;
					case PrivilegeMode.Granted:
						privileges.Add(statement.PrivilegeName);
						break;
				}
			}
		}

		return privileges.Select(privilege => this.OnResult(context, privilege)).Asynchronize();
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion

	#region 嵌套子类
	protected class Context
	{
		#region 构造函数
		public Context(Identifier identifier, Parameters parameters)
		{
			this.Identifier = identifier;
			this.Parameters = parameters;
			this.Statements = new List<StatementCollection>();
		}
		#endregion

		#region 公共属性
		public Identifier Identifier { get; }
		public Parameters Parameters { get; }
		public ICollection<StatementCollection> Statements { get; }
		#endregion
	}

	protected class Statement : IEquatable<Statement>
	{
		#region 构造函数
		public Statement(string privilegeName, PrivilegeMode privilegeMode)
		{
			if(string.IsNullOrEmpty(privilegeName))
				throw new ArgumentNullException(nameof(privilegeName));

			this.PrivilegeName = privilegeName.ToLowerInvariant();
			this.PrivilegeMode = privilegeMode;
		}
		#endregion

		#region 公共属性
		public string PrivilegeName { get; }
		public PrivilegeMode PrivilegeMode { get; }
		#endregion

		#region 重写方法
		public bool Equals(Statement other) => other is not null && string.Equals(this.PrivilegeName, other.PrivilegeName, StringComparison.OrdinalIgnoreCase) && this.PrivilegeMode == other.PrivilegeMode;
		public override bool Equals(object obj) => obj is Statement other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.PrivilegeName, this.PrivilegeMode);
		public override string ToString() => $"{this.PrivilegeName}({this.PrivilegeMode})";
		#endregion
	}

	protected class StatementCollection(params IEnumerable<Statement> statements) : ICollection<Statement>
	{
		#region 成员字段
		private readonly HashSet<Statement> _statements = statements == null ? new() : new(statements);
		#endregion

		#region 公共属性
		public int Count => _statements.Count;
		public bool IsEmpty => _statements.Count == 0;
		#endregion

		#region 公共方法
		public void Clear() => _statements.Clear();
		public bool Contains(Statement statement) => statement != null && _statements.Contains(statement);
		public bool Remove(Statement statement) => statement != null && _statements.Remove(statement);
		public bool Add(Statement statement) => statement != null && _statements.Add(statement);
		public void Add(IEnumerable<Statement> statements) => _statements.UnionWith(statements);
		#endregion

		#region 显式实现
		bool ICollection<Statement>.IsReadOnly => false;
		void ICollection<Statement>.Add(Statement statement) => this.Add(statement);
		public void CopyTo(Statement[] array, int arrayIndex) => _statements.CopyTo(array, arrayIndex);
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Statement> GetEnumerator() => _statements.GetEnumerator();
		#endregion
	}
	#endregion
}
