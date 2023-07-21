namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetResolverTest
{
	private readonly SpreadsheetResolver _resolver = new ();

    [Fact]
	public void TestResolve()
	{
		var path = Path.Combine(Environment.CurrentDirectory, $"{Templates.User.Template.Name}.xlsx");
		var input = File.Open(path, FileMode.Open, FileAccess.Read);

		var result = _resolver.ResolveAsync<User>(input, Templates.User.Template).Synchronize()?.ToArray();
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(Templates.User.Data.Length, result.Length);

		//关闭文件流
		input.Dispose();

		for(var i = 0; i < result.Length; i++)
		{
			Assert.Equal(Templates.User.Data[i].UserId, result[i].UserId);
			Assert.Equal(Templates.User.Data[i].Name, result[i].Name);
			Assert.Equal(Templates.User.Data[i].Nickname, result[i].Nickname);
			Assert.Equal(Templates.User.Data[i].Gender, result[i].Gender);
			Assert.Equal(Templates.User.Data[i].Birthday, result[i].Birthday);
		}
	}
}