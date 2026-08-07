using System;
using System.IO;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Bogus;
using ClosedXML.Excel;

using Zongsoft.Common;
using Zongsoft.Data;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml.Samples;

internal class Program
{
	private static readonly ModelDescriptor Model = Zongsoft.Data.Model.GetDescriptor<User>();
	private static readonly SpreadsheetGenerator Generator = new();
	private static readonly SpreadsheetExtractor Extractor = new();

	static async Task Main(string[] args)
	{
		var executor = Terminal.Console.Executor;

		executor.Command("export", async (context, cancellation) =>
		{
			var culture = GetCulture(context);
			var count = GetCount(context);
			var path = GetExportPath(context, culture, count);

			using var scope = new CultureScope(culture);
			await ExportAsync(path, count, culture, cancellation);
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"Exported {count} records: {path}");
			DisplayWorkbook(context.Output, path);
		});

		executor.Command("import", async (context, cancellation) =>
		{
			var path = GetPath(context);
			var users = await ImportAsync(path, cancellation);
			DisplayWorkbook(context.Output, path);
			DisplayData(context.Output, users);
		});

		executor.Command("verify", async (context, cancellation) =>
		{
			var culture = GetCulture(context);
			var count = GetCount(context);
			var path = GetExportPath(context, culture, count);

			using var scope = new CultureScope(culture);
			await ExportAsync(path, count, culture, cancellation);
			var users = await ImportAsync(path, cancellation);
			DisplayWorkbook(context.Output, path);
			DisplayData(context.Output, users);
		});

