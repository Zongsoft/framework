using System.Security.Claims;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Tests;

public class UserIdentityTest
{
	[Theory]
	[InlineData(1u)]
	[InlineData(941u)]
	public void PlatformAdministratorHasImplicitRoles(uint id)
	{
		var identity = new TestUser(id, IUser.Administrator).Identity("test", "test");

		Assert.True(identity.IsAdministrator());
		Assert.True(new ClaimsPrincipal(identity).IsInRole(IRole.Administrators));
		Assert.True(new ClaimsPrincipal(identity).IsInRole(IRole.Security));
	}

	[Theory]
	[InlineData(1u)]
	[InlineData(207u)]
	public void TenantAdministratorHasStandardRolesAndNamespace(uint id)
	{
		var identity = new TestUser(id, IUser.Administrator) { Namespace = "tenant-a" }.Identity("test", "test");

		Assert.True(identity.IsAdministrator());
		Assert.True(new ClaimsPrincipal(identity).IsInRole(IRole.Administrators));
		Assert.True(new ClaimsPrincipal(identity).IsInRole(IRole.Security));
		Assert.Equal("tenant-a", identity.GetNamespace());
	}

	[Theory]
	[InlineData(1u, "Other", null)]
	[InlineData(207u, "Other", "tenant-a")]
	public void OtherUsersDoNotReceiveBuiltinRoles(uint id, string name, string @namespace)
	{
		var identity = new TestUser(id, name) { Namespace = @namespace }.Identity("test", "test");

		Assert.False(identity.IsAdministrator());
		Assert.False(new ClaimsPrincipal(identity).IsInRole(IRole.Administrators));
		Assert.False(new ClaimsPrincipal(identity).IsInRole(IRole.Security));
	}

	[Fact]
	public void AuthenticatedAdministratorNeedsIdentifierButNotFixedNumericId()
	{
		var identity = new ClaimsIdentity("test");
		identity.AddClaim(new Claim(identity.NameClaimType, IUser.Administrator));

		Assert.False(identity.IsAdministrator());

		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "tenant-specific-key"));
		Assert.True(identity.IsAdministrator());
	}

	[Fact]
	public void AdministratorRoleUsesTheIdentityNamespace()
	{
		var identity = new TestUser(207, "Member") { Namespace = "tenant-a" }.Identity("test", "test");
		identity.AddRole("tenant-b:" + IRole.Administrators);
		Assert.False(identity.IsAdministrator());

		identity.AddRole(IRole.Administrators);
		Assert.True(identity.IsAdministrator());
		Assert.Equal("tenant-a", identity.GetNamespace());
	}

	[Theory]
	[InlineData("tenant-a:Alice", "Name", "Alice", "tenant-a")]
	[InlineData(":Administrator", "Name", "Administrator", null)]
	[InlineData("tenant-a:email:alice@example.invalid", "Email", "alice@example.invalid", "tenant-a")]
	[InlineData("tenant-a:phone:+123456", "Phone", "+123456", "tenant-a")]
	public void QualifiedUserCriteriaIncludeNamespace(string identifier, string field, string value, string @namespace)
	{
		var criteria = Assert.IsType<ConditionCollection>(new TestUserService().Criteria(identifier));

		Assert.Equal(ConditionCombination.And, criteria.Combination);
		Assert.Equal(2, criteria.Count);
		Assert.Equal(ConditionOperator.Equal, criteria.Find(field).Operator);
		Assert.Equal(value, Assert.IsType<Operand.ConstantOperand<string>>(criteria.Find(field).Value).Value);
		Assert.Equal(ConditionOperator.Equal, criteria.Find(nameof(IUser.Namespace)).Operator);

		if(@namespace == null)
			Assert.Null(criteria.Find(nameof(IUser.Namespace)).Value);
		else
			Assert.Equal(@namespace, Assert.IsType<Operand.ConstantOperand<string>>(criteria.Find(nameof(IUser.Namespace)).Value).Value);
	}

	[Theory]
	[InlineData("email:alice@example.invalid", "Email", "alice@example.invalid")]
	[InlineData("phone:+123456", "Phone", "+123456")]
	public void UserContactSelectorsRemainSupported(string identifier, string field, string value)
	{
		var criteria = Assert.IsType<ConditionCollection>(new TestUserService().Criteria(identifier));
		var condition = Assert.IsType<Condition>(Assert.Single(criteria));

		Assert.Equal(field, condition.Name);
		Assert.Equal(ConditionOperator.Equal, condition.Operator);
		Assert.Equal(value, Assert.IsType<Operand.ConstantOperand<string>>(condition.Value).Value);
	}

	[Theory]
	[InlineData("tenant-a:Alice", "Name", "Alice", "tenant-a")]
	[InlineData("tenant-a:email:alice@example.invalid", "Email", "alice@example.invalid", "tenant-a")]
	[InlineData("Alice", "Name", "Alice", null)]
	public void UserCriteriaRespectBusinessNamespaceMapping(string identifier, string field, string value, string tenant)
	{
		var criteria = Assert.IsType<ConditionCollection>(new TenantUserService().Criteria(identifier));

		Assert.Equal(value, Assert.IsType<Operand.ConstantOperand<string>>(criteria.Find(field).Value).Value);
		Assert.Null(criteria.Find(nameof(IUser.Namespace)));
		Assert.Equal(tenant == null ? 1 : 2, criteria.Count);

		if(tenant != null)
			Assert.Equal(tenant, Assert.IsType<Operand.ConstantOperand<string>>(criteria.Find("Tenant.TenantNo").Value).Value);
	}

	private sealed class TestUserService : UserServiceBase<TestUser>
	{
		protected override IDataAccess Accessor => throw new System.NotSupportedException();
		public ICondition Criteria(string identifier) => this.GetCriteria(new Identifier(typeof(IUser), identifier));
	}

	private sealed class TenantUserService : UserServiceBase<TestUser>
	{
		protected override IDataAccess Accessor => throw new System.NotSupportedException();
		public ICondition Criteria(string identifier) => this.GetCriteria(new Identifier(typeof(IUser), identifier));

		protected override ICondition GetCriteria(string identity, string @namespace) =>
			base.GetCriteria(identity, null) & (@namespace == null ? null : Condition.Equal("Tenant.TenantNo", @namespace));
	}

	private sealed class TestUser(uint id, string name) : IUser
	{
		public Identifier Identifier { get; set; } = new(typeof(IUser), id);
		public string Name { get; set; } = name;
		public string Email { get; set; }
		public string Phone { get; set; }
		public bool Enabled { get; set; } = true;
		public bool? Gender { get; set; }
		public string Avatar { get; set; }
		public string Nickname { get; set; }
		public string Namespace { get; set; }
		public string Description { get; set; }
	}
}
