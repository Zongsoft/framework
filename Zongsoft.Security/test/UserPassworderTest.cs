using Zongsoft.Data;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Tests;

public class UserPassworderTest : PassworderContractTest
{
	protected override Passworder Create(IDataAccess accessor) => ((IUserService)new UserService(accessor)).Passworder;

	private sealed class UserService(IDataAccess accessor) : Zongsoft.Security.Privileges.UserService
	{
		protected override IDataAccess Accessor => accessor;
	}
}
