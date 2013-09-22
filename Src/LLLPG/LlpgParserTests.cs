﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Utilities;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	[TestFixture]
	class LlpgParserTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		static Symbol Seq = S.Tuple;
		static Symbol AndNot = GSymbol.Get("#&!");
		static Symbol Gate = S.Lambda, Plus = GSymbol.Get("#suf+"), Star = GSymbol.Get("#suf*"), Opt = GSymbol.Get("#suf?");
		static Symbol Greedy = GSymbol.Get("greedy"), Nongreedy = GSymbol.Get("nongreedy");
		static Symbol Default = GSymbol.Get("default"), Error = GSymbol.Get("error");
		static LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c");

		[SetUp]
		void SetUp()
		{
			LanguageService.Current = LesLanguageService.Value;
			MessageSink.Current = MessageSink.Console;
		}

		[Test]
		public void Stage1Les_SimpleTests()
		{
			TestStage1("a", a);
			TestStage1("'a'", F.Literal('a'));
			TestStage1("123", F.Literal(123));
			TestStage1("a..b", F.Call(S.DotDot, a, b));
			TestStage1("~a", F.Call(S.NotBits, a));
			TestStage1("a*", F.Call(Star, a));
			TestStage1("a+", F.Call(Plus, a));
			TestStage1("a | b", F.Call(S.OrBits, a, b));
			TestStage1("a / b", F.Call(S.Div, a, b));
			TestStage1("a(b | c)", F.Call(a, F.Call(S.OrBits, b, c)));
			TestStage1("a => b", F.Call(Gate, a, b));
			TestStage1("()", F.Call(Seq));
			TestStage1("a b", F.Call(Seq, a, b));
			TestStage1("(a) (b)", F.Call(Seq, a, b));
			TestStage1("(a b)?", F.Call(Opt, F.Call(Seq, a, b)));
			TestStage1("{ a() }", F.Braces(F.Call(a)));
			TestStage1("&{ a b | c; }", F.Call(S.AndBits, F.Braces(F.Call(a, F.Call(S.OrBits, b, c)))));
			TestStage1("&!{ a(); b(); }", F.Call(AndNot, F.Braces(F.Call(a), F.Call(b))));
			TestStage1("greedy a", F.Call(Greedy, a));
			TestStage1("nongreedy a", F.Call(Nongreedy, a));
			TestStage1("nongreedy(a)", F.Call(Nongreedy, a));
			TestStage1("default a", F.Call(Default, a));
			TestStage1("error a", F.Call(Error, a));
		}
		[Test]
		public void Stage1Les_MoreTests()
		{
			TestStage1("~a..b", F.Call(S.NotBits, F.Call(S.DotDot, a, b)));
			TestStage1("{ a(); } b c", F.Call(Seq, F.Braces(F.Call(a)), b, c));
			TestStage1("a (b c)", F.Call(Seq, a, F.Call(Seq, b, c)));
			TestStage1("a | (a b c)", F.Call(S.OrBits, a, F.Call(Seq, a, b, c)));
			TestStage1("a(b c)", F.Call(a, F.Call(b, c)));
			TestStage1("a | b / c", F.Call(S.Div, F.Call(S.OrBits, a, b), c));
			TestStage1("a / b | c", F.Call(S.OrBits, F.Call(S.Div, a, b), c));
			TestStage1("a* b | c", F.Call(S.OrBits, F.Call(Seq, F.Call(Star, a), b), c));
			TestStage1("a b? / c", F.Call(S.Div, F.Call(Seq, a, F.Call(Opt, b)), c));
			TestStage1("a / b => b+ / c", F.Call(S.Div, F.Call(S.Div, a, F.Call(Gate, b, F.Call(Plus, b))), c));
			TestStage1("~(a..b) | (-a)..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("~ a..b  |  -a ..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("a..b+", F.Call(Plus, F.Call(S.DotDot, a, b)));
			TestStage1("greedy(a | b)+", F.Call(Plus, F.Call(Greedy, F.Call(S.OrBits, a, b))));
			TestStage1("nongreedy a+",   F.Call(Plus, F.Call(Nongreedy, a)));
			TestStage1("default a b | c", F.Call(S.OrBits, F.Call(Default, F.Call(Seq, a, b)), c));
			TestStage1("error   a b | c", F.Call(S.OrBits, F.Call(Error,   F.Call(Seq, a, b)), c));
		}

		void TestStage1(string text, LNode expected)
		{
			var lexer = LanguageService.Current.Tokenize(text, MessageSink.Console);
			var tokens = lexer.Buffered();
			var parser = new StageOneParser(tokens, lexer.File, MessageSink.Console);
			LNode result = parser.Parse();
			AreEqual(expected, result);
		}

		[Test]
		public void Stage2_Tests()
		{
			// If we change the way Preds are printed, this will break, of course
			TestStage2(true, "az", "'a'..'z'", "[a-z]");
			TestStage2(true, "azAZ", "('a'..'z')|('A'..'Z')", "[A-Za-z]");
			TestStage2(true, "NotAZ", "~('A'..'Z')", @"[^\$A-Z]");
			TestStage2(true, "Seq", "('-', '0'..'9')", @"[\-] [0-9]");
			TestStage2(true, "Hi0-9", @"(""Hi"", '0'..'9')", "[H] [i] [0-9]");
			TestStage2(true, "Or1", @"""ETX"" | 3", "([E] [T] [X] | (3))");
			TestStage2(true, "Or2", @"(~10, {code;}) | '\n'", @"(~(-1, 10) | [\n])"); // code blocks not printed
			TestStage2(true, "Star", @"@`#suf*`('0'..'9')", "([0-9])*");
			TestStage2(true, "Plus", @"@`#suf+`('0'..'9')", "[0-9] ([0-9])*");
			TestStage2(true, "Opt", @"@`#suf?`(('a','b'))", "([a] [b])?");
			TestStage2(true, "Greedy", @"@`#suf*`(greedy(('a','b')))", "greedy([a] [b])*");
			TestStage2(true, "Nongreedy", @"@`#suf*`(nongreedy(('a','b')))", "nongreedy([a] [b])*");
			TestStage2(true, "Default1", @"('a'|""bee""|default('b'))", "([a] | [b] [e] [e] | default [b])");
			TestStage2(true, "Default2", @"@`#suf*`('a'|default('b')|'c')", "([a] | default [b] | [c])*");
			TestStage2(true, Tuple.Create("RuleRef", @"'.' | Digit", "([.] | Digit)"),
			                 Tuple.Create("Digit", "'0'..'9'", "[0-9]"));
			TestStage2(false, "AorB", @"a | b", "(a|b)");
			TestStage2(false, "ABorCD", @"A.B | C.D", "(A.B|C.D)");
			TestStage2(false, "AB+orCD", @"@`#suf+`(A.B) | C.D", "(A.B (A.B)* | C.D)");
			TestStage2(false, Tuple.Create("RuleRef", @"""NaN"" | (Digit, _)", @"(""NaN"" | Digit ~(EOF))"),
			                 Tuple.Create("Digit", "zero|one", "(zero|one)"));
		}

		void TestStage2(bool lexer, string ruleName, string inputExpr, string asString)
		{
			TestStage2(lexer, Tuple.Create(ruleName, inputExpr, asString));
		}
		void TestStage2(bool lexer, params Tuple<string,string,string>[] ruleTuples)
		{
			var helper = lexer ? (IPGCodeGenHelper)new IntStreamCodeGenHelper() : new GeneralCodeGenHelper();
			var rules = new List<Pair<Rule,LNode>>();
			foreach (var tuple in ruleTuples)
			{
				string ruleName = tuple.Item1, inputExpr = tuple.Item2;
				var node = LesLanguageService.Value.ParseSingle(inputExpr, MessageSink.Console, LanguageService.Exprs);
				var rule = new Rule(node, GSymbol.Get(ruleName), null);
				rules.Add(Pair.Create(rule, node));
			}
			
			var parser = new StageTwoParser(helper, MessageSink.Console);
			parser.Parse(rules);
			for (int i = 0; i < rules.Count; i++) {
				var rule = rules[i].A;
				var ruleAsString = rule.Pred.ToString();
				var expected = ruleTuples[i].Item3;
				if (expected == null)
					MessageSink.Console.Write(MessageSink.Warning, ruleTuples[i].Item1, ruleAsString);
				else
					AreEqual(expected, ruleAsString);
			}
		}
	}
}