		executor.Aliaser.Set("export", "out");
		executor.Aliaser.Set("import", "in");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64))
			.AppendLine(CommandOutletColor.Cyan, "ClosedXML Import and Export Sample".Justify(64))
			.AppendLine(CommandOutletColor.DarkGray, "export [--count:<number>|-c:<number>] [--culture:<name>|-l:<name>] [file]".Justify(64))
			.AppendLine(CommandOutletColor.DarkGray, "import [file]  verify [options] [file]".Justify(64))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64));

		await executor.RunAsync(splash);
	}

	private static async ValueTask ExportAsync(string path, int count, CultureInfo culture, CancellationToken cancellation)
	{
		await using var stream = File.Create(path);
		await Generator.GenerateAsync(stream, Model, GenerateUsers(count, culture), cancellation);
	}

	private static User[] GenerateUsers(int count, CultureInfo culture)
	{
		var current = culture ?? CultureInfo.CurrentUICulture;
		var faker = new Faker(string.Equals(current.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase) ? "zh_CN" : "en_US");
		var users = new User[count];

		for(int index = 0; index < users.Length; index++)
		{
			users[index] = new User
			{
				UserId = index + 1,
				Name = faker.Name.FullName(),
				Gender = (Gender)faker.Random.Int(0, 1),
				Birthday = faker.Random.Bool(0.8f) ? faker.Date.Past(70, DateTime.Today.AddYears(-18)).Date : null,
				Email = faker.Internet.Email(),
				IsActive = faker.Random.Bool(0.8f) ? faker.Random.Bool() : null,
				Status = faker.Random.Bool(0.8f) ? (UserStatus)faker.Random.Int(0, 2) : null,
				Balance = faker.Random.Bool(0.8f) ? decimal.Round(faker.Random.Decimal(-10000, 10000), 2) : null,
				Creation = faker.Date.Recent(730),
				Description = faker.Random.Int(0, 1) == 0 ? faker.Lorem.Sentence(3, 0) : faker.Lorem.Paragraphs(2),
			};
		}

		return users;
	}

	private static async ValueTask<IReadOnlyList<User>> ImportAsync(string path, CancellationToken cancellation)
	{
		var users = new List<User>();
		await using var stream = File.OpenRead(path);

		await foreach(var user in Extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Model), cancellation))
			users.Add(user);

		return users;
	}

	private static void DisplayWorkbook(ICommandOutlet output, string path)
	{
		using var workbook = new XLWorkbook(path);
		var table = workbook.Table(Model.Name);

		output.WriteLine(CommandOutletColor.DarkCyan, $"Workbook: {Path.GetFileName(path)}");
		output.WriteLine($"Worksheet: {table.Worksheet.Name}");
		output.WriteLine($"Table: {table.Name}");
		output.WriteLine($"Range: {table.RangeAddress}");
		output.WriteLine($"Columns: {table.ColumnCount()}, Declared rows: {table.DataRange?.RowCount() ?? 0}");
	}

	private static void DisplayData(ICommandOutlet output, IReadOnlyList<User> users)
	{
		output.WriteLine(CommandOutletColor.Magenta, $"Imported records: {users.Count}");

		for(int index = 0; index < users.Count; index++)
		{
			var user = users[index];
			output.WriteLine($"[{index + 1}] Id={user.UserId}, Name={user.Name}, Gender={user.Gender}, Birthday={user.Birthday:yyyy-MM-dd}, Email={user.Email}, Active={user.IsActive?.ToString() ?? "<null>"}, Status={user.Status?.ToString() ?? "<null>"}, Balance={user.Balance?.ToString("0.00") ?? "<null>"}, Description={user.Description}, Creation={user.Creation:yyyy-MM-dd HH:mm:ss}");
		}
	}

	private static CultureInfo GetCulture(CommandContext context)
	{
		var specified = context.Options.TryGetValue<string>("culture", out var cultureName);

		if(!specified)
			specified = context.Options.TryGetValue<string>("l", out cultureName);

		if(!specified)
			return null;

		try
		{
			return CultureInfo.GetCultureInfo(cultureName);
		}
		catch(CultureNotFoundException)
		{
			throw new CommandOptionValueException("culture", cultureName);
		}
	}

	private static int GetCount(CommandContext context)
	{
		const int DEFAULT_COUNT = 10;

		if(!context.Options.TryGetValue<int>("count", out var count) &&
		   !context.Options.TryGetValue<int>("c", out count))
			return DEFAULT_COUNT;

		if(count < 0)
			throw new CommandOptionValueException("count", count.ToString(CultureInfo.InvariantCulture));

		return count;
	}

	private static string GetPath(CommandContext context)
	{
		var path = context.Arguments.IsEmpty ? "users.xlsx" : context.Arguments[0];
		return Path.GetFullPath(path);
	}

	private static string GetExportPath(CommandContext context, CultureInfo culture, int count)
	{
		if(!context.Arguments.IsEmpty)
			return Path.GetFullPath(context.Arguments[0]);

		var cultureName = (culture ?? CultureInfo.CurrentUICulture).Name;
		if(string.IsNullOrEmpty(cultureName))
			cultureName = "en";

		return Path.GetFullPath($"users.{cultureName}({count.ToString(CultureInfo.InvariantCulture)}).xlsx");
	}

	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
		private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

		public CultureScope(CultureInfo culture)
		{
			if(culture == null)
				return;

			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		public void Dispose()
		{
			if(_culture == null)
				return;

			CultureInfo.CurrentCulture = _culture;
			CultureInfo.CurrentUICulture = _uiCulture;
		}
	}

	private sealed class User
	{
		[ModelProperty(DbType.Int32, false, IsPrimaryKey = true)]
		public int UserId { get; set; }
		[ModelProperty(DbType.String, 50, false)]
		public string Name { get; set; }
		public Gender Gender { get; set; }
		public DateTime? Birthday { get; set; }
		[ModelProperty(DbType.AnsiString, 100, true)]
		public string Email { get; set; }
		public bool? IsActive { get; set; }
		public UserStatus? Status { get; set; }
		[ModelProperty(DbType.Decimal, true, Role = nameof(ModelPropertyRole.Currency))]
		public decimal? Balance { get; set; }
		[ModelProperty(DbType.DateTime, false, DefaultValue = "now()")]
		public DateTime Creation { get; set; }
		[ModelProperty(DbType.String, 500, true, Role = nameof(ModelPropertyRole.Description))]
		public string Description { get; set; }
	}

	private enum Gender : byte
	{
		Female,
		Male,
	}

	private enum UserStatus : byte
	{
		Inactive,
		Active,
		Suspended,
	}
}
