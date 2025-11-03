using System;
using System.Collections.Generic;

namespace Zongsoft.Data.PostgreSql.Tests.Models;

[Model("Security.User")]
public abstract class UserModel : IEquatable<UserModel>
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

	#region 集合属性
	public abstract IEnumerable<MemberModel> Parents { get; set; }
	public abstract IEnumerable<RoleModel> Roles { get; set; }
	#endregion

	#region 重写方法
	public virtual bool Equals(UserModel other) => other is not null && this.UserId == other.UserId;
	public override bool Equals(object obj) => obj is UserModel other && this.Equals(other);
	public override int GetHashCode() => this.UserId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Namespace) ?
		$"[{this.UserId}]{this.Name}" :
		$"[{this.UserId}]{this.Namespace}:{this.Name}";
	#endregion
}
