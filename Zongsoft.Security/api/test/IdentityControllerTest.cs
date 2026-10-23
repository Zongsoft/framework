using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Web.Controllers;

namespace Zongsoft.Security.Web.Tests;

public sealed class IdentityControllerTest : IDisposable
{
	private readonly IUserService _users = Authentication.Servicer.Users;
	private readonly IRoleService _roles = Authentication.Servicer.Roles;
	private readonly IMemberService _members = Authentication.Servicer.Members;
	private readonly IPrivilegeService _privileges = Authorization.Servicer.Privileges;

	public void Dispose()
	{
		Authentication.Servicer.Users = _users;
		Authentication.Servicer.Roles = _roles;
		Authentication.Servicer.Members = _members;
		Authorization.Servicer.Privileges = _privileges;
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Get_Identifier_ForwardsSchemaAndMapsMissingIdentity(bool role, bool found)
	{
		using var cancellation = new CancellationTokenSource();
		object identity = role ? new TestRole { Name = "Operators" } : new TestUser { Name = "Alice" };
		var calls = 0;
		this.SetIdentityServices((method, args) =>
		{
			calls++;
			Assert.Equal("GetAsync", method.Name);
			Assert.Equal(Identity(role, "42"), args[0]);
			Assert.Equal("Name,Enabled", args[1]);
			Assert.Equal(cancellation.Token, args[2]);
			return role ? new ValueTask<IRole>(found ? (IRole)identity : null) : new ValueTask<IUser>(found ? (IUser)identity : null);
		});
		var controller = Controller(role);
		controller.Request.Headers[Zongsoft.Web.Http.Headers.DataSchema] = "Name,Enabled";
		var result = role ? await ((RoleController)controller).Get("42", cancellation: cancellation.Token) : await ((UserController)controller).Get("42", cancellation: cancellation.Token);

		if(found)
			Assert.Same(identity, Assert.IsType<OkObjectResult>(result).Value);
		else
			Assert.IsType<NoContentResult>(result);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Get_Collection_ForwardsKeywordAndPagination(bool role, bool defaultPaging)
	{
		using var cancellation = new CancellationTokenSource();
		var page = defaultPaging ? null : new Paging(3, 7) { Total = 30 };
		object sequence = role ? TestContext.Sequence<IRole>(new TestRole()) : TestContext.Sequence<IUser>(new TestUser());
		Paging supplied = null;
		this.SetIdentityServices((method, args) =>
		{
			Assert.Equal("FindAsync", method.Name);
			Assert.Equal("Alice", args[0]);
			Assert.Equal("Name", args[1]);
			supplied = Assert.IsAssignableFrom<Paging>(args[2]);
			Assert.Equal(cancellation.Token, args[3]);
			return sequence;
		});
		var controller = Controller(role);
		controller.Request.Headers[Zongsoft.Web.Http.Headers.DataSchema] = "Name";
		var result = role ? await ((RoleController)controller).Get(null, "Alice", page, cancellation.Token) : await ((UserController)controller).Get(null, "Alice", page, cancellation.Token);
		Assert.Same(sequence, Assert.IsType<OkObjectResult>(result).Value);

		if(defaultPaging)
		{
			Assert.Equal(1, supplied.Index);
			Assert.Equal(20, supplied.Size);
		}
		else
		{
			Assert.Same(page, supplied);
			Assert.Equal(page.ToString(), controller.Response.Headers[Zongsoft.Web.Http.Headers.Pagination].ToString());
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Create_JsonModel_MapsCreatedRouteAndConflict(bool role, bool success)
	{
		using var cancellation = new CancellationTokenSource();
		object supplied = null;
		this.SetIdentityServices((method, args) =>
		{
			Assert.Equal("CreateAsync", method.Name);
			supplied = args[0];
			Assert.Equal("Alice", role ? ((IRole)supplied).Name : ((IUser)supplied).Name);
			Assert.Equal(cancellation.Token, args[1]);
			((IIdentifiable)supplied).Identifier = Identity(role, "created");
			return new ValueTask<bool>(success);
		});
		var controller = Controller(role, "{\"Name\":\"Alice\",\"Enabled\":true}", "application/json");
		var result = role ? await ((RoleController)controller).Create(cancellation.Token) : await ((UserController)controller).Create(cancellation.Token);

		if(success)
		{
			var created = Assert.IsType<CreatedAtActionResult>(result);
			Assert.Equal("Get", created.ActionName);
			Assert.Equal("created", created.RouteValues["id"]);
			Assert.Same(supplied, created.Value);
		}
		else
			Assert.IsType<ConflictResult>(result);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Update_JsonModel_UsesRouteIdentifier(bool role, bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		this.SetIdentityServices((method, args) =>
		{
			calls++;
			Assert.Equal("UpdateAsync", method.Name);
			Assert.Equal(Identity(role, "route-id"), ((IIdentifiable)args[0]).Identifier);
			Assert.Equal("New name", role ? ((IRole)args[0]).Name : ((IUser)args[0]).Name);
			Assert.Equal(cancellation.Token, args[1]);
			return new ValueTask<bool>(success);
		});
		var controller = Controller(role, "{\"Name\":\"New name\"}", "application/json");
		var result = role ? await ((RoleController)controller).Update("route-id", cancellation.Token) : await ((UserController)controller).Update("route-id", cancellation.Token);
		AssertMutation(result, success);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ModelRequests_UnsupportedServiceAndNullModel_ReturnExplicitErrors(bool role)
	{
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>(Unexpected);
		Authentication.Servicer.Roles = ServiceProxy.Create<IRoleService>(Unexpected);
		var unsupported = Controller(role, "{}", "application/json");
		var result = role ? await ((RoleController)unsupported).Create() : await ((UserController)unsupported).Create();
		Assert.Equal(StatusCodes.Status501NotImplemented, Assert.IsType<StatusCodeResult>(result).StatusCode);
		this.SetIdentityServices(Unexpected);
		var empty = Controller(role, "null", "application/json");
		Assert.IsType<BadRequestResult>(role ? await ((RoleController)empty).Create() : await ((UserController)empty).Create());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Delete_Batch_ParsesIdentifiersAndMapsAffectedCount(bool role, bool deleted)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		this.SetIdentityServices((method, args) =>
		{
			calls++;
			Assert.Equal("DeleteAsync", method.Name);
			Assert.Equal(new[] { 11u, 22u, 33u }, Assert.IsAssignableFrom<IEnumerable<Identifier>>(args[0]).Select(id => (uint)id.Value));
			Assert.All((IEnumerable<Identifier>)args[0], id => Assert.Equal(role ? typeof(IRole) : typeof(IUser), id.Type));
			Assert.Equal(cancellation.Token, args[1]);
			return new ValueTask<int>(deleted ? 3 : 0);
		});
		var controller = Controller(role, " 11,invalid;22; 33 ");
		var result = role ? await ((RoleController)controller).Delete(cancellation.Token) : await ((UserController)controller).Delete(cancellation.Token);

		if(deleted)
			Assert.Equal("3", Assert.IsType<ContentResult>(result).Content);
		else
			Assert.IsType<NotFoundResult>(result);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false, " ")]
	[InlineData(false, "bad,-1")]
	[InlineData(true, " ")]
	[InlineData(true, "bad,-1")]
	public async Task Delete_BatchWithoutValidIds_DoesNotCallService(bool role, string body)
	{
		this.SetIdentityServices(Unexpected);
		var controller = Controller(role, body);
		Assert.IsType<BadRequestResult>(role ? await ((RoleController)controller).Delete() : await ((UserController)controller).Delete());
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(true, false)]
	public async Task IdentityMutations_ForwardIdentifierAndMapOutcome(bool role, bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = new List<string>();
		this.SetIdentityServices((method, args) =>
		{
			calls.Add(method.Name);
			Assert.Equal(Identity(role, "42"), args[0]);
			Assert.Equal(cancellation.Token, args[^1]);

			if(method.Name == "RenameAsync")
				Assert.Equal("New name", args[1]);
			return new ValueTask<bool>(success);
		});
		var controller = Controller(role, "New name");

		if(role)
		{
			var target = (RoleController)controller;
			AssertMutation(await target.Delete("42", cancellation.Token), success);
			AssertMutation(await target.EnableAsync("42", cancellation.Token), success);
			AssertMutation(await target.DisableAsync("42", cancellation.Token), success);
			AssertMutation(await target.Exists("42", cancellation.Token), success);
			AssertMutation(await target.Rename("42", cancellation.Token), success);
		}
		else
		{
			var target = (UserController)controller;
			AssertMutation(await target.Delete("42", cancellation.Token), success);
			AssertMutation(await target.EnableAsync("42", cancellation.Token), success);
			AssertMutation(await target.DisableAsync("42", cancellation.Token), success);
			AssertMutation(await target.Exists("42", cancellation.Token), success);
			AssertMutation(await target.Rename("42", cancellation.Token), success);
		}

		Assert.Equal(new[] { "DeleteAsync", "EnableAsync", "DisableAsync", "ExistsAsync", "RenameAsync" }, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IdentityMutations_MissingInput_DoesNotCallService(bool role)
	{
		this.SetIdentityServices(Unexpected);
		if(role)
		{
			var controller = TestContext.Attach(new RoleController(), " ");
			Assert.IsType<BadRequestResult>(await controller.Delete((string)null));
			Assert.IsType<BadRequestResult>(await controller.Update(null));
			Assert.IsType<BadRequestResult>(await controller.EnableAsync(null));
			Assert.IsType<BadRequestResult>(await controller.DisableAsync(null));
			Assert.IsType<BadRequestResult>(await controller.Exists(null));
			Assert.IsType<BadRequestResult>(await controller.Rename(null));
			Assert.IsType<BadRequestResult>(await controller.Rename("42"));
		}
		else
		{
			var controller = TestContext.Attach(new UserController(), " ");
			Assert.IsType<BadRequestResult>(await controller.Delete((string)null));
			Assert.IsType<BadRequestResult>(await controller.Update(null));
			Assert.IsType<BadRequestResult>(await controller.EnableAsync(null));
			Assert.IsType<BadRequestResult>(await controller.DisableAsync(null));
			Assert.IsType<BadRequestResult>(await controller.Exists(null));
			Assert.IsType<BadRequestResult>(await controller.Rename("42"));
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Contact_Verification_ForwardsRequestParametersAndMapsToken(bool phone, bool issued)
	{
		using var cancellation = new CancellationTokenSource();
		var contact = phone ? "13800000000" : "alice@example.invalid";
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			Assert.Equal(phone ? "SetPhoneAsync" : "SetEmailAsync", method.Name);
			Assert.Equal(Identity(false, "42"), args[0]);
			Assert.Equal(contact, args[1]);
			var parameters = Assert.IsType<Parameters>(args[2]);
			Assert.Equal("tenant", parameters["namespace"]);
			Assert.Equal("sms", parameters["Channel"]);
			Assert.Equal(cancellation.Token, args[3]);
			return new ValueTask<string>(issued ? "verification-ticket" : null);
		});
		var controller = TestContext.Attach(new UserController(), contact);
		controller.Request.QueryString = new QueryString("?namespace=tenant");
		controller.Request.Headers["X-Channel"] = "sms";
		var result = phone ? await controller.SetPhoneVerify("42", cancellation.Token) : await controller.SetEmailVerify("42", cancellation.Token);

		if(issued)
			Assert.Equal("verification-ticket", Assert.IsType<ContentResult>(result).Content);
		else
			Assert.IsType<NotFoundResult>(result);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Contact_UpdateAndConfirm_ForwardValuesAndMapOutcome(bool phone, bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			Assert.Equal(phone ? "SetPhoneAsync" : "SetEmailAsync", method.Name);
			Assert.Equal(cancellation.Token, args[2]);

			if(calls++ == 0)
			{
				Assert.Equal(Identity(false, "42"), args[0]);
				Assert.Equal("contact-value", args[1]);
			}
			else
			{
				Assert.Equal("ticket", args[0]);
				Assert.Equal("secret", args[1]);
			}

			return new ValueTask<bool>(success);
		});
		var controller = TestContext.Attach(new UserController(), "contact-value");
		AssertMutation(phone ? await controller.SetPhone("42", cancellation.Token) : await controller.SetEmail("42", cancellation.Token), success);
		AssertMutation(phone ? await controller.SetPhone("ticket", "secret", cancellation.Token) : await controller.SetEmail("ticket", "secret", cancellation.Token), success);
		Assert.Equal(2, calls);
	}

	[Fact]
	public async Task Contact_MissingValues_DoesNotCallService()
	{
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>(Unexpected);
		var controller = TestContext.Attach(new UserController(), " ");
		Assert.IsType<BadRequestResult>(await controller.SetEmail("42"));
		Assert.IsType<BadRequestResult>(await controller.SetPhone("42"));
		Assert.IsType<BadRequestResult>(await controller.SetEmailVerify("42"));
		Assert.IsType<BadRequestResult>(await controller.SetPhoneVerify("42"));
		Assert.IsType<BadRequestResult>(await controller.SetEmail(null, "secret"));
		Assert.IsType<BadRequestResult>(await controller.SetPhone("ticket", null));
	}

	[Theory]
	[InlineData("alice", "alice", null)]
	[InlineData("tenant:alice", "alice", "tenant")]
	[InlineData("tenant:alice:mobile", "alice:mobile", "tenant")]
	[InlineData(":alice", "alice", "")]
	public async Task ForgetPassword_QualifiedIdentity_SplitsOnlyFirstColon(string identifier, string identity, string scope)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			calls++;
			Assert.Equal("ForgetPasswordAsync", method.Name);
			Assert.Equal(identity, args[0]);
			Assert.Equal(scope, args[1]);
			Assert.Equal("email", Assert.IsType<Parameters>(args[2])["channel"]);
			Assert.Equal(cancellation.Token, args[3]);
			return new ValueTask<string>("reset-ticket");
		});
		var controller = TestContext.Attach(new UserController.PasswordController());
		controller.Request.QueryString = new QueryString("?channel=email");
		Assert.Equal("reset-ticket", Assert.IsType<ContentResult>(await controller.ForgetAsync(identifier, cancellation.Token)).Content);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(5)]
	[InlineData(6)]
	public async Task ResetPassword_Answers_EnforcesThreeToFiveAndForwardsPayload(int answerCount)
	{
		using var cancellation = new CancellationTokenSource();
		var answers = answerCount == 0 ? null : System.Linq.Enumerable.Range(1, answerCount).Select(i => "answer-" + i).ToArray();
		var calls = 0;
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			calls++;
			Assert.Equal("ResetPasswordAsync", method.Name);
			Assert.Equal("alice", args[0]);
			Assert.Equal("tenant", args[1]);
			Assert.Same(answers, args[2]);
			Assert.Equal("new-test-password", args[3]);
			Assert.Equal(cancellation.Token, args[4]);
			return new ValueTask<bool>(true);
		});
		var controller = TestContext.Attach(new UserController.PasswordController());
		var result = await controller.ResetAsync("tenant:alice", new UserController.PasswordController.PasswordResetModel { Answers = answers, Password = "new-test-password" }, cancellation.Token);

		if(answerCount is >= 3 and <= 5)
			Assert.IsType<NoContentResult>(result);
		else
			Assert.IsType<BadRequestResult>(result);
		Assert.Equal(answerCount is >= 3 and <= 5 ? 1 : 0, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Password_HasChangeAndTokenReset_ForwardSecretsAndMapOutcome(bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = new List<string>();
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			calls.Add(method.Name);
			Assert.Equal(cancellation.Token, args[^1]);

			if(method.Name == "ResetPasswordAsync")
				Assert.Equal(new object[] { "ticket", "secret", "new-test-password", cancellation.Token }, args);
			else
			{
				Assert.Equal(Identity(false, "42"), args[0]);
				if(method.Name == "ChangePasswordAsync")
				{
					Assert.Equal("old-test-password", args[1]);
					Assert.Equal("new-test-password", args[2]);
				}
			}

			return new ValueTask<bool>(success);
		});
		var controller = TestContext.Attach(new UserController.PasswordController(), "new-test-password");
		var has = await controller.HasAsync("42", cancellation.Token);

		if(success)
			Assert.Equal("Yes!", Assert.IsType<ContentResult>(has).Content);
		else
			Assert.IsType<NoContentResult>(has);
		AssertMutation(await controller.ChangeAsync("42", new UserController.PasswordController.PasswordChangeModel { OldPassword = "old-test-password", NewPassword = "new-test-password" }, cancellation.Token), success);
		AssertMutation(await controller.ResetAsync("ticket", "secret", cancellation.Token), success);
		Assert.Equal(new[] { "HasPasswordAsync", "ChangePasswordAsync", "ResetPasswordAsync" }, calls);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(2)]
	public async Task PasswordQuestions_MapsMissingEmptyAndQuestionList(int count)
	{
		using var cancellation = new CancellationTokenSource();
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			Assert.Equal("GetPasswordQuestionsAsync", method.Name);
			Assert.Equal("alice", args[0]);
			Assert.Equal("tenant", args[1]);
			Assert.Equal(cancellation.Token, args[2]);
			return new ValueTask<string[]>(count < 0 ? null : count == 0 ? [] : ["first?", "second?"]);
		});
		var result = await TestContext.Attach(new UserController.PasswordController()).GetQuestionsAsync("tenant:alice", cancellation.Token);

