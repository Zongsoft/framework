using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Expressions.Tests;

public class LexerTest
{
	[Fact]
	public void Test()
	{
		const string EXPRESSION = @"1+2f	_abc123'text\'suffix'	-30L*4.5 / 5.5m (true || FALSE?yes:no)null??nothing";

		using var scanner = Lexer.Instance.GetScanner(EXPRESSION);

		var token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<int>(token.Value);
		Assert.Equal(1, (int)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Plus, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<float>(token.Value);
		Assert.Equal(2.0f, (float)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.Equal("_abc123", (string)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.Equal("text'suffix", (string)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Minus, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<long>(token.Value);
		Assert.Equal(30L, (long)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Multiply, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<double>(token.Value);
		Assert.Equal(4.5, (double)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Divide, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<decimal>(token.Value);
		Assert.Equal(5.5m, (decimal)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.OpeningParenthesis, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(Token.True, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.OrElse, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(Token.False, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Question, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.Equal("yes", (string)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Colon, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.Equal("no", (string)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.ClosingParenthesis, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(Token.Null, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(SymbolToken.Coalesce, token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.Equal("nothing", (string)token.Value);

		token = scanner.Scan();
		Assert.Null(token);
	}

	[Fact]
	public void TestConditionalExpression()
	{
		const string EXPRESSION = @"Field1 == 100 && Field2<1.23f && Field3 >=10.5m && (PI between ""3.1415926~3.1415927"" || Number IN [10,20,30] )";

		var lexer = new Lexer();
		lexer.Tokenizers.Insert(0, new KeywordTokenizer(true, "in", "Between"));

		using var scanner = lexer.GetScanner(EXPRESSION);

		var token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal("Field1", token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.Equal, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<int>(token.Value);
		Assert.Equal(100, (int)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.AndAlso, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal("Field2", token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.LessThan, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<float>(token.Value);
		Assert.Equal(1.23f, (float)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.AndAlso, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal("Field3", token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.GreaterThanOrEqual, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<decimal>(token.Value);
		Assert.Equal(10.5m, (decimal)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.AndAlso, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.OpeningParenthesis, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal("PI", token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Keyword, token.Type);
		Assert.Equal("Between", (string)token.Value, true);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<string>(token.Value);
		Assert.True(Zongsoft.Data.Range.TryParse<double>((string)token.Value, out var range));
		Assert.Equal(3.1415926, range.Minimum);
		Assert.Equal(3.1415927, range.Maximum);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.OrElse, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal("Number", token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Keyword, token.Type);
		Assert.Equal("IN", (string)token.Value, true);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.OpeningBracket, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<int>(token.Value);
		Assert.Equal(10, (int)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.Comma, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<int>(token.Value);
		Assert.Equal(20, (int)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.Comma, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Constant, token.Type);
		Assert.IsType<int>(token.Value);
		Assert.Equal(30, (int)token.Value);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.ClosingBracket, (SymbolToken)token);

		token = scanner.Scan();
		Assert.NotNull(token);
		Assert.Equal(TokenType.Symbol, token.Type);
		Assert.Equal(SymbolToken.ClosingParenthesis, (SymbolToken)token);

		token = scanner.Scan();
		Assert.Null(token);
	}
}

public class LexerBoundaryTest
{
	[Theory]
	[InlineData("tru")]
	[InlineData("nul")]
	[InlineData("trueValue")]
	[InlineData("FALSEhood")]
	[InlineData("null_name")]
	[InlineData("_name1")]
	public void CompleteIdentifier(string text)
	{
		using var scanner = Lexer.Instance.GetScanner(text);
		var token = scanner.Scan(out var position, out var length);

		Assert.Equal(TokenType.Identifier, token.Type);
		Assert.Equal(text, token.Value);
		Assert.Equal(0, position);
		Assert.Equal(text.Length, length);
		Assert.Null(scanner.Scan());
	}

	[Fact]
	public void KeywordAndSymbolBoundaries()
	{
		var lexer = new Lexer();
		lexer.Tokenizers.Insert(0, new KeywordTokenizer(true, "in", "between"));
		using var scanner = lexer.GetScanner("inside in betweenX BETWEEN ?? ? >= >");

		Assert.Equal(TokenType.Identifier, scanner.Scan().Type);
		Assert.Equal(TokenType.Keyword, scanner.Scan().Type);
		Assert.Equal(TokenType.Identifier, scanner.Scan().Type);
		Assert.Equal(TokenType.Keyword, scanner.Scan().Type);
		Assert.Equal(SymbolToken.Coalesce, scanner.Scan());
		Assert.Equal(SymbolToken.Question, scanner.Scan());
		Assert.Equal(SymbolToken.GreaterThanOrEqual, scanner.Scan());
		Assert.Equal(SymbolToken.GreaterThan, scanner.Scan());
		Assert.Null(scanner.Scan());
	}

	[Fact]
	public void StreamAndStringHaveSameCharactersAndOffsets()
	{
		const string TEXT = " \t'中文😀\\n' + trueValue 2147483648  \r\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TEXT));
		using var streamed = Lexer.Instance.GetScanner(stream);
		using var literal = Lexer.Instance.GetScanner(TEXT);

		while(true)
		{
			var first = literal.Scan(out var position, out var length);
			var second = streamed.Scan(out var streamedPosition, out var streamedLength);

			Assert.Equal(position, streamedPosition);
			Assert.Equal(length, streamedLength);
			Assert.Equal(first?.Type, second?.Type);
			Assert.Equal(first?.Value, second?.Value);

			if(first == null)
			{
				Assert.Equal(TEXT.Length, position);
				break;
			}
		}
	}

	[Fact]
	public void ScannerCanReadNonSeekableStream()
	{
		using var scanner = Lexer.Instance.GetScanner(new ForwardStream(Encoding.UTF8.GetBytes("name == '中文'")));
		var values = new List<object>();
		foreach(var token in scanner)
			values.Add(token.Value);
		Assert.Equal(["name", "==", "中文"], values);
	}

	[Fact]
	public void StreamReadFailureClosesSourceAndPreservesCause()
	{
		var cause = new IOException("read failure");
		using var stream = new FailingStream(cause);
		var error = Assert.Throws<IOException>(() =>
		{
			using var scanner = Lexer.Instance.GetScanner(stream);
		});

		Assert.Same(cause, error);
		Assert.False(stream.CanRead);
	}

	[Theory]
	[InlineData("\0")]
	[InlineData("中文")]
	[InlineData("'\\q'")]
	[InlineData("'\\")]
	[InlineData("'line\nbreak'")]
	[InlineData("'line\rbreak'")]
	[InlineData("'unterminated")]
	public void InvalidCharactersAndEscapes(string text)
	{
		Assert.Throws<SyntaxException>(() => ScanFirst(Lexer.Instance, text));
	}

	[Fact]
	public void SpanSlicesAndStackBuffersKeepRelativeOffsets()
	{
		const string SOURCE = "skip \t'中文😀' + 12L tail";
		using var scanner = Lexer.Instance.GetScanner(SOURCE.AsSpan(5, 13));
		Assert.Equal("中文😀", scanner.Scan(out var position, out var length).Value);
		Assert.Equal(1, position);
		Assert.Equal(6, length);
		Assert.Equal(SymbolToken.Plus, scanner.Scan(out position, out length));
		Assert.Equal(8, position);
		Assert.Equal(1, length);
		Assert.Equal(12L, scanner.Scan(out position, out length).Value);
		Assert.Equal(10, position);
		Assert.Equal(3, length);
		Assert.Null(scanner.Scan(out position, out length));
		Assert.Equal(13, position);
		Assert.Equal(0, length);

		Span<char> buffer = stackalloc char[16];
		"false??true".AsSpan().CopyTo(buffer);
		using var stacked = Lexer.Instance.GetScanner(buffer[..11]);
		Assert.Same(Token.False, stacked.Scan());
		Assert.Same(SymbolToken.Coalesce, stacked.Scan());
		Assert.Same(Token.True, stacked.Scan());
		Assert.Null(stacked.Scan());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t\r\n")]
	public void EmptyInputEndsAtItsLength(string text)
	{
		using var scanner = Lexer.Instance.GetScanner(text.AsSpan());
		Assert.Null(scanner.Scan(out var position, out var length));
		Assert.Equal(text?.Length ?? 0, position);
		Assert.Equal(0, length);
		Assert.Null(scanner.Scan());
	}

	[Fact]
	public void FailedTokenizerLeavesInputForNextTokenizer()
	{
		var lexer = new Lexer();
		var probe = new FailingTokenizer();
		lexer.Tokenizers.Insert(0, probe);
		using var scanner = lexer.GetScanner(" trueValue + 1");

		Assert.Equal("trueValue", scanner.Scan(out var position, out var length).Value);
		Assert.Equal(1, position);
		Assert.Equal(9, length);
		Assert.Same(SymbolToken.Plus, scanner.Scan(out position, out length));
		Assert.Equal(11, position);
		Assert.Equal(1, length);
		Assert.Equal(1, scanner.Scan(out position, out length).Value);
		Assert.Equal(13, position);
		Assert.Equal(1, length);
		Assert.Equal(["trueValue + 1", "+ 1", "1"], probe.Inputs);
		Assert.Null(scanner.Scan());
		Assert.Null(TokenResult.Fail().Token);
		Assert.Equal(0, TokenResult.Fail().Length);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(2)]
	public void SuccessfulTokenizerMustConsumeValidLength(int length)
	{
		var lexer = new Lexer();
		lexer.Tokenizers.Insert(0, new FixedTokenizer(new TokenResult(length, Token.True)));
		Assert.Throws<InvalidOperationException>(() => ScanFirst(lexer, "x"));
	}

	[Fact]
	public void EnumerationCopiesCursorAndKeepsSourceOpen()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("first + second"));
		var scanner = Lexer.Instance.GetScanner(stream);
		Assert.Equal("first", scanner.Scan().Value);

		foreach(var token in scanner)
		{
			Assert.Same(SymbolToken.Plus, token);
			break;
		}

		Assert.True(stream.CanRead);

		var values = new List<object>();
		foreach(var token in scanner)
			values.Add(token.Value);
		Assert.Equal(["+", "second"], values);
		Assert.True(stream.CanRead);
		Assert.Same(SymbolToken.Plus, scanner.Scan());
		Assert.Equal("second", scanner.Scan().Value);
		Assert.Null(scanner.Scan());

		scanner.Dispose();
		Assert.False(stream.CanRead);
		scanner.Dispose();
		Exception failure = null;

		try { scanner.Scan(); }
		catch(Exception exception) { failure = exception; }

		Assert.IsType<ObjectDisposedException>(failure);
	}

	[Fact]
	public void LiteralMatcherReusesConfiguredLongestLiteral()
	{
		var longer = new string(['b', 'e', 't', 'w', 'e', 'e', 'n']);
		var keyword = new KeywordTokenizer(true, "be", longer);
		var matched = keyword.Tokenize("BETWEEN ".AsSpan());
		Assert.Equal(7, matched.Length);
		Assert.Equal(TokenType.Keyword, matched.Token.Type);
		Assert.Same(longer, matched.Token.Value);
		Assert.Null(keyword.Tokenize("betweenX").Token);
		Assert.Null(keyword.Tokenize("bet").Token);
		Assert.Equal(2, new SymbolTokenizer(">", ">=").Tokenize(">=x").Length);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4096)]
	public void StringsPreservePlainAndEscapedContent(int padding)
	{
		var tokenizer = new StringTokenizer();
		var prefix = new string('中', padding);
		var plain = "'" + prefix + "😀'rest";
		var result = tokenizer.Tokenize(plain);
		Assert.Equal(prefix + "😀", result.Token.Value);
		Assert.Equal(prefix.Length + 4, result.Length);

		var escaped = "'" + prefix + "\\s\\t\\n\\r\\\\\\'\\\"\\$'rest";
		result = tokenizer.Tokenize(escaped);
		Assert.Equal(prefix + " \t\n\r\\'\"$", result.Token.Value);
		Assert.Equal(escaped.Length - 4, result.Length);
		Assert.Equal("rest", new IdentifierTokenizer().Tokenize(escaped.AsSpan(result.Length)).Token.Value);
	}

	[Theory]
	[InlineData("2147483647+", typeof(int), "2147483647", 10)]
	[InlineData("2147483648]", typeof(long), "2147483648", 10)]
	[InlineData("1e3", typeof(int), "1", 1)]
	[InlineData("9223372036854775807L,", typeof(long), "9223372036854775807", 20)]
	[InlineData("1.5F?", typeof(float), "1.5", 4)]
	[InlineData("1.5d;", typeof(double), "1.5", 4)]
	[InlineData("1.5m)", typeof(decimal), "1.5", 4)]
	public void NumberSuffixesKeepValueAndConsumedLength(string text, Type type, string expected, int length)
	{
		var result = new NumberTokenizer().Tokenize(text);
		Assert.Equal(TokenType.Constant, result.Token.Type);
		Assert.Equal(type, result.Token.Value.GetType());
		Assert.Equal(expected, ((IFormattable)result.Token.Value).ToString(null, CultureInfo.InvariantCulture));
		Assert.Equal(length, result.Length);
	}

	[Theory]
	[InlineData("1.")]
	[InlineData("1.2.3")]
	[InlineData("1.5L")]
	public void InvalidNumbersKeepSyntaxFailures(string text)
	{
		Assert.Throws<SyntaxException>(() => new NumberTokenizer().Tokenize(text));
	}

	[Fact]
	public void OverflowKeepsNumericFailure()
	{
		Assert.Throws<OverflowException>(() => new NumberTokenizer().Tokenize("9223372036854775808"));
		Assert.Throws<OverflowException>(() => new NumberTokenizer().Tokenize(new string('9', 80) + "f"));
	}

	[Fact]
	public void FixedTokensDoNotAllocateAfterWarmup()
	{
		Assert.Equal(0, MeasureAllocations(new BooleanTokenizer(), "true", out var consumed));
		Assert.Equal(4 * 256, consumed);
		Assert.Equal(0, MeasureAllocations(new NullTokenizer(), "null", out consumed));
		Assert.Equal(4 * 256, consumed);
		Assert.Equal(0, MeasureAllocations(new SymbolTokenizer(), "??", out consumed));
		Assert.Equal(2 * 256, consumed);
	}

	[Fact]
	public void NumericAllocationsDoNotGrowWithLiteralLength()
	{
		var tokenizer = new NumberTokenizer();
		var shortLiteral = "1";
		var longLiteral = new string('0', 1024) + "1";
		var shortAllocations = MeasureAllocations(tokenizer, shortLiteral, out var shortConsumed);
		var longAllocations = MeasureAllocations(tokenizer, longLiteral, out var longConsumed);
		Assert.Equal(256, shortConsumed);
		Assert.Equal(1025 * 256, longConsumed);
		Assert.Equal(shortAllocations, longAllocations);
	}

	private static long MeasureAllocations(ITokenizer tokenizer, string text, out int consumed)
	{
		for(int i = 0; i < 256; i++)
			tokenizer.Tokenize(text);

		consumed = 0;
		var before = GC.GetAllocatedBytesForCurrentThread();
		for(int i = 0; i < 256; i++)
			consumed += tokenizer.Tokenize(text).Length;
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	private static Token ScanFirst(Lexer lexer, string text)
	{
		using var scanner = lexer.GetScanner(text);
		return scanner.Scan();
	}

	private sealed class FixedTokenizer(TokenResult result) : ITokenizer
	{
		public TokenResult Tokenize(ReadOnlySpan<char> text) => result;
	}

	private sealed class FailingTokenizer : ITokenizer
	{
		public List<string> Inputs { get; } = [];
		public TokenResult Tokenize(ReadOnlySpan<char> text)
		{
			this.Inputs.Add(text.ToString());
			return new TokenResult(123, null);
		}
	}

	private sealed class ForwardStream(byte[] bytes) : MemoryStream(bytes)
	{
		public override bool CanSeek => false;
		public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
	}

	private sealed class FailingStream(IOException failure) : MemoryStream
	{
		public override int Read(byte[] buffer, int offset, int count) => throw failure;
		public override int Read(Span<byte> buffer) => throw failure;
	}
}
