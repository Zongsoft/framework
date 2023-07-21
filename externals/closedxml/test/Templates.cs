namespace Zongsoft.Externals.ClosedXml.Tests;

internal static class Templates
{
	public static class User
	{
		public static readonly DataFileTemplate Template = new("User", "用户");
		public static readonly Tests.User[] Data = new Tests.User[]
		{
			new(101, "Popeye", "Popeye Zhong", Gender.Male)
			{ Phone ="18912345678", Email="zongsoft@gmail.com" },
			new(102, "Grape", "Grape Liu", Gender.Female, new DateTime(1983, 1, 23)),
			new(103, "Elsa", "Elsa Zhong", Gender.Female, new DateTime(2015, 3, 23)),
			new(104, "Lucy", "Lucy Zhong", Gender.Female, new DateTime(2019, 5, 13)),
			new(105, "Jack", "Jack Ma"),
		};

		static User()
		{
			Template.Fields.Add(new(nameof(Tests.User.UserId), typeof(int), "用户编号"));
			Template.Fields.Add(new(nameof(Tests.User.Name), typeof(string), "用户名称"));
			Template.Fields.Add(new(nameof(Tests.User.Nickname), typeof(string), "用户昵称"));
			Template.Fields.Add(new(nameof(Tests.User.Email), typeof(string), "电子邮箱"));
			Template.Fields.Add(new(nameof(Tests.User.Phone), typeof(string), "手机号码"));
			Template.Fields.Add(new(nameof(Tests.User.Gender), typeof(Gender?), "性别"));
			Template.Fields.Add(new(nameof(Tests.User.Birthday), typeof(DateTime?), "出生日期"));
		}
	}
}
