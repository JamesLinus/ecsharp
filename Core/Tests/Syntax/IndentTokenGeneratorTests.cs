﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Tests;
using Loyc.Syntax.Les;

namespace Loyc.Syntax.Lexing
{
	[TestFixture]
	public class IndentTokenGeneratorTests : TestHelpers
	{
		[Test]
		public void BasicTests()
		{
			Test(@"", @"");
			Test(@"
				test1
				test2;
				test3",
				@"test1; test2; test3;");
			Test(@"
				test1;
				test2
				test3;",
				"test1; test2; test3;");
			Test(@"
				joe:
					Hello;",
				"joe: { Hello; }");
			Test(@"
				joe: blow:
					Hello;",
				"joe: { blow: { Hello; } }");
			Test(@"
				joe:
					Hello -
					I am joe
				end joe;
				bob:
					I am bob;
				end bob",
				"joe: { Hello -; I am joe; } end joe; bob: { I am bob; } end bob;");
			Test(@"
				apple
				:
					banana
				:
					coconut",
				"apple; : { banana; } : { coconut; }");
		}
		[Test]
		public void MoreTests()
		{
			Test("hello:", "hello: { }");
			Test(@"
				((hello
				world)):", "((hello world)): { }");
			Test(@"
				joe:
					Hello (
						My name is joe) etc
					Whazzap (I
				am drunk) etc
					(Goodbye: (leaving)
				)
				end joe",
				"joe: { Hello (My name is joe) etc; Whazzap (I am drunk) etc; (Goodbye: (leaving)); } end joe;");
			Test(@"
				joe: line1
					line2;",
				@"joe: { line1; line2; }");
			Test(@"
				joe: :blow
					Hello;",
				@"joe: {:{ blow; Hello; } }");
			Test(@"
				foo:
					:",
				@"foo: { :{} }");
			Test(@"
				look outside
				if: apocalypse
					then:
						suicide
				rip",
				@"look outside; if: { apocalypse; then: { suicide; } } rip;");
		}
		[Test]
		public void EolTriggerTests()
		{
			Test(@"inc x = x + 1", "inc x = x + 1;");
			Test(@"inc x = ", "inc x = { }");
			Test(@": = ", ": { = { } }");
			Test(@"
				= 
					: ", "= { : { } }");
			Test(@"
				inc x = 
					x + 1",
				"inc x = { x + 1; }");
			Test(@"
				: =
					Hello
				Goodbye;",
				@": { = { Hello; } } Goodbye;");
			Test(@"
				today:
					baking
					yummy = mash =
						potatoes
						chives
						gravy =
							mystery ingredients
						butter
					",
				"today: { baking; yummy = mash = { potatoes; chives; gravy = { mystery ingredients; } butter; } }");
			Test(@"
				fun = 
					sights seen
					time:
						enjoyed
				",
				"fun = { sights seen; time: { enjoyed; } }");
		}
		[Test]
		public void TestErrorsAndWarnings()
		{
			Test(@"
				unexpected
					indent;
					etc",
				@"unexpected; indent; etc;", 1, Severity.Error);
			Test(@"
					unexpected
				dedent
				etc;",
				@"unexpected; dedent; etc;", 1, Severity.Error);
			Test(@"
				unexpected:
						indent;
					partial dedent
				the end",
				@"unexpected: { indent; partial dedent; } the end;", 1, Severity.Error);
			Test(@"
				unexpected;
					indent
					blah blah
				etc;",
				@"unexpected; indent; blah blah; etc;", 1, Severity.Error);
			Test(@"
				unexpected
					indent;
				and;
					again;",
				@"unexpected; indent; and; again;", 2, Severity.Error);
			Test(@"
				joe: black:
				    Twice indented;
				  Once removed;
				fin;",
				@"joe: { black: { Twice indented; } Once removed; } fin;", 1, Severity.Warning);
			Test(("\tstyle:\n"+
				"\t....ok;\n"+
				"\tstill:\n"+
				"\t\tok;").Replace('.',' '),
				@"style: { ok; } still: { ok; }");
			Test(("....style\n"+
				"\tchange;").Replace('.',' '),
				@"style; change;", 1, Severity.Warning);
			Test(("\tstyle:\n"+
				"......change;\n").Replace('.',' '),
				@"style: { change; }", 1, Severity.Warning);
		}

		static readonly int[] allIndTrig = new int[] { (int)CalcTokenType.Assign, (int)CalcTokenType.Colon };
		static readonly int[] eolIndTrig = new int[] { (int)CalcTokenType.Assign };
		static readonly int[] openBr = new int[] { (int)CalcTokenType.LParen, (int)CalcTokenType.LBrace };
		static readonly int[] closeBr = new int[] { (int)CalcTokenType.RParen, (int)CalcTokenType.RBrace };
		private void Test(string input, string expectOutput, int expectMessages = 0, Severity expectSev = 0)
		{
			// Install token-to-string stategy to aid debugging
			using (Token.SetToStringStrategy(t => (t.Value ?? ((CalcTokenType)t.TypeInt).ToString()).ToString())) {
				MessageHolder errorList;
				var input2 = StripInitialNewline(input);
				var lexer = new CalculatorLexer(input2) { ErrorSink = errorList = new MessageHolder() };
				var wrapr = new IndentTokenGenerator(lexer, allIndTrig, new Token((int)CalcTokenType.Semicolon, 0, 0, null)) {
					EolIndentTriggers = eolIndTrig, 
					IndentToken = new Token((int)CalcTokenType.LBrace, 0, 0, null),
					DedentToken = new Token((int)CalcTokenType.RBrace, 0, 0, null),
				};
				var output = new DList<Token>();
				for (var t = wrapr.NextToken(); t.HasValue; t = wrapr.NextToken())
					output.Add(t.Value);
				var expectTokens = new CalculatorLexer(expectOutput).ToList();
				
				AreEqual(expectMessages, errorList.List.Count);
				if (expectMessages > 0)
					AreEqual(expectSev, errorList.List.Max(m => m.Severity));
				ExpectList(output, expectTokens, false);
			}
		}
		static UString StripInitialNewline(UString input)
		{
			if (input.StartsWith("\r")) input = input.Slice(1);
			if (input.StartsWith("\n")) input = input.Slice(1);
			return input;
		}
	}
}
