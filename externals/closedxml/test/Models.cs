namespace Zongsoft.Externals.ClosedXml.Tests;

public class User
{
	public User() { }
	public User(int userId, string name, string nickname, Gender? gender = null, DateTime? birthday = null)
	{
		this.UserId = userId;
		this.Name = name;
		this.Nickname = nickname;
		this.Gender = gender;
		this.Birthday = birthday;
	}

	public int UserId { get; set; }
	public string Name { get; set; }
	public string Nickname { get; set; }
	public string Email { get; set; }
	public string Phone { get; set; }
	public Gender? Gender { get; set; }
	public DateTime? Birthday { get; set; }
}

public enum Gender : byte
{
	Female,
	Male,
}