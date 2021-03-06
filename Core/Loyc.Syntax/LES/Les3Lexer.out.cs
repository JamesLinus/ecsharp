// Generated from Les3Lexer.ecs by LeMP custom tool. LeMP version: 2.4.2.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc;	// optional (for IMessageSink, Symbol, etc.)
using Loyc.Collections;	// optional (many handy interfaces & classes)
using Loyc.Syntax.Lexing;	// For BaseLexer
namespace Loyc.Syntax.Les
{
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	public partial class Les3Lexer {
		static readonly Symbol sy__apos_comma = (Symbol) "',", sy__apos_semi = (Symbol) "';", sy__aposx40 = (Symbol) "'@";
	
		void DotIndent()
		{
			int la0, la1;
			// Line 24: ([.] ([\t] | [ ] ([ ])*))*
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						// Line 24: ([\t] | [ ] ([ ])*)
						la0 = LA0;
						if (la0 == '\t')
							Skip();
						else {
							Match(' ');
							// Line 24: ([ ])*
							for (;;) {
								la0 = LA0;
								if (la0 == ' ')
									Skip();
								else
									break;
							}
						}
					} else
						break;
				} else
					break;
			}
		}
	
		object Newline(bool ignoreIndent = false)
		{
			int la0;
			// Line 27: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 27: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			AfterNewline(ignoreIndent, skipIndent: false);
			return _brackStack.Last == TokenType.LBrace ? null : WhitespaceTag.Value;
		}
	
		object SLComment()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 33: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\\':
					{
						la1 = LA(1);
						if (la1 == -1 || la1 == '\\')
							goto stop;
						else
							Skip();
					}
					break;
				case -1: case '\n': case '\r':
					goto stop;
				default:
					Skip();
					break;
				}
			}
		stop:;
			// Line 33: ([\\] [\\] | [\$\n\r] => )
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				Match('\\');
			} else { }
			// line 34
			return WhitespaceTag.Value;
		}
	
		object MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 36: nongreedy( MLComment / Newline / [^\$] )*
			for (;;) {
				switch (LA0) {
				case '*':
					{
						la1 = LA(1);
						if (la1 == -1 || la1 == '/')
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '*')
							MLComment();
						else
							Skip();
					}
					break;
				case '\n': case '\r':
					Newline(true);
					break;
				default:
					Skip();
					break;
				}
			}
		stop:;
			Match('*');
			Match('/');
			// line 37
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> Number_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
	
		object Number()
		{
			int la0, la1;
			UString suffix = default(UString);
			// Line 42: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				// line 42
				_type = TT.NegativeLiteral;
			}
			// Line 43: ( HexNumber / BinNumber / DecNumber )
			la0 = LA0;
			if (la0 == '0') {
				switch (LA(1)) {
				case 'X': case 'x':
					HexNumber();
					break;
				case 'B': case 'b':
					BinNumber();
					break;
				default:
					DecNumber();
					break;
				}
			} else
				DecNumber();
			// line 44
			UString numberText = Text();
			// Line 45: (IdCore)?
			do {
				la0 = LA0;
				if (la0 == '`') {
					la1 = LA(1);
					if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
						goto matchIdCore;
				} else if (Number_set0.Contains(la0))
					goto matchIdCore;
				break;
			matchIdCore:
				{
					_startPosition = InputPosition;
					object boolOrNull = NoValue.Value;
					suffix = IdCore(ref boolOrNull);
					// line 48
					PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
				}
			} while (false);
			// line 50
			return ParseLiteral2(suffix, numberText, true);
		}
	
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 52: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 52: greedy(['_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 52: ([0-9])*
						for (;;) {
							la0 = LA0;
							if (la0 >= '0' && la0 <= '9')
								Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			// Line 52: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		static readonly HashSet<int> HexDigit_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
	
		void HexDigit()
		{
			Match(HexDigit_set0);
		}
	
		void HexDigits()
		{
			int la0, la1;
			Skip();
			// Line 54: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 54: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 54: greedy([0-9A-Fa-f])*
						for (;;) {
							la0 = LA0;
							if (HexDigit_set0.Contains(la0))
								Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			// Line 54: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		bool Scan_HexDigits()
		{
			int la0, la1;
			if (!TryMatch(HexDigit_set0))
				return false;
			// Line 54: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0)){
					if (!TryMatch(HexDigit_set0))
						return false;}
				else
					break;
			}
			// Line 54: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('\'', '_'))
							return false;
						if (!TryMatch(HexDigit_set0))
							return false;
						// Line 54: greedy([0-9A-Fa-f])*
						for (;;) {
							la0 = LA0;
							if (HexDigit_set0.Contains(la0)){
								if (!TryMatch(HexDigit_set0))
									return false;}
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			// Line 54: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				if (!TryMatch('_'))
					return false;
			return true;
		}
	
		void DecNumber()
		{
			int la0, la1;
			// Line 57: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 58: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 59: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 59: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
	
		void HexNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 63: (HexDigits | [.] HexDigits => )
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			else { }
			// Line 65: ([.] ([0-9] =>  / &(HexDigits [Pp] [+\-0-9])) HexDigits)?
			do {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9')
						goto match1;
					else if (la1 >= 'A' && la1 <= 'F' || la1 >= 'a' && la1 <= 'f') {
						if (Try_HexNumber_Test0(1))
							goto match1;
					}
				}
				break;
			match1:
				{
					Skip();
					// Line 65: ([0-9] =>  / &(HexDigits [Pp] [+\-0-9]))
					la0 = LA0;
					if (la0 >= '0' && la0 <= '9') { } else
						Check(Try_HexNumber_Test0(0), "Expected HexDigits [Pp] [+\\-0-9]");
					HexDigits();
				}
			} while (false);
			// Line 67: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 67: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
	
		void BinNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 71: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 72: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 73: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 73: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
	
		object SQString()
		{
			int la0;
			// line 79
			bool parseNeeded = false;
			Skip();
			// Line 80: ([\\] [^\$] | [^\$\n\r'\\])
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept();
				// line 80
				parseNeeded = true;
			} else
				MatchExcept('\n', '\r', '\'', '\\');
			Match('\'');
			// line 81
			return ParseSQStringValue(parseNeeded);
		}
	
		object DQString()
		{
			int la0, la1;
			// line 84
			bool parseNeeded = false;
			Skip();
			// Line 85: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 85
						parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 86: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 86
				parseNeeded = true;
			// line 87
			return ParseStringValue(parseNeeded, isTripleQuoted: false);
		}
	
		object TQString()
		{
			int la0, la1, la2;
			bool parseNeeded = true;
			_style = NodeStyle.TDQStringLiteral;
			// Line 92: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 92: nongreedy(Newline / [^\$])*
				for (;;) {
					switch (LA0) {
					case '"':
						{
							la1 = LA(1);
							if (la1 == '"') {
								la2 = LA(2);
								if (la2 == -1 || la2 == '"')
									goto stop;
								else
									Skip();
							} else if (la1 == -1)
								goto stop;
							else
								Skip();
						}
						break;
					case -1:
						goto stop;
					case '\n': case '\r':
						Newline(true);
						break;
					default:
						Skip();
						break;
					}
				}
			stop:;
				Match('"');
				Match('"');
				Match('"');
			} else {
				// line 93
				_style = NodeStyle.TQStringLiteral;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 94: nongreedy(Newline / [^\$])*
				for (;;) {
					switch (LA0) {
					case '\'':
						{
							la1 = LA(1);
							if (la1 == '\'') {
								la2 = LA(2);
								if (la2 == -1 || la2 == '\'')
									goto stop2;
								else
									Skip();
							} else if (la1 == -1)
								goto stop2;
							else
								Skip();
						}
						break;
					case -1:
						goto stop2;
					case '\n': case '\r':
						Newline(true);
						break;
					default:
						Skip();
						break;
					}
				}
			stop2:;
				Match('\'');
				Match('\'');
				Match('\'');
			}
			// line 95
			return ParseStringValue(parseNeeded, isTripleQuoted: true);
		}
	
		void BQString(out bool parseNeeded)
		{
			int la0;
			// line 98
			parseNeeded = false;
			Skip();
			// Line 99: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 99
					parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			// Line 100: ([`])
			la0 = LA0;
			if (la0 == '`')
				Skip();
			else {
				// line 100
				parseNeeded = true;
				Error(0, "Expected closing backquote");
			}
		}
		static readonly HashSet<int> Operator_set0 = NewSet('!', '%', '&', '*', '+', '-', '.', '/', ':', '<', '=', '>', '?', '^', '|', '~');
	
		object Operator()
		{
			int la0;
			object result = default(object);
			// Line 107: ([$] | [!%&*+\--/:<-?^|~])
			la0 = LA0;
			if (la0 == '$')
				Skip();
			else
				Match(Operator_set0);
			// Line 107: ([!%&*+\--/:<-?^|~])*
			for (;;) {
				switch (LA0) {
				case '!': case '%': case '&': case '*':
				case '+': case '-': case '.': case '/':
				case ':': case '<': case '=': case '>':
				case '?': case '^': case '|': case '~':
					Skip();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			result = ParseOp(out _type);
			return result;
		}
		static readonly HashSet<int> SQOperator_set0 = NewSetOfRanges('!', '!', '#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
	
		object SQOperator()
		{
			int la0;
			Skip();
			// Line 110: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// Line 112: (['])?
			la0 = LA0;
			if (la0 == '\'') {
				Skip();
				// line 112
				_type = TT.Literal;
				return ParseSQStringValue(true);
			}
			// line 113
			return (Symbol) Text();
		}
	
		object Keyword()
		{
			int la0;
			Skip();
			Skip();
			// Line 122: ([#A-Z_a-z])*
			for (;;) {
				la0 = LA0;
				if (Number_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// line 123
			return (Symbol) Text();
		}
	
		object Id()
		{
			int la0, la1;
			UString idtext = default(UString);
			object value = default(object);
			// line 126
			object boolOrNull = NoValue.Value;
			idtext = IdCore(ref boolOrNull);
			// Line 128: ((TQString / DQString))?
			do {
				la0 = LA0;
				if (la0 == '"')
					goto match1;
				else if (la0 == '\'') {
					la1 = LA(1);
					if (la1 == '\'')
						goto match1;
				}
				break;
			match1:
				{
					var old_startPosition_10 = _startPosition;
					try {
						_startPosition = InputPosition;
						// Line 129: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"')
								value = TQString();
							else
								value = DQString();
						} else
							value = TQString();
						// line 131
						_type = TT.Literal;
						PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
						return ParseLiteral2(idtext, value.ToString(), false);
					} finally {
						_startPosition = old_startPosition_10;
					}
				}
			} while (false);
			// line 136
			return boolOrNull != NoValue.Value ? boolOrNull : (Symbol) idtext;
		}
	
		void IdContChar()
		{
			int la0;
			// Line 141: ( [#A-Z_a-z] | [0-9] | ['] &!(['] [']) )
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else if (la0 >= '0' && la0 <= '9')
				Skip();
			else {
				Match('\'');
				Check(!Try_IdContChar_Test0(0), "Did not expect ['] [']");
			}
		}
	
		bool Try_ScanIdContChar(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return ScanIdContChar();
		}
		bool ScanIdContChar()
		{
			int la0;
			// Line 141: ( [#A-Z_a-z] | [0-9] | ['] &!(['] [']) )
			la0 = LA0;
			if (Number_set0.Contains(la0)){
				if (!TryMatch(Number_set0))
					return false;}
			else if (la0 >= '0' && la0 <= '9'){
				if (!TryMatchRange('0', '9'))
					return false;}
			else {
				if (!TryMatch('\''))
					return false;
				if (Try_IdContChar_Test0(0))
					return false;
			}
			return true;
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
	
		void NormalId()
		{
			int la0;
			Match(Number_set0);
			// Line 143: greedy(IdContChar)*
			for (;;) {
				la0 = LA0;
				if (NormalId_set0.Contains(la0))
					IdContChar();
				else if (la0 == '\'') {
					if (!Try_IdContChar_Test0(1))
						IdContChar();
					else
						break;
				} else
					break;
			}
		}
	
		UString IdCore(ref object boolOrNull)
		{
			int la0;
			UString result = default(UString);
			// Line 146: (BQString | NormalId)
			la0 = LA0;
			if (la0 == '`') {
				bool parseNeeded;
				BQString(out parseNeeded);
				// line 146
				result = ParseStringValue(parseNeeded, false);
			} else {
				NormalId();
				// line 148
				result = Text();
				if (result == "true") {
					_type = TT.Literal;
					boolOrNull = G.BoxedTrue;
				}
				if (result == "false") {
					_type = TT.Literal;
					boolOrNull = G.BoxedFalse;
				}
				if (result == "null") {
					_type = TT.Literal;
					boolOrNull = null;
				}
			}
			return result;
		}
	
		void LettersOrPunc()
		{
			Skip();
		}
	
		object SpecialLiteral()
		{
			int la0;
			Skip();
			Skip();
			LettersOrPunc();
			// Line 159: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 159
			return ParseAtAtLiteral(Text());
		}
	
		object Shebang()
		{
			int la0;
			Check(InputPosition == 0, "Expected InputPosition == 0");
			Skip();
			Skip();
			// Line 164: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 164: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
			// line 165
			return WhitespaceTag.Value;
		}
	
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2, la3;
			object value = default(object);
			// Line 170: (Spaces / &{InputPosition == _lineStartAt} [.] [\t ] => DotIndent)?
			la0 = LA0;
			if (la0 == '\t' || la0 == ' ')
				Spaces();
			else if (la0 == '.') {
				if (InputPosition == _lineStartAt) {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ')
						DotIndent();
				}
			}
			// line 172
			_startPosition = InputPosition;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 178: ( Shebang / SpecialLiteral / [`] => Id / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / SQOperator / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / [@] / Keyword / Operator )
			do {
				switch (LA0) {
				case '#':
					{
						la1 = LA(1);
						if (la1 == '!') {
							// line 178
							_type = TT.Shebang;
							value = Shebang();
						} else
							goto matchId;
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '@') {
							la2 = LA(2);
							if (SQOperator_set0.Contains(la2)) {
								// line 179
								_type = TT.Literal;
								value = SpecialLiteral();
							} else
								goto match21;
						} else
							goto match21;
					}
					break;
				case '`':
					{
						// line 180
						_type = TT.BQId;
						value = Id();
					}
					break;
				case 'A': case 'B': case 'C': case 'D':
				case 'E': case 'F': case 'G': case 'H':
				case 'I': case 'J': case 'K': case 'L':
				case 'M': case 'N': case 'O': case 'P':
				case 'Q': case 'R': case 'S': case 'T':
				case 'U': case 'V': case 'W': case 'X':
				case 'Y': case 'Z': case '_': case 'a':
				case 'b': case 'c': case 'd': case 'e':
				case 'f': case 'g': case 'h': case 'i':
				case 'j': case 'k': case 'l': case 'm':
				case 'n': case 'o': case 'p': case 'q':
				case 'r': case 's': case 't': case 'u':
				case 'v': case 'w': case 'x': case 'y':
				case 'z':
					goto matchId;
				case '\n': case '\r':
					{
						// line 182
						_type = TT.Newline;
						value = Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 183
							_type = TT.SLComment;
							value = SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 != -1) {
									// line 184
									_type = TT.MLComment;
									value = MLComment();
								} else
									value = Operator();
							} else
								value = Operator();
						} else
							value = Operator();
					}
					break;
				case '-':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto matchNumber;
							else
								value = Operator();
						} else
							value = Operator();
					}
					break;
				case '0': case '1': case '2': case '3':
				case '4': case '5': case '6': case '7':
				case '8': case '9':
					goto matchNumber;
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else if (Number_set0.Contains(la1)) {
							if (InputPosition < 2 - 1 || !Try_ScanIdContChar(1 - 2)) {
								// line 199
								_type = TT.Keyword;
								value = Keyword();
							} else
								value = Operator();
						} else
							value = Operator();
					}
					break;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"') {
								la3 = LA(3);
								if (la3 != -1)
									goto matchTQString;
								else
									goto matchDQString;
							} else
								goto matchDQString;
						} else
							goto matchDQString;
					}
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'') {
								la3 = LA(3);
								if (la3 != -1)
									goto matchTQString;
								else
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (la1 == '\\') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 == '\'')
									goto matchSQString;
								else
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r')) {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQString;
							else
								goto matchSQOperator;
						} else
							goto matchSQOperator;
					}
				case ',':
					{
						// line 190
						_type = TT.Comma;
						Skip();
						// line 190
						value = sy__apos_comma;
					}
					break;
				case ';':
					{
						// line 191
						_type = TT.Semicolon;
						Skip();
						// line 191
						value = sy__apos_semi;
					}
					break;
				case '(':
					{
						// line 192
						_type = TT.LParen;
						Skip();
						// line 192
						_brackStack.Add(_type);
					}
					break;
				case ')':
					{
						// line 193
						_type = TT.RParen;
						Skip();
						// line 193
						if (_brackStack.Count > 1)
							_brackStack.Pop();
					}
					break;
				case '[':
					{
						// line 194
						_type = TT.LBrack;
						Skip();
						// line 194
						_brackStack.Add(_type);
					}
					break;
				case ']':
					{
						// line 195
						_type = TT.RBrack;
						Skip();
						// line 195
						if (_brackStack.Count > 1)
							_brackStack.Pop();
					}
					break;
				case '{':
					{
						// line 196
						_type = TT.LBrace;
						Skip();
						// line 196
						_brackStack.Add(_type);
					}
					break;
				case '}':
					{
						// line 197
						_type = TT.RBrace;
						Skip();
						// line 197
						if (_brackStack.Count > 1)
							_brackStack.Pop();
					}
					break;
				case '!': case '$': case '%': case '&':
				case '*': case '+': case ':': case '<':
				case '=': case '>': case '?': case '^':
				case '|': case '~':
					value = Operator();
					break;
				default:
					{
						Skip();
						// line 201
						_type = TT.Unknown;
					}
					break;
				}
				break;
			matchId:
				{
					// line 181
					_type = TT.Id;
					value = Id();
				}
				break;
			matchNumber:
				{
					// line 185
					_type = TT.Literal;
					value = Number();
				}
				break;
			matchTQString:
				{
					// line 186
					_type = TT.Literal;
					value = TQString();
				}
				break;
			matchDQString:
				{
					// line 187
					_type = TT.Literal;
					value = DQString();
				}
				break;
			matchSQString:
				{
					// line 188
					_type = TT.Literal;
					value = SQString();
				}
				break;
			matchSQOperator:
				{
					// line 189
					_type = TT.SingleQuoteOp;
					value = SQOperator();
				}
				break;
			match21:
				{
					// line 198
					_type = TT.At;
					Skip();
					// line 198
					value = sy__aposx40;
				}
			} while (false);
			// line 203
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, value);
		}
	
		public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 213: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
					goto stop;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '"')
								goto stop;
							else
								Skip();
						} else if (la1 == -1)
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				default:
					Skip();
					break;
				}
			}
		stop:;
			// Line 213: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 213
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 213
				return true;
			}
		}
	
		public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 216: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
					goto stop;
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '\'')
								goto stop;
							else
								Skip();
						} else if (la1 == -1)
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				default:
					Skip();
					break;
				}
			}
		stop:;
			// Line 216: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 216
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 216
				return true;
			}
		}
	
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 219: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 219
							nested--;
						} else if (la1 != -1)
							goto match4;
						else
							break;
					} else {
						la1 = LA(1);
						if (la1 == '*')
							goto match4;
						else if (la1 == '/') {
							if (!Try_MLCommentLine_Test0(1))
								goto match4;
							else
								break;
						} else if (la1 != -1)
							goto match4;
						else
							break;
					}
				} else if (la0 == '/') {
					la1 = LA(1);
					if (la1 == '*') {
						Skip();
						Skip();
						// line 220
						nested++;
					} else
						Skip();
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
				continue;
			match4:
				{
					Skip();
					Check(!Try_MLCommentLine_Test0(0), "Did not expect [/]");
				}
			}
			// Line 224: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 224
				return false;
			} else {
				Match('*');
				Match('/');
				// line 224
				return true;
			}
		}
		static readonly HashSet<int> HexNumber_Test0_set0 = NewSetOfRanges('+', '+', '-', '-', '0', '9');
	
		private bool Try_HexNumber_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return HexNumber_Test0();
		}
		private bool HexNumber_Test0()
		{
			if (!Scan_HexDigits())
				return false;
			if (!TryMatch('P', 'p'))
				return false;
			if (!TryMatch(HexNumber_Test0_set0))
				return false;
			return true;
		}
	
		private bool Try_IdContChar_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return IdContChar_Test0();
		}
		private bool IdContChar_Test0()
		{
			if (!TryMatch('\''))
				return false;
			if (!TryMatch('\''))
				return false;
			return true;
		}
	
		private bool Try_MLCommentLine_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return MLCommentLine_Test0();
		}
		private bool MLCommentLine_Test0()
		{
			if (!TryMatch('/'))
				return false;
			return true;
		}
	} ;
}	// braces around the rest of the file are optional