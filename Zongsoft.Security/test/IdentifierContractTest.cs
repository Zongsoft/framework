using System;
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Tests;

public class IdentifierContractTest
{
	[Fact]
	public void ModuleRegistersIdentifierTypeAliases()
	{
		Assert.NotNull(Module.Current);
		Identifier user = Member.User(17u);
		Identifier role = Member.Role(23u);
		Assert.Equal("$Zongsoft.Security.User:<uint>17", user.ToString());
		Assert.Equal("$Zongsoft.Security.Role:<uint>23", role.ToString());
		Assert.Equal(user, Identifier.Parse(user.ToString()));
		Assert.Equal(role, Identifier.Parse(role.ToString()));
		Assert.Equal(user, Identifier.Parse("$Zongsoft.Security.Privileges.IUser@Zongsoft.Core:<uint>17"));
	}

	[Fact]
	public void ModelsValidateCategoriesAndConvertKeys()
	{
		var user = Model.Build<UserModel>();
		var role = Model.Build<RoleModel>();
		IIdentifiable userIdentity = user;
		IIdentifiable roleIdentity = role;
		userIdentity.Identifier = new Identifier(typeof(IUser), "17");
		roleIdentity.Identifier = new Identifier(typeof(IRole), "23");
		Assert.Equal(17u, user.UserId);
		Assert.Equal(23u, role.RoleId);
		Assert.Equal(new Identifier(typeof(IUser), 17u), userIdentity.Identifier);
		Assert.Equal(new Identifier(typeof(IRole), 23u), roleIdentity.Identifier);
		Assert.Throws<ArgumentException>(() => userIdentity.Identifier = roleIdentity.Identifier);
		Assert.Throws<ArgumentException>(() => roleIdentity.Identifier = userIdentity.Identifier);
		Assert.Equal(17u, user.UserId);
		Assert.Equal(23u, role.RoleId);
	}

	[Fact]
	public void ClaimsFillUserIdentifier()
	{
		var identity = new ClaimsIdentity([
			new Claim(ClaimTypes.NameIdentifier, "17", ClaimValueTypes.UInteger32),
			new Claim(ClaimTypes.Name, "alice"),
		], "Test");
		var user = identity.AsModel<UserModel>();
		Assert.Equal(17u, user.UserId);
		Assert.Equal("alice", user.Name);
		Assert.Equal(((IIdentifiable)user).Identifier, identity.Identify());
	}

	[Theory]
	[InlineData(MemberType.User)]
	[InlineData(MemberType.Role)]
	public void MembersExposeActualIdentity(MemberType type)
	{
		var model = Model.Build<MemberModel>();
		model.RoleId = 23;
		model.MemberId = 17;
		model.MemberType = type;
		var other = Model.Build<MemberModel>();
		other.RoleId = model.RoleId;
		other.MemberId = model.MemberId;
		other.MemberType = model.MemberType;
		var member = (IMember)model;
		Assert.Equal(new Identifier(typeof(IRole), 23u), member.RoleId);
		Assert.Equal(new Identifier(type == MemberType.User ? typeof(IUser) : typeof(IRole), 17u), member.MemberId);
		Assert.True(member.Equals(other));
		Assert.True(model.Equals((IMember<RoleModel>)other));
		Assert.Equal(model.GetHashCode(), other.GetHashCode());
	}
}
