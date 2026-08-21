using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(nameof(SchemaMappingCollection))]
public class SchemaParserTest : IDisposable
{
	private const string Namespace = "SchemaTests";
	private readonly SchemaParser _parser;

	public SchemaParserTest()
	{
		AddMappings();
		_parser = SchemaParser.Instance;
	}

	[Theory]
	[InlineData(nameof(Employee.FullName), MemberTypes.Property)]
	[InlineData(nameof(Employee.ComputedCode), MemberTypes.Field)]
	public void Parse_ExplicitUnmappedModelMember_CreatesIgnoredMember(string name, MemberTypes memberType)
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", $"Id,{name}", typeof(Employee)));
		var computed = schema.Members[name];

		Assert.True(computed.Ignored);
		Assert.Null(computed.Property);
		Assert.Equal(name, computed.Member.Name);
		Assert.Equal(memberType, computed.Member.MemberType);
	}

	[Fact]
	public void Parse_ExplicitUnmappedModelMember_IsCaseInsensitive()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "fullname", typeof(Employee)));
		var computed = Assert.Single(schema.Members);

		Assert.Equal("fullname", computed.Name);
		Assert.Equal(nameof(Employee.FullName), computed.Member.Name);
		Assert.True(computed.Ignored);
	}

	[Fact]
	public void Parse_NestedComputedMember_UsesNavigationModelScope()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Metric{DisplayAvatar}", typeof(Employee)));
		var computed = schema.Find("Metric.DisplayAvatar");

		Assert.NotNull(computed);
		Assert.True(computed.Ignored);
		Assert.Equal(typeof(Profile).GetProperty(nameof(Profile.DisplayAvatar)), computed.Member);
	}

	[Fact]
	public void Parse_NavigationShorthands_ProduceEquivalentTrees()
	{
		var bare = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Department", typeof(Employee)));
		var dotted = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Department.*", typeof(Employee)));
		var braced = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Department{*}", typeof(Employee)));

		Assert.Equal(braced.ToString(), bare.ToString());
		Assert.Equal(braced.ToString(), dotted.ToString());
		Assert.True(bare.Contains("Department.Id"));
		Assert.True(bare.Contains("Department.Name"));
		Assert.False(bare.Contains("Department.Manager"));
	}

	[Fact]
	public void Parse_DottedNestedExpression_MatchesBracedExpression()
	{
		const string dottedExpression = "*,User,Department.*,Department.Manager.Name,Department.Manager.FullName,Department.Manager.Gender,!Department.Manager.Secret";
		const string bracedExpression = "*,User{*},Department{*,Manager{Name,FullName,Gender,!Secret}}";
		var dotted = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", dottedExpression, typeof(Employee)));
		var braced = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", bracedExpression, typeof(Employee)));

		Assert.Equal(braced.ToString(), dotted.ToString());
		Assert.True(dotted.Contains("Department.Manager.Name"));
		Assert.True(dotted.Contains("Department.Manager.FullName"));
		Assert.True(dotted.Contains("Department.Manager.Gender"));
		Assert.False(dotted.Contains("Department.Manager.Secret"));
		Assert.Equal(dottedExpression, dotted.Text);
	}

	[Fact]
	public void Parse_DottedPath_IsCaseInsensitiveAndAllowsWhitespace()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", " department . MANAGER . name ", typeof(Employee)));

		Assert.True(schema.Contains("Department.Manager.Name"));
		Assert.Equal("Department{Manager{Name}}", schema.ToString());
	}

	[Fact]
	public void Parse_NavigationExclusions_RemoveEntireMember()
	{
		var named = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "User,!User", typeof(Employee)));
		var cleared = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "User{!}", typeof(Employee)));

		Assert.True(named.IsEmpty);
		Assert.True(cleared.IsEmpty);
		Assert.Equal(named.ToString(), cleared.ToString());
	}

	[Fact]
	public void Parse_ClearThenIncludeInNavigation_RetainsNavigation()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "User{!,Name}", typeof(Employee)));

		Assert.True(schema.Contains("User.Name"));
		Assert.False(schema.Contains("User.Id"));
		Assert.Equal("User{Name}", schema.ToString());
	}

	[Theory]
	[InlineData("Posts:10", "Posts:10{*}")]
	[InlineData("Posts(~Approved)", "Posts(~Approved){*}")]
	[InlineData("Posts(-Approved)", "Posts(~Approved){*}")]
	[InlineData("Posts(+Approved)", "Posts(Approved){*}")]
	[InlineData("Posts:10(~Approved)", "Posts:10(~Approved){*}")]
	public void Parse_TerminalNavigationModifier_ExpandsWildcard(string shorthand, string explicitWildcard)
	{
		var actual = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", shorthand, typeof(Employee)));
		var expected = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", explicitWildcard, typeof(Employee)));

		Assert.Equal(expected.ToString(), actual.ToString());
		Assert.True(actual.Contains("Posts.Id"));
		Assert.True(actual.Contains("Posts.Approved"));
	}

	[Fact]
	public void Parse_TerminalNavigationModifiers_ExpandWildcard()
	{
		var shorthand = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Posts:12(-Approved)", typeof(Employee)));
		var explicitWildcard = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Posts:12(~Approved){*}", typeof(Employee)));
		var member = shorthand.Members[nameof(Employee.Posts)];

		Assert.Equal(explicitWildcard.ToString(), shorthand.ToString());
		Assert.Equal(12, member.Limit);
		Assert.True(shorthand.Contains("Posts.Id"));
		Assert.True(shorthand.Contains("Posts.Approved"));
		Assert.Collection(member.Sortings, sorting =>
		{
			Assert.Equal(nameof(Post.Approved), sorting.Name);
			Assert.Equal(SortingMode.Descending, sorting.Mode);
		});

		var statement = new SelectStatement(shorthand.Entity);
		SelectStatementBuilder.GenerateSchema(new Aliaser(), statement, statement.Table, member);
		var slave = Assert.IsType<SelectStatement>(Assert.Single(statement.Slaves));
		Assert.True(slave.Paging.IsLimited(out var count, out var offset));
		Assert.Equal(12, count);
		Assert.Equal(0, offset);
		Assert.Contains(slave.Select.Members.Cast<FieldIdentifier>(), field => field.Token.Property.Name == nameof(Post.Approved));
		Assert.Contains(slave.OrderBy.Members, sorting => sorting.Field.Token.Property.Name == nameof(Post.Approved) && sorting.Mode == SortingMode.Descending);
	}

	[Theory]
	[InlineData("Id.Value")]
	[InlineData("!Misspelled")]
	[InlineData("!Department.Misspelled.Name")]
	[InlineData("!Department.Manager.Misspelled")]
	[InlineData("!Department.Manager.*")]
	[InlineData("Department.!Manager")]
	[InlineData("Posts:10.Manager")]
	[InlineData("Posts:10(~Approved).Manager")]
	[InlineData("Department.*:10")]
	[InlineData("User{!*}")]
	public void Parse_InvalidDottedOrExclusionMember_ThrowsSchemaArgument(string expression)
	{
		var exception = Assert.Throws<DataArgumentException>(() => _parser.Parse($"{Namespace}.Employee", expression, typeof(Employee)));
		Assert.Equal("$schema", exception.Name);
	}

	[Fact]
	public void Parse_DottedExclusionOfAbsentValidMember_IsNoOp()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Id,!Department.Manager.Secret", typeof(Employee)));

		Assert.Single(schema.Members);
		Assert.True(schema.Contains(nameof(Employee.Id)));
		Assert.False(schema.Contains(nameof(Employee.Department)));
	}

	[Fact]
	public void Parse_DottedLeafExclusion_PreservesEmptyParentsAndCanonicalTree()
	{
		const string expression = "Department.Manager.Name,!Department.Manager.Name";
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", expression, typeof(Employee)));
		var canonical = schema.ToString();

		Assert.True(schema.Contains("Department.Manager"));
		Assert.False(schema.Contains("Department.Manager.Name"));
		Assert.True(schema.Find("Department.Manager").Property.IsComplex);
		Assert.Equal("Department{Manager{}}", canonical);
		Assert.Equal(canonical, _parser.Parse($"{Namespace}.Employee", canonical, typeof(Employee)).ToString());
	}

	[Fact]
	public void Parse_DottedComputedMember_UsesNavigationModelScope()
	{
		const string expression = "ComputedProfile.DisplayAvatar";
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", expression, typeof(Employee)));
		var computed = schema.Find(expression);

		Assert.NotNull(computed);
		Assert.True(computed.Ignored);
		Assert.Equal(typeof(Profile).GetProperty(nameof(Profile.DisplayAvatar)), computed.Member);
		Assert.Equal(expression, schema.Text);
		Assert.Equal("ComputedProfile{DisplayAvatar}", schema.ToString());
	}

	[Fact]
	public void Parse_Wildcard_DoesNotAddUnmappedModelMembers()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "*", typeof(Employee)));

		Assert.True(schema.Contains(nameof(Employee.Id)));
		Assert.True(schema.Contains(nameof(Employee.FirstName)));
		Assert.False(schema.Contains(nameof(Employee.FullName)));
		Assert.False(schema.Contains(nameof(Employee.ComputedCode)));
		Assert.False(schema.Contains(nameof(Employee.ExplicitOnly)));
		Assert.False(schema.Find(nameof(Employee.FirstName)).Ignored);
	}

	[Fact]
	public void SelectBuilder_IgnoredMembers_DoNotEmitFields()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "FirstName,LastName,FullName", typeof(Employee)));
		var statement = new SelectStatement(schema.Entity);
		var aliaser = new Aliaser();

		foreach(var member in schema.Members)
			SelectStatementBuilder.GenerateSchema(aliaser, statement, statement.Table, member);

		var fields = statement.Select.Members.Cast<FieldIdentifier>().ToArray();
		Assert.DoesNotContain(fields, field => string.Equals(field.Name, nameof(Employee.FullName), StringComparison.OrdinalIgnoreCase));
		Assert.Equal([nameof(Employee.FirstName), nameof(Employee.LastName)], fields.Select(field => field.Token.Property.Name));
	}

	[Theory]
	[InlineData("Posts:12{*}", 12)]
	[InlineData("Posts:0{*}", 0)]
	[InlineData("Posts:*{*}", 0)]
	public void SelectBuilder_CollectionLimit_AppliesOnlyPositiveLimits(string expression, int expected)
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", expression, typeof(Employee)));
		var statement = new SelectStatement(schema.Entity);
		var member = schema.Members[nameof(Employee.Posts)];

		SelectStatementBuilder.GenerateSchema(new Aliaser(), statement, statement.Table, member);

		var slave = Assert.IsType<SelectStatement>(Assert.Single(statement.Slaves));
		Assert.Equal(expected, member.Limit);

		if(expected > 0)
		{
			Assert.NotNull(slave.Paging);
			Assert.True(slave.Paging.IsLimited(out var count, out var offset));
			Assert.Equal(expected, count);
			Assert.Equal(0, offset);
		}
		else
			Assert.Null(slave.Paging);
	}

	[Fact]
	public void SelectBuilder_NegativeCollectionLimit_IsUnlimited()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Posts:1{*}", typeof(Employee)));
		var statement = new SelectStatement(schema.Entity);
		var member = schema.Members[nameof(Employee.Posts)];
		var property = typeof(SchemaMemberBase).GetProperty(nameof(ISchemaMember.Limit));

		property.SetValue(member, -1);
		SelectStatementBuilder.GenerateSchema(new Aliaser(), statement, statement.Table, member);

		var slave = Assert.IsType<SelectStatement>(Assert.Single(statement.Slaves));
		Assert.Equal(-1, member.Limit);
		Assert.Null(slave.Paging);
	}

	[Fact]
	public void Parse_MappedMemberWinsOverUnrecognizedFallback()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", nameof(Employee.FirstName), typeof(Employee)));
		var member = schema.Members[nameof(Employee.FirstName)];

		Assert.False(member.Ignored);
		Assert.NotNull(member.Property);
	}

	[Fact]
	public void Parse_InheritedWildcard_UsesDerivedMemberAndIncludesBaseMembers()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "*", typeof(Employee)));

		Assert.True(schema.Contains(nameof(Employee.BaseCode)));
		Assert.Same(schema.Entity, schema.Members[nameof(Employee.FirstName)].Property.Entity);
		Assert.Equal(1, schema.Members.Count(member => string.Equals(member.Name, nameof(Employee.FirstName), StringComparison.OrdinalIgnoreCase)));
	}

	[Fact]
	public void Parse_DerivedParserHandlesUnrecognizedMember()
	{
		var parser = new CustomSchemaParser();
		var schema = Assert.IsType<Schema>(parser.Parse($"{Namespace}.Employee", "ProjectedName", typeof(Employee)));
		var member = schema.Members["ProjectedName"];

		Assert.True(member.Ignored);
		Assert.Equal(nameof(Employee.FullName), member.Member.Name);
	}

	[Fact]
	public void Parse_UnknownMember_ThrowsSchemaArgument()
	{
		var exception = Assert.Throws<DataArgumentException>(() => _parser.Parse($"{Namespace}.Employee", "Misspelled", typeof(Employee)));
		Assert.Equal("$schema", exception.Name);
		Assert.Contains("Misspelled", exception.Message);
	}

	[Fact]
	public void Parse_DefaultParserResolvesExplicitComputedMember()
	{
		var schema = Assert.IsType<Schema>(SchemaParser.Instance.Parse($"{Namespace}.Employee", nameof(Employee.FullName), typeof(Employee)));
		Assert.True(schema.Members[nameof(Employee.FullName)].Ignored);
	}

	[Theory]
	[InlineData("StaticValue")]
	[InlineData("Secret")]
	[InlineData("Item")]
	public void Parse_NonPublicInstanceModelMember_ThrowsSchemaArgument(string name)
	{
		var exception = Assert.Throws<DataArgumentException>(() => _parser.Parse($"{Namespace}.Employee", name, typeof(Employee)));
		Assert.Contains(name, exception.Message);
	}

	[Theory]
	[InlineData("*, Site{*}", "Site.Name")]
	[InlineData("*,Thread{*}", "Thread.Approved")]
	[InlineData("*, Message{*}", "Message.Id")]
	[InlineData("*,Post{Approved}", "Post.Approved")]
	[InlineData("*,Fields{*,Components{*}},Assets{*}", "Fields.Components.Name")]
	[InlineData("*,User{*,BranchMembers{BranchId,Branch{Name}},Members{RoleId,role{*}}}", "User.Members.Role.Name")]
	[InlineData("ModelId,EvaluationMode,FinalTime", "FinalTime")]
	[InlineData("WarnProjectId,WarnProjectName,,Post{Approved},", "Post.Approved")]
	[InlineData("*,Metric{*}", "Metric.Avatar")]
	public void Parse_DeduplicatedExternalProjectShapes_UsesOnlySyntheticMappings(string expression, string expectedPath)
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", expression, typeof(Employee)));
		Assert.True(schema.Contains(expectedPath));
	}

	[Fact]
	public void IncludeAndExclude_DeepCompatibilityPaths_MergeAndPrune()
	{
		var schema = Assert.IsType<Schema>(_parser.Parse($"{Namespace}.Employee", "Id,User{Name}", typeof(Employee)));

		schema.Include("User.Profile.Avatar");
		Assert.True(schema.Contains("User.Profile.Avatar"));
		Assert.True(schema.Contains("User.Name"));

		Assert.True(schema.Exclude("User.Profile.Avatar", out var removed));
		Assert.Equal("Avatar", removed.Name);
		Assert.False(schema.Contains("User.Profile"));
		Assert.True(schema.Contains("User.Name"));
	}

	public void Dispose()
	{
		Mapping.Entities.Remove($"{Namespace}.Employee");
		Mapping.Entities.Remove($"{Namespace}.BaseEmployee");
		Mapping.Entities.Remove($"{Namespace}.User");
		Mapping.Entities.Remove($"{Namespace}.Profile");
		Mapping.Entities.Remove($"{Namespace}.Post");
		Mapping.Entities.Remove($"{Namespace}.Field");
		Mapping.Entities.Remove($"{Namespace}.Component");
		Mapping.Entities.Remove($"{Namespace}.Asset");
		Mapping.Entities.Remove($"{Namespace}.BranchMember");
		Mapping.Entities.Remove($"{Namespace}.Branch");
		Mapping.Entities.Remove($"{Namespace}.Member");
		Mapping.Entities.Remove($"{Namespace}.Role");
		Mapping.Entities.Remove($"{Namespace}.Department");
		Mapping.Entities.Remove($"{Namespace}.Manager");
	}

	private static void AddMappings()
	{
		var profile = new DataEntity(Namespace, "Profile");
		profile.Properties.Simplex(nameof(Profile.Avatar), DataType.String, 100, true);

		var user = new DataEntity(Namespace, "User");
		user.Properties.Simplex(nameof(User.Id), DataType.Int64, false);
		user.Properties.Simplex(nameof(User.Name), DataType.String, 50, true);
		user.Properties.Complex(nameof(User.Profile), "Profile");
		user.Properties.Complex(nameof(User.BranchMembers), "BranchMember", false, DataAssociationMultiplicity.Many);
		user.Properties.Complex(nameof(User.Members), "Member", false, DataAssociationMultiplicity.Many);

		var post = new DataEntity(Namespace, "Post");
		post.Properties.Simplex(nameof(Post.Id), DataType.Int64, false);
		post.Properties.Simplex(nameof(Post.Approved), DataType.Boolean, false).Sortable = true;

		var component = new DataEntity(Namespace, "Component");
		component.Properties.Simplex(nameof(Component.Id), DataType.Int64, false);
		component.Properties.Simplex(nameof(Component.Name), DataType.String, 50, true);

		var field = new DataEntity(Namespace, "Field");
		field.Properties.Simplex(nameof(Field.Id), DataType.Int64, false);
		field.Properties.Simplex(nameof(Field.Name), DataType.String, 50, true);
		field.Properties.Complex(nameof(Field.Components), "Component", false, DataAssociationMultiplicity.Many);

		var asset = new DataEntity(Namespace, "Asset");
		asset.Properties.Simplex(nameof(Asset.Id), DataType.Int64, false);
		asset.Properties.Simplex(nameof(Asset.Name), DataType.String, 50, true);

		var branch = new DataEntity(Namespace, "Branch");
		branch.Properties.Simplex(nameof(Branch.Id), DataType.Int64, false);
		branch.Properties.Simplex(nameof(Branch.Name), DataType.String, 50, true);

		var branchMember = new DataEntity(Namespace, "BranchMember");
		branchMember.Properties.Simplex(nameof(BranchMember.BranchId), DataType.Int64, false);
		branchMember.Properties.Complex(nameof(BranchMember.Branch), "Branch");

		var role = new DataEntity(Namespace, "Role");
		role.Properties.Simplex(nameof(Role.Id), DataType.Int64, false);
		role.Properties.Simplex(nameof(Role.Name), DataType.String, 50, true);

		var member = new DataEntity(Namespace, "Member");
		member.Properties.Simplex(nameof(Member.RoleId), DataType.Int64, false);
		member.Properties.Complex(nameof(Member.Role), "Role");

		var manager = new DataEntity(Namespace, "Manager");
		manager.Properties.Simplex(nameof(Manager.Id), DataType.Int64, false);
		manager.Properties.Simplex(nameof(Manager.Name), DataType.String, 50, true);
		manager.Properties.Simplex(nameof(Manager.FullName), DataType.String, 100, true);
		manager.Properties.Simplex(nameof(Manager.Gender), DataType.String, 20, true);
		manager.Properties.Simplex(nameof(Manager.Secret), DataType.String, 100, true);

		var department = new DataEntity(Namespace, "Department");
		department.Properties.Simplex(nameof(Department.Id), DataType.Int64, false);
		department.Properties.Simplex(nameof(Department.Name), DataType.String, 50, true);
		department.Properties.Complex(nameof(Department.Manager), "Manager");

		var baseEmployee = new DataEntity(Namespace, "BaseEmployee");
		baseEmployee.Properties.Simplex(nameof(Employee.BaseCode), DataType.String, 50, true);
		baseEmployee.Properties.Simplex(nameof(Employee.FirstName), DataType.String, 50, true);

		var employee = new DataEntity(Namespace, "Employee", "BaseEmployee");
		employee.Properties.Simplex(nameof(Employee.Id), DataType.Int64, false);
		employee.Properties.Simplex(nameof(Employee.FirstName), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.LastName), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.ModelId), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.EvaluationMode), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.FinalTime), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.WarnProjectId), DataType.String, 50, true);
		employee.Properties.Simplex(nameof(Employee.WarnProjectName), DataType.String, 50, true);
		employee.Properties.Complex(nameof(Employee.User), "User");
		employee.Properties.Complex(nameof(Employee.Posts), "Post", false, DataAssociationMultiplicity.Many);
		employee.Properties.Complex(nameof(Employee.Site), "User");
		employee.Properties.Complex(nameof(Employee.Thread), "Post");
		employee.Properties.Complex(nameof(Employee.Message), "Post");
		employee.Properties.Complex(nameof(Employee.Post), "Post");
		employee.Properties.Complex(nameof(Employee.Metric), "Profile");
		employee.Properties.Complex(nameof(Employee.Department), "Department");
		employee.Properties.Complex(nameof(Employee.Fields), "Field", false, DataAssociationMultiplicity.Many);
		employee.Properties.Complex(nameof(Employee.Assets), "Asset", false, DataAssociationMultiplicity.Many);

		Mapping.Entities.Add(baseEmployee);
		Mapping.Entities.Add(profile);
		Mapping.Entities.Add(user);
		Mapping.Entities.Add(post);
		Mapping.Entities.Add(component);
		Mapping.Entities.Add(field);
		Mapping.Entities.Add(asset);
		Mapping.Entities.Add(branch);
		Mapping.Entities.Add(branchMember);
		Mapping.Entities.Add(role);
		Mapping.Entities.Add(member);
		Mapping.Entities.Add(manager);
		Mapping.Entities.Add(department);
		Mapping.Entities.Add(employee);
	}

	private sealed class CustomSchemaParser : SchemaParser
	{
		protected override MemberInfo OnUnrecognized(IDataEntity entity, Type modelType, ISchemaMember parent, string name)
		{
			if(modelType == typeof(Employee) && name == "ProjectedName")
				return typeof(Employee).GetProperty(nameof(Employee.FullName));

			return base.OnUnrecognized(entity, modelType, parent, name);
		}
	}

	private sealed class Employee
	{
		public long Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string BaseCode { get; set; }
		public string ModelId { get; set; }
		public string EvaluationMode { get; set; }
		public string FinalTime { get; set; }
		public string WarnProjectId { get; set; }
		public string WarnProjectName { get; set; }
		public User User { get; set; }
		public ICollection<Post> Posts { get; set; }
		public User Site { get; set; }
		public Post Thread { get; set; }
		public Post Message { get; set; }
		public Post Post { get; set; }
		public Profile Metric { get; set; }
		public Profile ComputedProfile => this.Metric;
		public Department Department { get; set; }
		public ICollection<Field> Fields { get; set; }
		public ICollection<Asset> Assets { get; set; }
		public string FullName => $"{this.FirstName} {this.LastName}";
		public string ComputedCode = string.Empty;
		public string Initials => $"{this.FirstName?[0]}{this.LastName?[0]}";
		public string UserLabel => this.User?.Name;
		public string ExplicitOnly => this.FullName.ToUpperInvariant();
		public string Alias => this.FullName;
		public string CycleA => this.CycleB;
		public string CycleB => this.CycleA;
		public static string StaticValue { get; set; }
		private string Secret { get; set; }
		public string this[int index] => index.ToString();
	}

	private sealed class User
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public Profile Profile { get; set; }
		public ICollection<BranchMember> BranchMembers { get; set; }
		public ICollection<Member> Members { get; set; }
	}

	private sealed class Profile
	{
		public string Avatar { get; set; }
		public string DisplayAvatar => this.Avatar?.ToUpperInvariant();
	}

	private sealed class Post
	{
		public long Id { get; set; }
		public bool Approved { get; set; }
	}

	private sealed class Field
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public ICollection<Component> Components { get; set; }
	}

	private sealed class Component
	{
		public long Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class Asset
	{
		public long Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class BranchMember
	{
		public long BranchId { get; set; }
		public Branch Branch { get; set; }
	}

	private sealed class Branch
	{
		public long Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class Member
	{
		public long RoleId { get; set; }
		public Role Role { get; set; }
	}

	private sealed class Role
	{
		public long Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class Department
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public Manager Manager { get; set; }
	}

	private sealed class Manager
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public string FullName { get; set; }
		public string Gender { get; set; }
		public string Secret { get; set; }
	}
}

[CollectionDefinition(nameof(SchemaMappingCollection), DisableParallelization = true)]
public sealed class SchemaMappingCollection;
