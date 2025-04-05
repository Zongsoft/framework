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

using System;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges.Models;

[Model($"{Module.NAME}.Role")]
public abstract class RoleModel : IRole, IIdentifiable, IIdentifiable<uint>, IEquatable<RoleModel>
{
	#region 公共属性
	public abstract uint RoleId { get; set; }
	public abstract string Name { get; set; }
	public abstract bool Enabled { get; set; }
	public abstract string Avatar { get; set; }
	public abstract string Nickname { get; set; }
	public abstract string Namespace { get; set; }
	public abstract string Description { get; set; }
	#endregion

	#region 标识属性
	Identifier IIdentifiable.Identifier
	{
		get => new(typeof(RoleModel), this.RoleId, this.Name, this.Description);
		set => this.RoleId = value.Validate<IRole, uint>(out var id) ? id : throw new ArgumentException();
	}

	Identifier<uint> IIdentifiable<uint>.Identifier
	{
		get => new(typeof(RoleModel), this.RoleId, this.Name, this.Description);
		set => this.RoleId = value.Validate<IRole>(out var id) ? id : throw new ArgumentException();
	}
	#endregion

	#region 公共方法
	public void Identify<T>(T value)
	{
		switch(value)
		{
			case int id:
				this.RoleId = (uint)id;
				break;
			case uint id:
				this.RoleId = id;
				break;
			case string id:
				this.RoleId = uint.Parse(id);
				break;
			case Identifier id:
				if(id.HasValue)
					this.Identify(id.Value);
				break;
			default:
				throw new InvalidOperationException($"The specified '{value}' value cannot be converted to a role identifier.");
		}
	}
	#endregion

	#region 重写方法
	public virtual bool Equals(RoleModel other) => other is not null && this.RoleId == other.RoleId;
	public override bool Equals(object obj) => obj is RoleModel other && this.Equals(other);
	public override int GetHashCode() => this.RoleId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Namespace) ? $"[{this.RoleId}]{this.Name}" : $"[{this.RoleId}]{this.Namespace}:{this.Name}";
	#endregion
}
