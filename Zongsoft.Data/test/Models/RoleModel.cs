using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

[Model("Security.Role")]
public abstract class RoleModel : IEquatable<RoleModel>
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

	#region 集合属性
	public abstract IEnumerable<MemberModel> Children { get; set; }
	public abstract IEnumerable<MemberModel> Parents { get; set; }
	public abstract IEnumerable<RoleModel> Roles { get; set; }
	#endregion

	#region 重写方法
	public virtual bool Equals(RoleModel other) => other is not null && this.RoleId == other.RoleId;
	public override bool Equals(object obj) => obj is RoleModel other && this.Equals(other);
	public override int GetHashCode() => this.RoleId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Namespace) ?
		$"[{this.RoleId}]{this.Name}" :
		$"[{this.RoleId}]{this.Namespace}:{this.Name}";
	#endregion
}
