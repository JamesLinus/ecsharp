﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Collections;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections.Impl;
using TK = Loyc.Syntax.Lexing.TokenKind;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// Converts a token list into a token tree. Everything inside brackets, parens
	/// or braces is made a child of the open bracket's Block.
	/// </summary>
	public class TokensToTree : ILexer
	{
		public TokensToTree(ILexer source, bool skipWhitespace)
			{ _source = source; _skipWhitespace = skipWhitespace; }

		ILexer _source;
		bool _skipWhitespace;
		bool _closerMatched;
		Token? _closer;

		public ISourceFile File
		{
			get { return _source.File; }
		}
		public Action<int, string> OnError
		{
			get { return _source.OnError; }
			set { _source.OnError = value; }
		}
		public int IndentLevel
		{
			get { return _source.IndentLevel; }
		}
		public int LineNumber
		{
			get { return _source.LineNumber; }
		}
		public void Reset()
		{
			_source.Reset();
		}

		Token? LLNextToken()
		{
			Token? t;
			if (_closer != null) {
				t = _closer;
				_closer = null;
				return t;
			}
			do
				t = _source.NextToken();
			while (_skipWhitespace && t != null && t.Value.IsWhitespace);
			return t;
		}

		public Token? NextToken()
		{
			_current = LLNextToken();
			if (_current == null)
				return null;

			TK tt = _current.Value.Kind;
			if (IsOpener(tt)) {
				var v = _current.Value;
				GatherChildren(ref v);
				return _current = v;
			} else
				return _current;
		}

		void GatherChildren(ref Token openToken)
		{
			Debug.Assert(openToken.Value == null);
			if (openToken.Value != null && openToken.Children != null)
				return; // wtf, it's already a tree

			TK ott = openToken.Kind;
			int oldIndentLevel = _source.IndentLevel;
			TokenTree children = new TokenTree(_source.File);

			for (;;) {
				Token? t = LLNextToken(); // handles LBrace, LParen, LBrack internally
				if (t == null) {
					OnError(openToken.StartIndex, Localize.From("Reached end-of-file before '{0}' was closed", openToken.ToString()));
					break;
				}
				TK tt = t.Value.Kind;
				if (IsOpener(tt)) {
					var v = t.Value;
					GatherChildren(ref v);
					children.Add(v);
					if (_closer != null && _closerMatched) {
						children.Add(_closer.Value);
						_closer = null;
					}
				} else if (IsCloser(tt)) {
					// indent must match dedent, '{' must match '}' (the parser 
					// can complain itself about "(]" and "[)" if it wants; we 
					// allow these to match because some languages might want it.)
					bool dentMismatch = (ott == TK.Indent) != (tt == TK.Dedent);
					if (dentMismatch || (ott == TK.LBrace) != (tt == TK.RBrace))
					{
						OnError(openToken.StartIndex, Localize.From("Opening '{0}' does not match closing '{1}' on line {2}", 
							openToken.ToString(), t.Value.ToString(), _source.IndexToLine(t.Value.StartIndex)));
						// - If dentMismatch and ott == TK.Indent, do not close.
						// - If dentMismatch and tt = TK.Dedent, close but do not match.
						// - If the closer is more indented than the opener, do not close.
						// - If the closer is less indented than the opener, close but do not match.
						// - If the closer is the same indentation as the opener, close and match.
						if (dentMismatch ? tt == TK.Dedent : IndentLevel <= oldIndentLevel) {
							// close
							_closer = t.Value;
							_closerMatched = !dentMismatch && (IndentLevel == oldIndentLevel);
							break;
						} else
							children.Add(t.Value); // do not close
					} else {
						_closer = t.Value;
						_closerMatched = true;
						break;
					}
				} else
					children.Add(t.Value);
			}
			openToken.Value = children;
		}

		private bool IsOpener(TK tt)
		{
			return tt >= TK.LParen && ((int)tt & 0x0100) == 0;
		}
		private bool IsCloser(TK tt)
		{
			return tt >= TK.LParen && ((int)tt & 0x0100) != 0;
		}

		Token? _current;
		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		bool System.Collections.IEnumerator.MoveNext()
		{
			NextToken();
			return _current.HasValue;
		}

		public SourcePos IndexToLine(int index)
		{
			return _source.IndexToLine(index);
		}
	}

}
