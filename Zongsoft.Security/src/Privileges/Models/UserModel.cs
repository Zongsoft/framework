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

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges.Models;

[Model($"{Module.NAME}.User")]
public abstract class UserModel : IUser, IIdentifiable, IIdentifiable<uint>, IEquatable<UserModel>
{
	#region 公共属性
	public abstract uint UserId { get; set; }
	public abstract string Name { get; set; }
	public abstract string Email { get; set; }
	public abstract string Phone { get; set; }
	public abstract bool? Gender { get; set; }
	public abstract bool Enabled { get; set; }
	public abstract string Avatar { get; set; }
	public abstract string Nickname { get; set; }
	public abstract string Namespace { get; set; }
	public abstract string Description { get; set; }
	#endregion

	#region 标识属性
	Identifier IIdentifiable.Identifier
	{
		get => new(Users.Identifiers.User, this.UserId, this.Name, this.Description);
		set => this.UserId = value.TryGetValue<uint>(Users.Identifiers.User, out var id) ? id : throw new ArgumentException();
	}

	Identifier<uint> IIdentifiable<uint>.Identifier
	{
		get => new(Users.Identifiers.User, this.UserId, this.Name, this.Description);
		set => this.UserId = value.TryGetValue(Users.Identifiers.User, out var id) ? id : throw new ArgumentException();
	}
	#endregion

	#region 公共方法
	public void Identify<T>(T value)
	{
		switch(value)
		{
			case int id:
				this.UserId = (uint)id;
				break;
			case uint id:
				this.UserId = id;
				break;
			case string id:
				this.UserId = uint.Parse(id);
				break;
			case Identifier id:
				if(id.HasValue)
					this.Identify(id.Value);
				break;
			default:
				throw new InvalidOperationException(string.Format(Properties.Resources.User_IdentifierInvalid_Message, value));
		}
	}
	#endregion

	#region 重写方法
	public virtual bool Equals(UserModel other) => other is not null && this.UserId == other.UserId;
	public override bool Equals(object obj) => obj is UserModel other && this.Equals(other);
	public override int GetHashCode() => this.UserId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Namespace) ? $"[{this.UserId}]{this.Name}" : $"[{this.UserId}]{this.Namespace}:{this.Name}";
	#endregion
}
