using System;
using System.IO;
using System.Text;
using System.Linq;

using Xunit;

using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Expressions.Tests;

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
		Assert.Equal(["name", "==", "中文"], scanner.Select(token => token.Value).ToArray());
	}

	[Theory]
	[InlineData("\0")]
	[InlineData("中文")]
	[InlineData("'\\s'")]
	[InlineData("'\\q'")]
	[InlineData("'unterminated")]
	public void InvalidCharactersAndEscapes(string text)
	{
		using var scanner = Lexer.Instance.GetScanner(text);
		Assert.Throws<SyntaxException>(() => scanner.Scan());
	}

	private sealed class ForwardStream(byte[] bytes) : MemoryStream(bytes)
	{
		public override bool CanSeek => false;
		public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
	}
}