		if(count < 0)
			Assert.IsType<NotFoundResult>(result);
		else if(count == 0)
			Assert.IsType<NoContentResult>(result);
		else
			Assert.Equal("first?\nsecond?", Assert.IsType<ContentResult>(result).Content);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("tenant:")]
	public async Task Password_EmptyIdentity_DoesNotCallService(string identifier)
	{
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>(Unexpected);
		var controller = TestContext.Attach(new UserController.PasswordController());
		Assert.IsType<BadRequestResult>(await controller.ForgetAsync(identifier));
		Assert.IsType<BadRequestResult>(await controller.GetQuestionsAsync(identifier));
		Assert.IsType<BadRequestResult>(await controller.ResetAsync(identifier, new UserController.PasswordController.PasswordResetModel { Answers = ["one", "two", "three"] }));
	}

	[Fact]
	public async Task PasswordQuestions_MismatchedAnswers_RejectsBeforeService()
	{
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>(Unexpected);
		var controller = TestContext.Attach(new UserController.PasswordController());
		Assert.IsType<BadRequestResult>(await controller.SetQuestionsAndAnswers("42", new UserController.PasswordController.QuestionsAndAnswersModel { Questions = ["one", "two"], Answers = ["answer"] }));
		Assert.IsType<BadRequestResult>(await controller.HasAsync(null));
		Assert.IsType<BadRequestResult>(await controller.ResetAsync(null, "secret"));
		Assert.IsType<BadRequestResult>(await controller.ResetAsync("ticket", (string)null));
		Assert.IsType<BadRequestResult>(await controller.SetQuestionsAndAnswers(null, default));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PasswordQuestions_MatchingAnswers_ForwardsAllValues(bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var questions = new[] { "first?", "second?", "third?" };
		var answers = new[] { "one", "two", "three" };
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			Assert.Equal("SetPasswordQuestionsAndAnswersAsync", method.Name);
			Assert.Equal(Identity(false, "42"), args[0]);
			Assert.Equal("test-password", args[1]);
			Assert.Same(questions, args[2]);
			Assert.Same(answers, args[3]);
			Assert.Equal(cancellation.Token, args[4]);
			return new ValueTask<bool>(success);
		});
		var result = await TestContext.Attach(new UserController.PasswordController()).SetQuestionsAndAnswers("42", new UserController.PasswordController.QuestionsAndAnswersModel { Password = "test-password", Questions = questions, Answers = answers }, cancellation.Token);
		AssertMutation(result, success);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ParentRoles_ParseListAndPreserveMemberType(bool role, bool changed)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			calls++;
			Assert.Equal(role ? Member.Role("42") : Member.User("42"), args[0]);
			Assert.Equal(cancellation.Token, args[^1]);

			if(method.Name == "SetParentAsync")
			{
				Assert.Equal(Identity(true, "7"), args[1]);
				return new ValueTask<bool>(changed);
			}

			Assert.Equal("SetParentsAsync", method.Name);
			Assert.Equal(new[] { Identity(true, 7u), Identity(true, 8u) }, Assert.IsAssignableFrom<IEnumerable<Identifier>>(args[1]));
			return new ValueTask<int>(changed ? 2 : 0);
		});
		IActionResult single;
		IActionResult multiple;

		if(role)
		{
			var controller = TestContext.Attach(new RoleController.MemberController(), "7,bad;8");
			single = await controller.SetParent("42", "7", cancellation.Token);
			multiple = await controller.SetParents("42", cancellation.Token);
		}
		else
		{
			var controller = TestContext.Attach(new UserController.MemberController(), "7,bad;8");
			single = await controller.SetParent("42", "7", cancellation.Token);
			multiple = await controller.SetParents("42", cancellation.Token);
		}

		AssertMutation(single, changed);
		if(changed)
			Assert.Equal("2", Assert.IsType<ContentResult>(multiple).Content);
		else
			Assert.IsType<NoContentResult>(multiple);
		Assert.Equal(2, calls);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void ParentRoles_AncestorDepth_SelectsCorrectServiceOverload(bool role, bool limited)
	{
		using var cancellation = new CancellationTokenSource();
		object expected = limited ? TestContext.Sequence<ICollection<Identifier>>(new[] { Identity(true, "7") }) : TestContext.Sequence<IRole>(new TestRole());
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			Assert.Equal("GetAncestorsAsync", method.Name);
			Assert.Equal(role ? Member.Role("42") : Member.User("42"), args[0]);
			Assert.Equal(limited ? 3 : 2, args.Length);
			Assert.Equal(cancellation.Token, args[^1]);

			if(limited)
				Assert.Equal(2, args[1]);
			return expected;
		});
		var result = role ? TestContext.Attach(new RoleController.MemberController()).GetAncestors("42", limited ? 2 : null, cancellation.Token) : TestContext.Attach(new UserController.MemberController()).GetAncestors("42", limited ? 2 : null, cancellation.Token);
		Assert.Same(expected, Assert.IsType<OkObjectResult>(result).Value);
	}

	[Theory]
	[InlineData("User:42", false)]
	[InlineData("User:42", true)]
	[InlineData("7@Role", false)]
	[InlineData("7@Role", true)]
	public async Task Members_Single_ParsesBothFormsAndMapsMutation(string text, bool changed)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = new List<string>();
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			calls.Add(method.Name);
			Assert.Equal(Identity(true, "9"), args[0]);
			Assert.Equal(text.StartsWith("User") ? Member.User("42") : Member.Role("7"), args[1]);
			Assert.Equal(cancellation.Token, args[2]);
			return new ValueTask<bool>(changed);
		});
		var controller = TestContext.Attach(new RoleController.MemberController());
		var result = await controller.Set("9", text, cancellation.Token);

		if(changed)
		{
			var created = Assert.IsType<CreatedAtActionResult>(result);
			Assert.Equal("Get", created.ActionName);
			Assert.Equal("9", created.RouteValues["id"]);
		}
		else
			Assert.IsType<NoContentResult>(result);

		AssertMutation(await controller.Remove("9", text, cancellation.Token), changed);
		Assert.Equal(new[] { "SetAsync", "RemoveAsync" }, calls);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Members_Batch_ParsesTextAndJsonAndForwardsReset(bool json, bool reset)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService<TestRole, TestMember>>((method, args) =>
		{
			calls++;
			Assert.Equal("SetAsync", method.Name);
			Assert.Equal(Identity(true, "9"), args[0]);
			Assert.Equal(new[] { Member.User("42"), Member.Role("7") }, Assert.IsAssignableFrom<IEnumerable<Member>>(args[1]));
			Assert.Equal(reset, args[2]);
			Assert.Equal(cancellation.Token, args[3]);
			return new ValueTask<int>(2);
		});
		var body = json ? "[{\"Id\":\"42\",\"MemberType\":0,\"Embedded\":true},{\"Id\":\"7\",\"MemberType\":1},{\"MemberType\":0}]" : "User:42,invalid;7@Role";
		var controller = TestContext.Attach(new RoleController.MemberController(), body, json ? "application/json" : "text/plain");
		var result = Assert.IsType<CreatedAtActionResult>(await controller.Set("9", reset, cancellation.Token));
		Assert.Equal("Get", result.ActionName);
		Assert.Equal("9", result.RouteValues["id"]);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(null, 0)]
	[InlineData("User:42;7@Role", 2)]
	public async Task Members_RemoveBatch_EmptyBodyMeansClear(string body, int removed)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			calls++;
			Assert.Equal("RemoveAsync", method.Name);
			Assert.Equal(Identity(true, "9"), args[0]);

			if(body == null)
				Assert.Null(args[1]);
			else
				Assert.Equal(new[] { Member.User("42"), Member.Role("7") }, Assert.IsAssignableFrom<IEnumerable<Member>>(args[1]));
			Assert.Equal(cancellation.Token, args[2]);
			return new ValueTask<int>(removed);
		});
		var result = await TestContext.Attach(new RoleController.MemberController(), body).Remove("9", cancellation.Token);

		if(removed > 0)
			Assert.Equal("2", Assert.IsType<ContentResult>(result).Content);
		else
			Assert.IsType<NoContentResult>(result);
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Members_InvalidRequests_DoNotCallService()
	{
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>(Unexpected);
		var role = TestContext.Attach(new RoleController.MemberController(), "invalid");
		Assert.IsType<BadRequestResult>(await role.Set(null, "User:42"));
		Assert.IsType<BadRequestResult>(await role.Set("9", "unknown:42"));
		Assert.IsType<BadRequestResult>(await role.Set("9"));
		Assert.IsType<BadRequestResult>(await role.Remove("9", "42"));
		Assert.IsType<BadRequestResult>(await role.Remove((string)null));
		Assert.IsType<BadRequestResult>(await role.SetParent(null, "7"));
		Assert.IsType<BadRequestResult>(await role.SetParents(null, default));
		var user = TestContext.Attach(new UserController.MemberController(), " ");
		Assert.IsType<BadRequestResult>(await user.SetParent("42", null));
		Assert.IsType<BadRequestResult>(await user.SetParents(null, default));
		Assert.IsType<BadRequestResult>(await user.SetParents("42", default));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Privileges_Get_ForwardsSubjectAndRequestParameters(bool role)
	{
		using var cancellation = new CancellationTokenSource();
		var sequence = TestContext.Sequence<IPrivilege>(new TestPrivilege { Name = "Orders.Read" });
		var calls = 0;
		Authorization.Servicer.Privileges = ServiceProxy.Create<IPrivilegeService>((method, args) =>
		{
			calls++;
			Assert.Equal("GetPrivilegesAsync", method.Name);
			Assert.Equal(Identity(role, "42"), args[0]);
			Assert.Equal("tenant", Assert.IsType<Parameters>(args[1])["namespace"]);
			Assert.Equal(cancellation.Token, args[2]);
			return sequence;
		});
		IAsyncEnumerable<IPrivilege> result;

		if(role)
		{
			var controller = TestContext.Attach(new RoleController.PrivilegeController());
			controller.Request.QueryString = new QueryString("?namespace=tenant");
			result = controller.Get("42", cancellation.Token);
		}
		else
		{
			var controller = TestContext.Attach(new UserController.PrivilegeController());
			controller.Request.QueryString = new QueryString("?namespace=tenant");
			result = controller.Get("42", cancellation.Token);
		}

		Assert.Same(sequence, result);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Privileges_Set_DeserializesItemsAndForwardsReset(bool role, bool changed)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		Authorization.Servicer.Privileges = ServiceProxy.Create<IPrivilegeService<TestPrivilege>>((method, args) =>
		{
			calls++;
			Assert.Equal("SetPrivilegesAsync", method.Name);
			Assert.Equal(Identity(role, "42"), args[0]);
			Assert.Equal(new[] { "Orders.Read", "Orders.Write" }, Assert.IsAssignableFrom<IEnumerable<IPrivilege>>(args[1]).Select(p => p.Name));
			Assert.True(Assert.IsType<bool>(args[2]));
			Assert.Equal("tenant", Assert.IsType<Parameters>(args[3])["namespace"]);
			Assert.Equal(cancellation.Token, args[4]);
			return new ValueTask<int>(changed ? 2 : 0);
		});
		const string BODY = "[{\"Name\":\"Orders.Read\"},{\"Name\":\"Orders.Write\"}]";
		IActionResult result;

		if(role)
		{
			var controller = TestContext.Attach(new RoleController.PrivilegeController(), BODY, "application/json");
			controller.Request.QueryString = new QueryString("?namespace=tenant");
			result = await controller.Set("42", true, cancellation.Token);
		}
		else
		{
			var controller = TestContext.Attach(new UserController.PrivilegeController(), BODY, "application/json");
			controller.Request.QueryString = new QueryString("?namespace=tenant");
			result = await controller.Set("42", true, cancellation.Token);
		}

		if(changed)
			Assert.Equal("2", Assert.IsType<ContentResult>(result).Content);
		else
			Assert.IsType<NoContentResult>(result);
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Privileges_UnsupportedModelsAndFiltering_Return501(bool role)
	{
		Authorization.Servicer.Privileges = ServiceProxy.Create<IPrivilegeService>((method, _) => method.Name == "get_Filtering" ? null : throw new InvalidOperationException());
		if(role)
		{
			var controller = TestContext.Attach(new RoleController.PrivilegeController(), "[]", "application/json");
			Assert.IsType<BadRequestResult>(await controller.Set(null));
			Assert.IsType<BadRequestResult>(await controller.SetFiltering(null));
			Assert.Equal(501, Assert.IsType<StatusCodeResult>(await controller.Set("42")).StatusCode);
			Assert.Equal(501, Assert.IsType<StatusCodeResult>(await controller.SetFiltering("42")).StatusCode);
			Assert.Equal(501, Assert.Throws<BadHttpRequestException>(() => controller.GetFiltering("42")).StatusCode);
		}
		else
		{
			var controller = TestContext.Attach(new UserController.PrivilegeController(), "[]", "application/json");
			Assert.IsType<BadRequestResult>(await controller.Set(null));
			Assert.IsType<BadRequestResult>(await controller.SetFiltering(null));
			Assert.Equal(501, Assert.IsType<StatusCodeResult>(await controller.Set("42")).StatusCode);
			Assert.Equal(501, Assert.IsType<StatusCodeResult>(await controller.SetFiltering("42")).StatusCode);
			Assert.Equal(501, Assert.Throws<BadHttpRequestException>(() => controller.GetFiltering("42")).StatusCode);
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task PrivilegeFiltering_UsesFilteringServiceForReadAndWrite(bool role, bool reset)
	{
		using var cancellation = new CancellationTokenSource();
		var sequence = TestContext.Sequence<IPrivilege>(new TestPrivilege { Name = "Orders.Read" });
		var calls = new List<string>();
		var filtering = ServiceProxy.Create<IPrivilegeService<TestPrivilege>>((method, args) =>
		{
			calls.Add(method.Name);
			var identifier = Assert.IsType<Identifier>(args[0]);
			Assert.Equal(role ? typeof(IRole) : typeof(IUser), identifier.Type);
			Assert.Equal("42", identifier.Value);
			Assert.Equal(cancellation.Token, args[^1]);
			var parameters = Assert.IsType<Parameters>(args[^2]);
			Assert.Equal("tenant-a", parameters["namespace"]);
			Assert.Equal("trace-id", parameters["Trace"]);

			if(method.Name == "GetPrivilegesAsync")
				return sequence;

			Assert.Equal("SetPrivilegesAsync", method.Name);
			Assert.Equal("Orders.Read", Assert.Single(Assert.IsAssignableFrom<IEnumerable<IPrivilege>>(args[1])).Name);
			Assert.Equal(reset, Assert.IsType<bool>(args[2]));
			return new ValueTask<int>(1);
		});
		Authorization.Servicer.Privileges = ServiceProxy.Create<IPrivilegeService>((method, args) => method.Name == "get_Filtering" ? filtering : Unexpected(method, args));
		ControllerBase controller = role ? new RoleController.PrivilegeController() : new UserController.PrivilegeController();
		TestContext.Attach(controller, "[{\"Name\":\"Orders.Read\"}]", "application/json");
		controller.Request.QueryString = new QueryString("?namespace=tenant-a");
		controller.Request.Headers["X-Trace"] = "trace-id";
		var read = role ? ((RoleController.PrivilegeController)controller).GetFiltering("42", cancellation.Token) : ((UserController.PrivilegeController)controller).GetFiltering("42", cancellation.Token);
		Assert.Same(sequence, read);
		var written = role ? await ((RoleController.PrivilegeController)controller).SetFiltering("42", reset, cancellation.Token) : await ((UserController.PrivilegeController)controller).SetFiltering("42", reset, cancellation.Token);
		Assert.Equal("1", Assert.IsType<ContentResult>(written).Content);
		Assert.Equal(new[] { "GetPrivilegesAsync", "SetPrivilegesAsync" }, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Get_ServiceCancellation_PropagatesOriginalToken(bool role)
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		this.SetIdentityServices((method, args) =>
		{
			Assert.Equal("GetAsync", method.Name);
			Assert.Equal(cancellation.Token, args[^1]);

			return role ? new ValueTask<IRole>(Task.FromCanceled<IRole>(cancellation.Token)) : new ValueTask<IUser>(Task.FromCanceled<IUser>(cancellation.Token));
		});
		var controller = Controller(role);
		var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => role ? ((RoleController)controller).Get("42", cancellation: cancellation.Token) : ((UserController)controller).Get("42", cancellation: cancellation.Token));
		Assert.Equal(cancellation.Token, exception.CancellationToken);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ParentRoles_GetParents_PreservesSubjectAndResult(bool role)
	{
		using var cancellation = new CancellationTokenSource();
		var sequence = TestContext.Sequence<IRole>(new TestRole { Name = "Operators" });
		var calls = 0;
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			calls++;
			Assert.Equal("GetParentsAsync", method.Name);
			Assert.Equal(role ? Member.Role("42") : Member.User("42"), args[0]);
			Assert.Equal(cancellation.Token, args[1]);

			return sequence;
		});
		var result = role ? TestContext.Attach(new RoleController.MemberController()).GetParents("42", cancellation.Token) : TestContext.Attach(new UserController.MemberController()).GetParents("42", cancellation.Token);
		Assert.Same(sequence, result);
		Assert.Equal(1, calls);
	}

	[Fact]
	public void Members_Get_ForwardsRoleAndSchema()
	{
		using var cancellation = new CancellationTokenSource();
		var sequence = TestContext.Sequence<IMember>(new TestMember { Id = "42" });
		Authentication.Servicer.Members = ServiceProxy.Create<IMemberService>((method, args) =>
		{
			Assert.Equal("GetAsync", method.Name);
			Assert.Equal(Identity(true, "7"), args[0]);
			Assert.Equal("Member", args[1]);
			Assert.Equal(cancellation.Token, args[2]);

			return sequence;
		});
		var controller = TestContext.Attach(new RoleController.MemberController());
		controller.Request.Headers[Zongsoft.Web.Http.Headers.DataSchema] = "Member";
		Assert.Same(sequence, controller.Get("7", cancellation.Token));
	}

	[Fact]
	public async Task ForgetPassword_NoTicket_ReturnsNotFound()
	{
		var calls = 0;
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			calls++;
			Assert.Equal("ForgetPasswordAsync", method.Name);
			Assert.Equal("alice", args[0]);

			return new ValueTask<string>((string)null);
		});
		Assert.IsType<NotFoundResult>(await TestContext.Attach(new UserController.PasswordController()).ForgetAsync("alice"));
		Assert.Equal(1, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PasswordQuestions_OnlyOneSidePresent_ForwardsPartialUpdate(bool questionsOnly)
	{
		var content = new UserController.PasswordController.QuestionsAndAnswersModel
		{
			Questions = questionsOnly ? ["first?", "second?"] : null,
			Answers = questionsOnly ? null : ["one", "two"],
			Password = "test-password",
		};
		var calls = 0;
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService>((method, args) =>
		{
			calls++;
			Assert.Equal("SetPasswordQuestionsAndAnswersAsync", method.Name);
			Assert.Same(content.Questions, args[2]);
			Assert.Same(content.Answers, args[3]);

			return new ValueTask<bool>(true);
		});
		Assert.IsType<NoContentResult>(await TestContext.Attach(new UserController.PasswordController()).SetQuestionsAndAnswers("42", content));
		Assert.Equal(1, calls);
	}

	private void SetIdentityServices(Func<MethodInfo, object[], object> handler)
	{
		Authentication.Servicer.Users = ServiceProxy.Create<IUserService<TestUser>>(handler);
		Authentication.Servicer.Roles = ServiceProxy.Create<IRoleService<TestRole>>(handler);
	}

	private static ControllerBase Controller(bool role, string body = null, string contentType = "text/plain") => role ? TestContext.Attach(new RoleController(), body, contentType) : TestContext.Attach(new UserController(), body, contentType);
	private static Identifier Identity(bool role, object id) => new(role ? typeof(IRole) : typeof(IUser), id);
	private static object Unexpected(MethodInfo method, object[] _) => throw new InvalidOperationException($"Unexpected service call: {method.Name}");
	private static void AssertMutation(IActionResult result, bool success)
	{
		if(success)
			Assert.IsType<NoContentResult>(result);
		else
			Assert.IsType<NotFoundResult>(result);
	}

	public sealed class TestUser : IUser
	{
		public Identifier Identifier { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public bool Enabled { get; set; }
		public bool? Gender { get; set; }
		public string Avatar { get; set; }
		public string Nickname { get; set; }
		public string Namespace { get; set; }
		public string Description { get; set; }
	}

	public sealed class TestRole : IRole
	{
		public Identifier Identifier { get; set; }
		public string Name { get; set; }
		public bool Enabled { get; set; }
		public string Avatar { get; set; }
		public string Nickname { get; set; }
		public string Namespace { get; set; }
		public string Description { get; set; }
	}

	public sealed class TestMember : IMember<TestRole>
	{
		public string Id { get; set; }
		public bool Embedded { get; set; }
		public Identifier RoleId => default;
		public Identifier MemberId => Identity(this.MemberType == MemberType.Role, this.Id);
		public MemberType MemberType { get; set; }
		public object Member => this.Embedded ? new Member(this.MemberId, this.MemberType) : null;
		public TestRole Role => null;
		public bool Equals(IMember other) => other != null && this.MemberId == other.MemberId && this.MemberType == other.MemberType;
		public bool Equals(IMember<TestRole> other) => this.Equals((IMember)other);
	}

	public sealed class TestPrivilege : IPrivilege
	{
		public string Name { get; set; }
		public bool IsEmpty => string.IsNullOrEmpty(this.Name);
		public bool Equals(IPrivilege other) => string.Equals(this.Name, other?.Name, StringComparison.Ordinal);
	}
}
