using System;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Expressions.Tests
{
	public class LexerTest
	{
		[Fact]
		public void Test()
		{
			const string EXPRESSION = @"1+2f	_abc123'text\'suffix'	-30L*4.5 / 5.5m (true || FALSE?yes:no)null??nothing";

			var scanner = Lexer.Instance.GetScanner(EXPRESSION);
			Assert.NotNull(scanner);

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

			var scanner = lexer.GetScanner(EXPRESSION);
			Assert.NotNull(scanner);

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
}
