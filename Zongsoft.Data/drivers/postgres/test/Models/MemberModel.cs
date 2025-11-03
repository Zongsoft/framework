using System;

namespace Zongsoft.Data.PostgreSql.Tests.Models;

[Model("Security.Member")]
public abstract class MemberModel : IEquatable<MemberModel>
{
	#region 公共属性
	public abstract uint RoleId { get; set; }
	public abstract uint MemberId { get; set; }
	public abstract MemberType MemberType { get; set; }
	public abstract RoleModel Role { get; set; }
	public object Member => this.MemberType switch
	{
		MemberType.User => this.MemberUser,
		MemberType.Role => this.MemberRole,
		_ => null
	};

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public abstract RoleModel MemberRole { get; set; }

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public abstract UserModel MemberUser { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(MemberModel other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;

	public override bool Equals(object obj) => obj is MemberModel other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.RoleId, this.MemberId, this.MemberType);
	public override string ToString() => $"{this.RoleId}:{this.MemberId}@{this.MemberType}";
	#endregion
}
