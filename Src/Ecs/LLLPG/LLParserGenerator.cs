﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Collections;
using Loyc.CompilerCore;
using Loyc.Collections.Impl;
using Loyc.Utilities;
using ecs;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

#if false
	TODO: use this table as a test suite for the parser

	General rules:
	- Variables and fields use #var(type, name1, name2(initial_value), name3)
	  Properties use #property(name, type, #{ body; }) instead.
	  The parser treats "var x" as #var(var, x), but #var(#missing, x) is canonical.
	- All spaces have the form #spacekind(name, #(inherited_types), #{body});
	  the third argument is omitted if the body is omitted.
	  e.g. #struct(Point<#T>, #(IPoint), #{ public int X, Y; });
	- Methods, operators and constructors use #def(retType, name, #(args), #{body});
	  the body can be omitted, or replaced with #==>(target) for forwarding.
	  "if" and "where" clauses are attached as #if and #where attributes.
	  e.g. #def(#double, Square, #(double x), #{ return x*x; });

	Standard EC# statements: Declarations       Prefix notation
	-------------------------------------       ---------------
	using System.Collections.Generic;           #import(System.Collections.Generic);
	using System { Linq, Text };                #import (System.Linq, System.Text);
	using Foo = Bar;                            [#fileLocal] #alias(Foo = Bar);
	extern alias Z;                             #extern_alias(Z);
	[assembly:Attr]                             [Attr] #assembly;
	case 123:                                   #case(123);
	default:                                    #label(#default);
	label_name:                                 #label(label_name);
	int x = 0;                                  #var(int, x(0));
	int* a, b = &x, c;                          #var(#*(int), a, b(&x), c);
	public partial class Foo<T> : IFoo {}       [#public, #partial] #class(Foo<T>, #(IFoo), {});
	struct Foo<\T> if default(T) + 0 is legal   [#if(default(T) + 0 is legal)] #struct(Foo<\T>, #missing, {});
	enum Foo : byte { A = 1, B, C, Z = 26 }     #enum(Foo, byte, #(A = 1, B, C, Z = 26));
	trait Foo<\T> : Stream { ... }              #trait(Foo<\T>, #(Stream), {...});
	interface Foo<T> : IEnumerable<T> { ... }   #interface(Foo<T>, #(IEnumerable<T>), {...});
	namespace Foo<T> { ... }                    #namespace(Foo<T>, #missing, {...});
	namespace Foo<T> { ... }                    #namespace(Foo<T>, #missing, {...});
	alias Map<K,V> = Dictionary<K,V>;           #alias(Foo<T> = Bar<T>);
	alias Foo = Bar : IFoo { ... }              #alias(Foo<T> = Bar<T>, #(IFoo), { ... });
	event EventHandler Click;                   #event(EventHandler, Click);
	event EventHandler A, B;                    #event(EventHandler, A, B));
	event EventHandler A { add { } remove { } } #event(EventHandler, A, { add({ }); remove({ }); }));
	delegate void foo<T>(T x) where T:class,X   [#where(T, #class, X)] #delegate(foo<T>, #(T x), void);
	public new partial string foo(int x);       [#public, #partial, #new] #def(#string, foo, #(int x));
	int foo(int x) => x * x;                    #def(int, foo, #(int x), { x * x; });
	int foo(int x) { return x * x; }            #def(int, foo, #(int x), { #return(x * x); });
	def foo(int x) ==> bar;                     [#def] #def(#missing, foo, #(int x), #==>(bar));
	int Foo { get; set; }                       #property(int, Foo, { get; set; })
	IEnumerator IEnumerable.GetEnumerator() { } #def(IEnumerator, IEnumerable.GetEnumerator, #(), { });
	new (int x) : this(x, 0) { y = x; }         #def(#missing, #new, #(int x), { #this(x, 0); y = x; });
	Foo (int x) : base(x) { y = x; }            #def(#missing, Foo,  #(int x), { #base(x); y = x; });
	~Foo () { ... }                             #def(#missing, #~(Foo), #(), { ... });
	static bool operator==(T a, T b) { ... }    [#static] #def(#bool, [#operator] #==, #(T a, T b), { ... });
	static implicit operator A(B b) { ... }     [#static, #implicit] #def(A, [#operator] #cast, #(B b), { ... });
	static explicit operator A<T><\T>(B<T> b);  [#static, #explicit] #def(A<T>, [#operator] #of<#cast, \T>, #(B<T> b));
	bool operator `when`(Cond cond) { ... }     #def(#bool, [#operator] when, #(Cond cond), { ... });

	Standard EC# statements: Executable         Prefix notation
	-----------------------------------         ---------------
	if (c) f();                                 #if(c, f());
	if (c) { f(); }                             #if(c, { f(); });
	if (c) a = 1, b = 2;                        #if(c, #(a = 1, b = 2));
	if (c) f(); else { g(); }                   #if(c, f(), { g() });
	for (int x = 0; x * y < 100; x++) f(x);     #for(#var(int, x(0)), x * y < 100, x++, f(x));
	foreach (var x in list) { ... }             #foreach(#var(var, x), list, { ... }) // not "#in(#var(var, x), list)" because that's unparsable
	while (x > 0) { ... }                       #while(x > 0, { ... })
	switch (c) { case '+', '-': goto default;   #switch(c, { #case('+', '-'); #goto(#default);
	             default: break; }                           #default; #break; }
	checked { ... }                             #checked({ ... })
	unchecked { ... }                           #unchecked({ ... })
	using (d = new Form()) { ... }              #using(d = new Form(), { ... })
	using (IDisposable d = new Form()) { ... }  #using(#var(IDisposable, d(new Form()), { ... })
	


	EC# expressions       Prefix notation            EC# expressions       Prefix notation 
	---------------       ---------------            ---------------       ---------------
	foo (or @foo)         foo                        a + b          
	food.pizza.cheese     #.(food, pizza, cheese)     
	.foo                  #.(foo)                     
	foo<A, B>             #of(foo, A, B)              
	operator ==           #operator(#==)              
	int                   #int                       
	int x = 0             #var(#int, x(0))           
    foo()::x              #:::(foo(), x)               
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      

#endif

	/// <summary>A helper class for generating input nodes to the parser generator.</summary>
	/// <remarks>
	/// I was going to use this to bootstrap the EC# parser, but I'm not sure if 
	/// it's necessary now; I should probably just create predicates directly (<see cref="Pred"/>).
	/// </remarks>
	public class PGFactory : GreenFactory
	{
		public PGFactory() : base(EmptySourceFile.Unknown) { }
		public PGFactory(ISourceFile file) : base(file) { }

		public static readonly Symbol _Gate = GSymbol.Get("#=>");
		public static readonly Symbol _Star = GSymbol.Get("#*");
		public static readonly Symbol _Plus = GSymbol.Get("#+");
		public static readonly Symbol _Opt = GSymbol.Get("#?");
		public static readonly Symbol _Nongreedy = GSymbol.Get("nongreedy");
		public static readonly Symbol _Greedy = GSymbol.Get("greedy");
		public static readonly Symbol _rule = GSymbol.Get("rule");
		public static readonly Symbol _token = GSymbol.Get("token");

		public GreenNode Any { get { return Symbol(GSymbol.Get("_")); } } // represents any terminal

		public GreenNode Rule(string name, params GreenAtOffs[] sequence)
		{
			return Def(Symbol(_rule), GSymbol.Get(name), ArgList(), Braces(sequence));
		}
		public GreenNode Seq(params GreenAtOffs[] sequence) { return Call(S.Tuple, sequence); }
		public GreenNode Seq(params char[] sequence) { return Call(S.Tuple, sequence.Select(c => (GreenAtOffs)_(c)).ToArray()); }
		public GreenNode Star(params GreenAtOffs[] sequence) { return Call(_Star, sequence); }
		public GreenNode Plus(params GreenAtOffs[] sequence) { return Call(_Plus, sequence); }
		public GreenNode Opt(params GreenAtOffs[] sequence)  { return Call(_Opt,  sequence); }
		public GreenNode Nongreedy(GreenNode loop) { return Greedy(loop, false); }
		public GreenNode Greedy(GreenNode loop, bool greedy = true)
		{
			Debug.Assert(loop.Name == _Star || loop.Name == _Plus || loop.Name == _Opt);
			return Call(greedy ? _Greedy : _Nongreedy, loop);
		}
		public  GreenNode And(params GreenAtOffs[] sequence)  { return Call(S.AndBits, AutoS(sequence)); }
		public  GreenNode AndNot(params GreenAtOffs[] sequence) { return Call(S.Not, AutoS(sequence)); }
		public  GreenNode AndCode(params GreenAtOffs[] sequence) { return Call(S.AndBits, Code(sequence)); }
		public  GreenNode Code(params GreenAtOffs[] statements) { return Call(S.Braces, statements); }
		private GreenNode AutoS(GreenAtOffs[] sequence)
		{
			return sequence.Length == 1 ? sequence[0].Node : Seq(sequence);
		}
		public GreenNode _(char c)
		{
			return Literal(c);
		}
	}

	/// <summary>Encapsulates LLLPG, the Loyc LL Parser Generator, which generates
	/// LL(k) recursive-descent parsers.</summary>
	/// <remarks>
	/// Note: the input to LLLPG is usually provided in the form of EC# source code.
	/// In that case, there is no need to use this class directly.
	/// <para/>
	/// LLLPG is a new LL(k) parser generator under the umbrella of the Loyc 
	/// project. The Loyc project is not formally announced, however, so forget I
	/// said anything.
	/// <para/>
	/// LLLPG generates recursive-descent parsers for LL(k) grammars. It is 
	/// designed for parsing computer languages, not natural languages. It also
	/// it supports "syntactic predicates" which are zero-width syntactic 
	/// assertions, and "semantic predicates" which are arbitrary expressions. 
	/// What all this basically means is that it generates the kind of parsing code 
	/// that people would write by hand, but it takes out the tedious work involved.
	/// Below, I will assume you already know what "grammars", "lexers", "tokens" 
	/// and "parsers" are, but that you are not familiar with the fine details. 
	/// I will briefly explain what LL(k) means in the next section.
	/// <para/>
	/// LLParserGenerator generates parsers in the form of an EC# Loyc tree. TODO:
	/// make a tool for generating text output.
	/// <para/>
	/// An LLLPG grammar consists of a set of "rules" that can refer to each other.
	/// Each rule defines a sub-parser--a parser for just that rule alone--but in
	/// general, LLLPG interprets rules in the context of the whole grammar, so 
	/// parsers are not necessarily independent; more on that later. Here is a 
	/// simple example of an LLLPG lexer (lexers are also known as tokenizers or 
	/// scanners):
	/// <code>
	/// using System;
	/// using Loyc.LLPG;
	/// 
	/// [[LLParser]]
	/// class Tokenizer
	/// {
	///   public token Id ==> #
	///     [ ('@' { _verbatim=1; })? NormalIdStart NormalIdCont*
	///     | '@' { _verbatim=1; } SqString
	///     ];
	///   rule IdStart ==> #[ Letter | '_' ];
	///   rule IdCont  ==> #[ IdStart | ('0'..'9') ];
	///   rule Letter  ==> #[ 'a'..'z' | 'A'..'Z' | &{Char.IsLetter(LA0)} . ];
	///   bool _verbatim;
	/// }
	/// </code>
	/// The first part, <c>[[LLParser]] class Tokenizer</c> tells LLLPG to 
	/// generate a class called Tokenizer based on the specified set of rules.
	/// If the <c>[[LLParser]]</c> tag is not present, LLLPG will ignore the class 
	/// and write it unchanged into the output file.
	/// <para/>
	/// As you can see, the syntax of a rule is
	/// <code>
	///   rule rule_name ==> #[ description of the syntax of the rule ];
	/// </code>
	/// and instead of "rule" you can use the word "token"; "token", which is used 
	/// to mark tokens in a lexer (as opposed to parser rules or partial tokens), 
	/// tells LLLPG that the rule can be followed by anything, and that ambiguities 
	/// with the exit branch should be ignored (in technical terms, the follow set 
	/// is <c>(_|$)*</c>, whereas a normal rule starts with a follow set of "end
	/// of file" or nothing at all, if the rule is marked "private".) Pay no
	/// attention to the strange delimiter <c>#[ ... ]</c>; it has something to do 
	/// with the Loyc project, which, as I was saying, doesn't really exist yet. 
	/// Remember the First Rule of Fight Club!
	/// <para/>
	/// Each rule is translated directly to a method in the class. In this example,
	/// the <c>Tokenizer</c> class will have <c>Id()</c>, <c>IdStart()</c>, 
	/// <c>IdCont()</c>, and <c>Letter()</c> methods, each of which will parse
	/// the sub-grammar described by the corresponding rule. <c>Id()</c> will be
	/// a public method, the others will be private, and _verbatim is an ordinary
	/// boolean variable; LLLPG does not understand the declaration of _verbatim 
	/// and will pass it to the output file unmodified.
	/// <para/>
	/// The syntax inside the square brackets is a variation of the EBNF notation.
	/// Here is a brief overview:
	/// <ul>
	/// <li>A character constant such as <c>'x'</c> causes that character to be 
	/// consumed from the input. An identifier preceded by a dollar sign, such as 
	/// <c>$foo</c>, is used in a parser to represent a token of type foo 
	/// (typically the lexer will have a corresponding rule with the same name).
	/// In parsing jargon, 'x' and $foo are also known as "terminals", which 
	/// means "elements of the input stream". The dot character "<c>.</c>" 
	/// represents any single terminal (not including end-of-file).</li>
	/// <li>Identifiers such as <c>Foo</c> refer to other rules in the same 
	/// grammar. References to other rules are also known as "nonterminals", 
	/// which means, er, "not terminals".</li>
	/// <li>Ranges like <c>'0'..'9'</c> match a range of characters (unicode code 
	/// points--well, for now it matches UTF-16 code points, sorry.) 
	/// <li>Negative sets are also allowed using the ~ prefix, e.g. ~'0'..'9' 
	/// matches any character that is not a digit, and ~$foo matches any token
	/// that is not of type "foo". <c>~</c> can only be used on terminals; use
	/// <c>&!foo .</c> for nonterminals (see below).</li>
	/// <li>The suffix <c>*</c> means "zero or more of those" and <c>+</c> means
	/// "one or more". For example, <c>'0'..'9'+</c> matches one or more digits.</li>
	/// <li>When you put these elements side-by-side, they form a sequence.</li>
	/// <li>The suffix <c>?</c> means optional, e.g. '$'? Id means "optional 
	/// dollar sign, then Id".</li>
	/// <li>The <c>|</c> operator separates alternatives. For example, 
	/// <c>'0'..'9'|'_'</c> means "a digit or an underscore". Please note that 
	/// <c>a b | c</c> means <c>(a b) | c</c>, not <c>a (b | c)</c>.</li>
	/// <li>The & prefix indicates a "zero-width assertion", also known as an 
	/// "and-predicate". It can be followed by source code in braces, for example
	/// <c>&{char.IsLetter(LA0)} .</c> means "run the C# expression 
	/// <c>char.IsLetter(LA0)</c> and if the result is true, consume any character".
	/// <c>&</c> can also be followed by syntax. For example, imagine that Number 
	/// is a rule that matches input such as <c>2.3</c>, <c>-10</c> and 
	/// <c>+15.4</c>. Then <c>&('+'|'-') Number</c> means "check if the input 
	/// could be a <c>Number</c> <b>and also</b> check that it starts with '-' or 
	/// '+'." Thus, & can narrow down the scope of acceptable input.</li>
	/// <li>The <c>&!</c> prefix is the opposite of <c>&</c>; the condition after 
	/// <c>&!</c> must be false. For example, <c>&!'0' Number</c> matches a
	/// Number that does not start with '0'.</li>
	/// <li><c>a => b</c> is called a "gate", and is typically used for 
	/// optimization. It is an advanced feature and will be described later.</li>
	/// <c>At any point you can C# code in <c>{ curly braces }</c>, which are 
	/// actions to be taken as the parser parses.</c></li>
	/// </ul>
	/// 
	/// <h2>LL(k) parsing and how it compares to the alternatives</h2>
	/// 
	/// LLLPG is in the LL(k) family of parser generators. It is suitable for writing
	/// both lexers (also known as tokenizers or scanners) and parsers, but not for 
	/// writing one-stage parsers that combine lexing and parsing into one step. It 
	/// is more powerful (or at least easier to use) than LL(1) parser generators 
	/// such as Coco/R.
	/// <para/>
	/// Parser generators and hand-written parsers based on LL(k) are very popular.
	/// There are two other popular families of parser generators, based on LALR(1)
	/// and PEGs:
	/// <ul>
	/// <li>LALR(1) parsers are simplified LR(1) parsers which--wait, no, I'm not 
	///     going to explain what LALR(1) is because it would take a huge amount 
	///     of space. Suffice it to say that LALR(1) parsers support left-recursion 
	///     and use table-based lookups that are impractical to write by hand, i.e.
	///     a parser generator is always required, unlike LL(k) grammars which are
	///     straightforward to write by hand and therefore straightforward to 
	///     understand. They support neither a superset nor a subset of LL(k) 
	///     grammars. I have the impression that LALR is an evolutionary dead end:
	///     I've never heard of an LALR(2) or LALR(k) parser generator. Meanwhile,
	///     regardless of merit, LR(k) for k>1 just aren't very popular.</li>
	/// <li>PEGs are recursive-descent parsers with syntactic predicates, so PEG
	///     grammars are potentially very similar to grammars that you would use 
	///     with LLLPG. However, PEGs do not use prediction, meaning that they don't
	///     try to figure out in advance what the input means; for example, if the
	///     input is "42", a PEG parser does not look at the '4' and decide "oh, 
	///     that looks like a number, so I'll call the number sub-parser". Instead, 
	///     a PEG parser simply "tries out" each option starting with the first, 
	///     until one of them successfully parses the input. Without a prediction 
	///     step, PEG parsers apparently require memoization for efficiency (that 
	///     is, a memory of failed and successful matches at each input character,
	///     to avoid repeating the same work over and over in different contexts).
	///     PEGs usually combine lexing (tokenization) and parsing into a single 
	///     grammar (although it is not required), while other kinds of parsers 
	///     separate lexing and parsing into independent stages.</li>
	/// </ul>
	/// Other kinds of parser generators also exist, but are less popular. As I
	/// was saying, the main difference between LL(k) and its closest cousin, the 
	/// PEG, is that LL(k) parsers use prediction and LL(k) grammars usually suffer
	/// from ambiguity, while PEGs do not use prediction and the definition of PEGs
	/// pretends that ambiguity does not exist because it has a well-defined system
	/// of prioritization.
	/// <para/>
	/// "Prediction" means figuring out which branch to take before it is taken.
	/// In a "plain" LL(k) parser (without and-predicates), the parser makes a 
	/// decision and "never looks back". For example, when parsing the following
	/// LL(1) grammar:
	/// <code>
	/// public rule Tokens ==> #[ Token* ];
	/// public rule Token  ==> #[ Float | Id ];
	/// token Float        ==> #[ '0'..'9'* '.' '0'..'9'+ ];
	/// token Id           ==> #[ IdStart IdCont* ];
	/// rule  IdStart      ==> #[ 'a'..'z' | 'A'..'Z' | '_' ];
	/// rule  IdCont       ==> #[ IdStart | '0'..'9' ];
	/// </code>
	/// The <c>Token</c> method will get the next input character (known as
	/// <c>LA(0)</c> or lookahead zero), check if it is a digit or '.', then call 
	/// <c>Float</c> if so or <c>Id</c> otherwise. If the input is something like 
	/// "42", which does not match the definition of <c>Float</c>, the problem will 
	/// be detected by the <c>Float</c> method, not by <c>Token</c>, and the parser 
	/// cannot back up and try something else. If you add a new <c>Int</c> rule:
	/// <code>
	/// ...
	/// public rule Token ==> #[ Float | Int | Id ];
	/// token Float       ==> #[ '0'..'9'* '.' '0'..'9'+ ];
	/// token Int         ==> #[ '0'..'9'+ ];
	/// token Id          ==> #[ IdStart IdCont* ];
	/// ...
	/// </code>
	/// Now you have a problem, because the parser potentially requires infinite 
	/// lookahead to distinguish between <c>Float</c> and <c>Int</c>. By default,
	/// LLLPG uses LL(2), meaning it allows at most two characters of lookahead. 
	/// With two characters of lookahead, it is possible to tell that input like 
	/// "1.5" is Float, but it is not possible to tell whether "42" is a Float or 
	/// an Int without looking at the third character. Thus, this grammar is 
	/// ambiguous in LL(2), even though it is unambiguous when you have infinite 
	/// lookahead. The parser will handle single-digit integers fine, but given
	/// a two-digit integer it will call <c>Float</c> and then produce an error
	/// because the expected '.' was missing.
	/// <para/>
	/// A PEG parser does not have this problem; it will "try out" Float first 
	/// and if that fails, the parser backs up and tries Int next.
	/// <para/>
	/// Although LLLPG is designed to parse LL(k) grammars, it handles ambiguity 
	/// similarly to a PEG: if <c>A|B</c> is ambiguous, the parser will choose A
	/// by default because it came first, but it will also warn you about the 
	/// ambiguity.
	/// <para/>
	/// Since the number of leading digits is unlimited, LLLPG will consider this 
	/// grammar ambiguous no matter how high your maximum lookahead <c>k</c> (as 
	/// in LL(k)) is. You can resolve the conflict by combining Float and Int into 
	/// a single rule:
	/// <code>
	/// public rule Tokens ==> #[ Token* ];
	/// public rule Token  ==> #[ Number | Id ];
	/// token Number       ==> #[ '.' '0'..'9'+
	///                         | '0'..'9'+ ('.' '0'..'9'+)? ];
	/// token Id           ==> #[ IdStart IdCont* ];
	/// ...
	/// </code>
	/// Unfortunately, it's a little tricky sometimes to merge rules correctly.
	/// In this case, the problem is that <c>Int</c> always starts with a digit
	/// but <c>Float</c> does not. My solution was to separate out the case
	/// of "no leading digits" into a separate "alternative" from the "has leading 
	/// digits" case. That's the only best solution I can think of; others have
	/// pitfalls. For example, if you write:
	/// <code>
	/// token Number      ==> #[ '0'..'9'* ('.' '0'..'9'+)? ];
	/// </code>
	/// You'll have a problem because this matches an empty input, or it matches
	/// "hello" without consuming any input. Therefore, LLLPG will complain that 
	/// Token is "nullable" (meaning, it can succeed without consuming any input)
	/// and therefore must not be used in a loop (<c>Token*</c>). After all, if 
    /// you call Number in a loop and it doesn't match anything, you'll have an 
    /// infinite loop which is very bad.
	/// <para/>
	/// You can actually prevent it from matching an empty input as follows:
	/// <code>
	/// token Number      ==> #[ &('0'..'9'|'.')
	///                          '0'..'9'* ('.' '0'..'9'+)? ];
	/// </code>
	/// This means that the number must start with '0'..'9' or '.'.
	/// Now <c>Number()</c> cannot possibly match an empty input. Unfortunately,
	/// LLLPG is not smart enough to tell that it cannot match an empty input;
	/// it does not currently analyze and-predicates at all, so it doesn't 
	/// understand the effect caused by <c>&('0'..'9'|'.')</c>. Consequently it
	/// will still complain that <c>Token</c> is nullable even though it isn't.
	/// Hopefully this will be fixed in a future version, when I or someone 
	/// smart has time to figure out how to perform the analysis.
	/// <para/>
	/// Another approach is
	/// <code>
	/// token Number      ==> #[ {bool dot=false;}
	///                          ('.' {dot=true;})?
	///                          '0'..'9'+ (&{!dot} '.' '0'..'9'+)?
	///                        ];
	/// </code>
	/// Here I have created a "dot" flag which is set to "true" if the first 
	/// character is a dot. Later, the sequence <c>'.' '0'..'9'+</c> is only 
	/// allowed if the "dot" flag has been set. This approach works correctly;
	/// however, you must exercise caution when using &{...} because &{...} blocks
	/// may execute earlier than you might expect them to; this is explained 
	/// below.
	/// <para/>
	/// I mentioned that PEGs can combine lexing and parsing in a single grammar 
	/// because they effectively support unlimited lookahead. To demonstrate why 
	/// LL(k) parsers usually can't combine lexing and parsing, imagine that you 
	/// want to parse a program that supports variable assignments like "x = 0" 
	/// and function calls like x(0), something like this:
	/// <code>
	/// // "Id" means identifier, LParen means left parenthesis '(', etc.
	/// // For now, don't worry about what "#[" means. It's really not important.
	/// rule Expr    ==> #[ Assign | Call | ... ];
	/// rule Assign  ==> #[ Id Equals Expr ];
	/// rule Call    ==> #[ Id LParen ArgList ];
	/// rule ArgList ...
	/// ...
	/// </code>
	/// If the input is received in the form of tokens, then this grammar only 
	/// requires LL(2): the Expr parser just has to look at the second token to 
	/// find out whether it is Equals ('=') or LParen ('(') to decide whether to 
	/// call Assign or Call. However, if the input is received in the form of 
	/// characters, no amount of lookahead is enough! The input could be 
	/// something like
	/// <code>
	/// this_name_is_31_characters_long = 42;
	/// </code> 
	/// To parse this directly from characters, 33 characters of lookahead would
	/// be required (LL(33)), and of course, in principle, there is no limit to 
	/// the amount of lookahead. Besides, LLLPG is designed for small amounts of 
	/// lookahead like LL(2) or maybe LL(4); any double-digit value is usually a
	/// mistake. LL(33) could produce a ridiculously large and inefficient parser 
	/// (I'm too afraid to even try it.)
	/// <para/>
	/// In summary, LL(k) parsers are not as smart as PEG parsers, because they 
	/// are normally limited to k characters or tokens of lookahead, and k is 
	/// usually small. PEGs, in contrast, can always "back up" and try another 
	/// alternative when parsing fails. LLLPG makes up for this problem with 
	/// syntactic predicates, which allow unlimited lookahead, but you must insert 
	/// them yourself, so there is slightly more work involved and you have to pay 
	/// some attention to the lookahead issue. In exchange for this extra effort, 
	/// though, your parsers are likely to have good performance. I say "likely" 
	/// because I haven't been able to find any benchmarks comparing LL(k) parsers 
	/// to PEG parsers, but I've heard rumors that PEGs are slower, and intuitively 
	/// it seems to me that the memoization and retrying required by PEGs must have 
	/// some cost, it can't be free. Prediction is not free either, but since 
	/// lookahead has a strict limit, the costs usually don't get very high.
	/// <para/>
	/// It is also natural to compare LLLPG to <a href="http://antlr.org/">ANTLR</a>.
	/// LLLPG cannot handle the same variety of grammars as ANTLR because it does
	/// not support LL(*), a feature of ANTLR that allows it to scan ahead by an
	/// unlimited amount to choose which of multiple alternatives to take. And 
	/// then there's the new ANTLR 4, which I only found out about when I was 
	/// halfway through making LLLPG. ANTLR 4 apparently has some very fancy-pants 
	/// unlimited-lookahead parsing, which goes beyond even LL(*). I still happy 
	/// with LLLPG, though. Having limited lookahead may force you, the developer, 
	/// to do a little more work, but it also makes you more conscious of the 
	/// parsing process, which encourages you to write grammars that are more 
	/// efficient.
	/// <para/>
	/// It's kind of like the phenomenon that C code tends to be more efficient 
	/// than C++ code, even though C doesn't have any major features that C++ does
	/// not. It's not the features of C that make it efficient, rather it's the 
	/// lack of features: since C doesn't automate very much work, it encourages 
	/// the developer to minimize the amount of work that needs doing. It is also 
	/// more transparent; nothing happens automatically, so complex and costly 
	/// processes cannot be hidden the way they are in other languages.
	/// <para/>
	/// However, the "C" argument is really an excuse, because I actually don't 
	/// like C for that very reason: programming in C is too much work and too 
	/// error-prone. I would not argue that LLLPG is better than ANTLR, or even 
	/// "as good as" ANTLR, but I would argue that (unlike plain C or even C#) 
	/// LLLPG is good enough for most parsing jobs, and that it encourages you to 
	/// make efficient parsers by (1) forcing you to write grammars that resemble 
	/// the generated code, so it is easy to understand the cost of parsing, and 
	/// (2) complaining whenever the lookahead limit is exceeded. You can always 
	/// work around the lookahead limit, but you must do so explicitly, so the 
	/// parser won't do a lot of work without your knowledge.
	/// <para/>
	/// It's fair to ask why I created LLLPG when ANTLR already existed. It wasn't
	/// that I had any philosophical disagreement with ANTLR; it's just that the C#
	/// version of ANTLR was buggy to the point of being almost unusable (I have
	/// no idea if that's still the case) and lagged well behind the Java version 
	/// (and apparently still does). I also wasn't satisfied with the generated 
	/// code; I felt that it was longer, uglier and slower than necessary, and I
	/// didn't want to use ANTLR's runtime library (LLLPG does not require a 
	/// runtime library). Finally, some features such as gates didn't work the way 
	/// I thought they should. (by now I have forgotten how ANTLR behaves, so I 
	/// can't tell you what my objections were.)
	/// <para/>
	/// So for C# developers, the main benefits of LLLPG are that it (1) makes 
	/// efficient code, and (2) has first-class support for C# (and nothing else,
	/// for now.)
	/// <para/>
	/// LLLPG is also the flagship demo of EC# macros. When EC# is ready for public
	/// use, LLLPG will not be a standalone program like other parser generators, 
	/// it will merely be one of many domain-specific languages supported by 
	/// Enhanced C#. Because of this, no special work will be required to set up
	/// your build environment for LLLPG; LLLPG will be integrated into the language
	/// as seamlessly as "yield return", closures, LINQ, and other features of C#
	/// that we take for granted.
	/// <para/>
	/// It's better to use LLLPG than to write a parser by hand, because LL parsing 
	/// requires a lot of cross-rule knowledge to work correctly, and is therefore 
	/// error-prone. For instance, to implement a parser for the following grammar 
	/// by hand...
	/// <code>
	/// public rule Token  ==> #[ ID | SQString | DQString | CodeOpenQuote ];
	/// rule ID            ==> #[ '@'? IDStartChar IDContChar* ];
	/// rule IDStartChar   ==> #[ 'a'..'z'|'A'..'Z'|'_' ];
	/// rule IDContChar    ==> #[ IDStartChar|'0'..'9' ];
	/// rule DQString      ==> #[ '@' '"' nongreedy(_)* '"' 
	///                         | '"' (~('"'|'\n'|'\r'))* '"' ];
	/// rule SQString      ==> #[ '\'' nongreedy(.)* '\'' ];
	/// rule CodeOpenQuote ==> #[ '@' '{' ];
	/// </code>
	/// your code for Token() must check the first lookahead character, LA(0), to
	/// figure out which of the branches to take. If that character is '@', it must
	/// check LA(1) also. Thus, Token must have intimate knowledge of each of the
	/// other rules it calls. In this case it is not too much work, but this 
	/// example is only a lexer (a.k.a. a tokenizer); parsers often get far more 
	/// complicated.
	/// <para/>
	/// A more subtle difficulty for LL parsing is that rules may have to know 
	/// about their callers. Here's a very simple example:
	/// <code>
	/// // Comma-separated value file
	/// public rule CSVFile ==> #[ Line* ];
	/// rule Line           ==> #[ Field (',' Field)* EOL ];
	/// rule EOL            ==> #[ ('\r' '\n'?) | '\n' ];
	/// rule Field          ==> #[ nongreedy(_)*
	///                          | '"' ('"' '"' | ~('\n'|'\r'))* '"' ];
	/// </code>
	/// This grammar describes a file filled with comma-separated values. Notice 
	/// that 'Field' has the loop <c>nongreedy(_)*</c>. The underscore means "any 
	/// character", <c>(_)*</c> means "any sequence of characters", and 
	/// <c>nongreedy</c> means "break out of the loop at the first opportunity."
	/// How does it know to when to break out of the loop? Because LLLPG computes 
	/// the "follow set" or "return address" of each rule. In this case, 'Field' 
	/// can be followed by ','|'\n'|'\r', so the loop will break as soon as one 
	/// of these characters is encountered.
	/// <para/>
	/// Thus, LLParserGenerator's main job is to generate "prediction code", code
	/// that makes decisions in advance about which branch to take. It also 
	/// generates "matching code"--the code that actually consumes the input--but
	/// this code is very simple and could easily be written by hand.
	/// 
	/// <h3>How to use the LLParserGenerator class</h3>
	/// 
	/// LLLPG generates a parser for a set of <see cref="Rule"/> objects. Each rule 
	/// represents a sub-parser, whose job is to parse a single predicate 
	/// (<see cref="Pred"/> object). Many low-level details of the parsing process 
	/// can be customized (for now, customization is done by making a derived class 
	/// and/or writing a new implementation of <see cref="IPGTerminalSet"/>; 
	/// perhaps I'll invent simpler extension mechanisms later).
	/// <para/>
	/// To use this class, first call <see cref="AddRules"/> or <see cref="AddRule"/> 
	/// to input a list of interconnected rules (the rules can be built from EC# 
	/// source code <see cref="Node"/>s, or created in <see cref="Rule"/> objects 
	/// and added directly). Also, you can set properties such as 
	/// <see cref="DefaultK"/> to configure default behavior of the generator.
	/// <para/>
	/// Then call <see cref="GenerateCode"/> to generate the parser.
	/// 
	/// <h3>How LLParserGenerator works internally</h3>
	/// 
	/// See <see cref="GenerateCode"/> for more information about that. By the way,
	/// for some reason I wasn't in a very LINQy mood when I wrote this class. Lots 
	/// of old-fashioned for loops in here.
	/// 
	/// <h3>Using and-predicates</h3>
	/// 
	/// TODO
	/// ...
	/// Consider this scenario:
	/// <code>
	/// bool flag = false;
	/// public rule Paradox ==> #[ 'x' | {flag = true;} &{flag} 'x' ];
	/// </code>
	/// What will the value of 'flag' be after you call <c>Paradox()</c>? Since
	/// both branches are the same ('x'), the grammar is ambiguous, and the only 
	/// way LLLPG can make a decision is by running the expression {flag}. But
	/// the semantic actions {flag=false;} and {flag=true;} execute <i>after</i>
	/// prediction, so {flag} actually runs first even though it appears to come
	/// after {flag=true;}. You can clearly see this when you look at the actual
	/// generated code:
	/// <code>
	///   public void Paradox()
	///   {
	///     if (!(flag)) {
	///       Match('x');
	///     } else {
	///       flag = true;
	///       Match('x');
	///     }
	///   }
	/// </code>
	/// What happened? Well, LLLPG doesn't bother to read LA(0) because it won't
	/// help make a decision. So the usual prediction step is replaced with a test
	/// of the and-predicate &{flag}, and then the matching code runs (<c>'x'</c>
	/// for the left branch and <c>{flag = true;} 'x'</c> for the right branch).
	/// <para/>
	/// TODO: Warning: It is bad style to put an action block {...} before a &{...} 
	/// and-predicate because the and-predicate will be called before the action.
	/// </remarks>
	public partial class LLParserGenerator : PGFactory
	{
		public const int EOF = PGIntSet.EOF_int;

		#region Tests
		void Seq()
		{
			
		}
		#endregion

		Dictionary<Symbol, Rule> _rules = new Dictionary<Symbol,Rule>();
		HashSet<Rule> _tokens = new HashSet<Rule>();
		public int DefaultK = 2;
		public int FollowSetK = 2;
		public int TokenFollowSetK = 1;
		public bool NoDefaultArm = false;

		/// <summary>Called when an error or warning occurs while parsing a grammar
		/// or while generating code for a parser.</summary>
		/// <remarks>The parameters are (1) a Node that represents the location of 
		/// the error, or Node.Missing if the grammar was created programmatically 
		/// without any source code backing it; (2) a predicate related to the error, 
		/// or null if the error is a syntax error; (3) $Warning for a warning or 
		/// $Error for an error; and (4) the text of the error message.</remarks>
		public event Action<Node, Pred, Symbol, string> OutputMessage;

		protected static Symbol Warning = GSymbol.Get("Warning");
		protected static Symbol Error = GSymbol.Get("Error");
		private void Output(Node node, Pred pred, Symbol type, string msg)
		{
			if (OutputMessage != null)
				OutputMessage(node, pred, type, msg);
		}

		#region Step 1: AddRules() and related

		public LLParserGenerator() { }

		public Dictionary<Symbol, Rule> AddRules(Node stmtList)
		{
			_rules = new Dictionary<Symbol, Rule>();
			foreach (var stmt in stmtList.Args)
			{
				if (stmt.Calls(S.Def, 4))
				{
					bool isToken;
					var name = stmt.Args[0].Name;
					if ((isToken = name == _token) || stmt.Args[0].Name == _rule) try
						{
							var body = stmt.Args[3];
							var expr = body.Args[body.Args.Count - 1];
							var rule = new Rule(expr, name, NodeToPred(expr), stmt.TryGetAttr(S.Public) != null) { IsToken = isToken };
							AddRule(rule);
						}
						catch (Exception ex)
						{
							Console.WriteLine("ConvertRule failed: " + ex.Message);
						}
				}
			}
			return _rules;
		}
		public void AddRules(IEnumerable<Rule> rules)
		{
			foreach (var rule in rules)
				AddRule(rule);
		}
		public void AddRule(Rule rule)
		{
			_rules.Add(rule.Name, rule);
			if (rule.IsToken)
				_tokens.Add(rule);
		}

		enum Context { Rule, GateLeft, GateRight, And };

		private Pred NodeToPred(Node expr, Context ctx = Context.Rule)
		{
			if (expr.IsCall)
			{
				bool slash = false, not, orTerminals;
				if (expr.Calls(S.DotDot, 2) && expr.Args[0].IsLiteral && expr.Args[1].IsLiteral)
				{
					object v0 = expr.Args[0].Value, v1 = expr.Args[1].Value;
					if (v0 is char && v1 is char)
						return new TerminalPred(expr, (char)v0, (char)v1);
				}
				else if (expr.CallsMin(S.Tuple, 1))
				{
					// sequence: (a, b, c)
					if (expr.Calls(S.Tuple, 1))
						return NodeToPred(expr.Args[0], ctx);
					return ArgsToSeq(expr, ctx);
				}
				else if ((orTerminals = expr.Calls(S.OrBits, 2)) || expr.Calls(S.Or, 2) || (slash = expr.Calls(S.Div, 2)))
				{
					// alternatives: a | b, a || b, a / b
					var left = NodeToPred(expr.Args[0], ctx);
					var right = NodeToPred(expr.Args[1], ctx);
					var lt = AsTerminalSet(left);
					var rt = AsTerminalSet(right);
					if (lt != null && rt != null && lt.CanMerge(rt))
						return lt.Merge(rt);
					else
					{
						//if (orTerminals)
						//	throw new ArgumentException("Cannot use '{0}' as an argument to '|' because it is not a terminal set. Use '||' instead.",
						//		expr.Args[lt != null ? 1 : 0].ToString());
						return new Alts(expr, left, right, slash);
					}
				}
				else if (expr.CallsMin(_Star, 1) || expr.CallsMin(_Plus, 1) || expr.CallsMin(_Opt, 1))
				{
					// loop (a`+`, a`*`) or optional (a`?`)
					var type = expr.Name;
					bool? greedy = null;
					bool g;
					Pred subpred = null;
					if (expr.ArgCount == 1)
					{ // +, * or ? had only one argument (usual case)
						expr = expr.Args[0];
						if ((g = expr.CallsMin(_Greedy, 1)) || expr.CallsMin(_Nongreedy, 1))
						{
							greedy = g;
							slash = true; // ignore ambiguous
							if (expr.Args.Count == 1)
								subpred = NodeToPred(expr.Args[0], ctx);
							else
								subpred = ArgsToSeq(expr, ctx);
						}
						else
							subpred = NodeToPred(expr, ctx);
					}
					else
					{
						subpred = ArgsToSeq(expr, ctx);
					}

					if (type == _Opt)
						return new Alts(expr, LoopMode.Opt, subpred) { Greedy = greedy };
					if (type == _Plus)
					{
						var seq = new Seq(expr);
						seq.List.Add(subpred);
						seq.List.Add(new Alts(expr, LoopMode.Star, subpred));
						return seq;
					}
					return new Alts(expr, LoopMode.Star, subpred) { Greedy = greedy };
				}
				else if (expr.Calls(_Gate, 2))
				{
					if (ctx == Context.GateLeft || ctx == Context.GateRight)
						throw new ArgumentException(Localize.From(
							"Cannot use a gate ('{0}') inside another gate", expr));
					return new Gate(expr, NodeToPred(expr.Args[0], Context.GateLeft),
										  NodeToPred(expr.Args[1], Context.GateRight));
				}
				else if ((not = expr.Calls(S.Not, 1)) || expr.Calls(S.AndBits, 1))
				{
					expr = expr.Args[0];
					var subpred = AutoNodeToPred(expr, Context.And);
					var subpred2 = subpred as AndPred;
					if (subpred2 != null)
					{
						subpred2.Not ^= not;
						return subpred2;
					}
					else
						return new AndPred(expr, subpred, not);
				}
			}
			else
			{
				// Non-call
				while (expr.Head != null) // eliminate parenthesis
					expr = expr.Head;
				if (expr.IsLiteral && expr.Value is char)
					return new TerminalPred(expr, (char)expr.Value);
				if (expr.IsSimpleSymbol)
					return new RuleRef(expr, _rules[expr.Name]);
			}
			throw new ArgumentException("Unrecognized expression '{0}'", expr.ToString());
		}
		private Seq ArgsToSeq(Node expr, Context ctx)
		{
			var objs = expr.Args.Select(node => AutoNodeToPred(node, ctx)).ToList();
			Seq seq = new Seq(expr);
			Node action = null;
			for (int i = 0; i < objs.Count; i++)
			{
				if (objs[i] is Node)
				{
					var code = objs[i] as Node;
					if (ctx == Context.And || ctx == Context.GateLeft)
						throw new ArgumentException(Localize.From(ctx == Context.And ?
							"Cannot use an action block ('{0}') inside an '&' or '!' predicate; these predicates are for prediction only." :
							"Cannot use an action block ('{0}') on the left side of a '=>' gate; the left side is for prediction only.", objs[i].ToString()));
					action = Pred.AppendAction(action, code);
				}
				else // Pred
				{
					Pred pred = (Pred)objs[i];
					pred.PreAction = action;
					action = null;
					seq.List.Add(pred);
				}
			}
			if (action != null)
				seq.PostAction = action;
			return seq;
		}
		private object AutoNodeToPred(Node expr, Context ctx)
		{
			if (expr.CallsMin(S.Braces, 0))
				return expr; // code
			return NodeToPred(expr, ctx);
		}
		static TerminalPred AsTerminalSet(Pred pred)
		{
			if (pred is RuleRef)
				return AsTerminalSet(((RuleRef)pred).Rule.Pred);
			if (pred is TerminalPred)
				return (TerminalPred)pred;
			return null;
		}

		#endregion

		#region Step 2: DetermineFollowSets() and related

		static readonly Pred EndOfToken = new TerminalPred(null, TrivialTerminalSet.All, true);
		static readonly Pred EofAfterStartRule = new TerminalPred(null, TrivialTerminalSet.EOF, true);

		void DetermineFollowSets()
		{
			foreach (Rule rule in _rules.Values)
				new DetermineLocalFollowSets(this).Run(rule);

			// Each rule's Next is always an EndOfRule object, which has a list 
			// of things that could follow the rule elsewhere in the grammar.
			// To determine the follow set of each rule, me must find all places 
			// where the rule is used...
			new DetermineRuleFollowSets(_rules).Run();

			EndOfToken.Next = EndOfToken;
			EofAfterStartRule.Next = EofAfterStartRule;
			foreach (var rule in _rules.Values)
			{
				if (rule.IsToken) {
					rule.EndOfRule.FollowSet.Clear();
					rule.EndOfRule.FollowSet.Add(EndOfToken);
				} else if (rule.IsStartingRule)
					rule.EndOfRule.FollowSet.Add(EofAfterStartRule);
			}
		}

		/// <summary>Figures out the correct value of <see cref="Pred.Next"/> for 
		/// each sub-predicate in a rule.</summary>
		class DetermineLocalFollowSets : PredVisitor
		{
            LLParserGenerator LLPG;
            public DetermineLocalFollowSets(LLParserGenerator llpg) { LLPG = llpg; }

			TerminalPred AnyFollowSet = TerminalPred.AnyFollowSet();
            

			public void Run(Rule rule)
			{
				Visit(rule.Pred, rule.EndOfRule);
			}
			void Visit(Pred pred, Pred next)
			{
				pred.Next = next;
				pred.Call(this);
			}

			public override void Visit(Seq seq)
			{
				var next = seq.Next;
				for (int i = seq.List.Count - 1; i >= 0; i--)
				{
					Visit(seq.List[i], next);
					next = seq.List[i];
				}
			}
			public override void Visit(Alts alts)
			{
				var next = (alts.Mode == LoopMode.Star ? alts : alts.Next);

                if (next == alts) {
                    int badArm = alts.Arms.IndexWhere(arm => arm.IsNullable);
                    if (badArm > -1)
                        LLPG.Output(alts.Basis, alts, Error, string.Format(
                            "Arm #{0} of this loop is nullable; the parser could loop forever without consuming any input.", badArm+1));
                }

				for (int i = 0; i < alts.Arms.Count; i++)
					Visit(alts.Arms[i], next);
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Match, gate);
				Visit(gate.Predictor, AnyFollowSet);
			}
			public override void Visit(AndPred pred)
			{
				var child = pred.Pred as Pred;
				if (child != null)
					Visit(child, AnyFollowSet);
			}
		}

		/// <summary>Populates each rule's <see cref="EndOfRule.FollowSet"/> 
		/// according to the predicates that follow each reference to the rule 
		/// in the entire grammar.</summary>
		/// <remarks>Ignores the <see cref="Rule.IsToken"/> flag.</remarks>
		class DetermineRuleFollowSets : RecursivePredVisitor
		{
			private Dictionary<Symbol, Rule> _rules;
			public DetermineRuleFollowSets(Dictionary<Symbol, Rule> rules) { _rules = rules; }

			public void Run()
			{
				foreach (Rule rule in _rules.Values)
					rule.Pred.Call(this);
			}
			public override void Visit(RuleRef rref)
			{
				if (rref.Next is EndOfRule)
					rref.Rule.EndOfRule.FollowSet.UnionWith((rref.Next as EndOfRule).FollowSet);
				else
					rref.Rule.EndOfRule.FollowSet.Add(rref.Next);
			}
		}

		#endregion

		#region Step 3: code generation (simply calls GenerateCodeVisitor)

		protected ISourceFile _sourceFile;
		protected Node _classBody;

		/// <summary>Generates a parser for the grammar described by the rules 
		/// that have already been added to this object by calling 
		/// <see cref="AddRule"/> or <see cref="AddRules"/>.</summary>
		/// <param name="className"></param>
		/// <param name="sourceFile"></param>
		/// <returns>The generated parser class.</returns>
		/// <remarks>
		/// This method calls a couple of preprocessing steps before generating 
		/// code:
		/// <ol>
		/// <li><see cref="DetermineLocalFollowSets"/>: scans all predicates of all rules 
		///     and notifies each predicate of the predicate that follows it by setting 
		///     <see cref="Pred.Next"/>.</li>
		/// <li><see cref="DetermineRuleFollowSets"/>: scan all predicates of all rules 
		///     looking for <see cref="RuleRef"/>s. For each RuleRef found, add the value 
		///     of <see cref="Pred.Next"/> to the follow set of the rule to which it 
		///     refers (stored in <see cref="Rule.EndOfRule"/>).</li>
		/// </ol>
		/// The actual parser can then be generated. Generating code for sequences 
		/// like <c>'(' 'a'..'z' ')'</c> is trivial; by far the greatest difficulty 
		/// is generating prediction code when the grammar branches (<c>x | y | z</c>). 
		/// Since this class creates LL(k) parsers without memoization or implicit 
		/// backtracking, it relies on prediction trees to correctly decide <i>in 
		/// advance</i> which branch to follow.
		/// <para/>
		/// The following kinds of grammar elements require prediction:
		/// <para/>
		/// <ul>
		/// <li><c>a | b</c> (which is equivalent to <c>a / b</c>): prediction chooses between a and b</li>
		/// <li><c>a?</c>: prediction chooses between a and whatever follows a?</li>
		/// <li><c>a*</c>: prediction chooses between a and whatever follows a*</li>
		/// <li><c>(a | b)*: </c>prediction chooses between three alternatives (a, b, and exiting the loop).</li>
		/// <li><c>(a | b)?: </c>prediction chooses between three alternatives (a, b, and nothing)</c>.</li>
		/// <li><c>a+</c>: exactly equivalent to <c>a a*</c></li>
		/// </ul>
		/// Let's look at a simple example of the prediction code generated for a rule 
		/// called "Foo":
		/// <code>
		/// // rule a ==> #[ 'a' | 'A' ];
		/// // rule b ==> #[ 'b' | 'B' ];
		/// // public rule Foo ==> #[ a | b ];
		/// public void Foo()
		/// {
		///   var la0 = LA(0);
		///   if (la0 == 'a' || la0 == 'A')
		///     a();
		///   else
		///     b();
		/// }
		/// </code>
		/// By default, to make prediction more efficient, the last alternative is 
		/// assumed to match if the others don't. So when <c>a</c> doesn't match, <c>b</c>
		/// is called even though it has not been verified to match yet. This behavior
		/// can be changed by putting the <c>[MatchLastByDefault(false)]</c> attribute 
		/// on either a single rule or on the entire parser class (NOT IMPLEMENTED).
		/// Alternatively, you can select the default using the default(...) pseudo-
		/// function, e.g.
		/// <code>
		/// // public rule Foo ==> #[ default(a) | b ];
		/// public void Foo()
		/// {
		///   var la0 = LA(0);
		///   if (la0 == 'b' || la0 == 'B')
		///     b();
		///   else
		///     a();
		/// }
		/// </code>
		/// In general, the prediction and matching phases are physically separated:
		/// First an if-else chain does prediction, and then a second if-else chain
		/// reacts to the choice that was made. However, in simple cases like this one
		/// that only require LL(1) prediction, prediction and matching are merged 
		/// into a single if-else chain. The if-else statements are the prediction 
		/// part of the code, while the calls to a() and b() are the matching part.
		/// <para/>
		/// Here's another example:
		/// <code>
		/// // public rule Foo ==> #[ (a | b? 'c')* ];
		/// public void Foo()
		/// {
		///   for (;;) {
		///     var la0 = LA(0);
		///     if (la0 == 'a' || la0 == 'A')
		///       a();
		///     else if (la0 == 'b' || la0 == 'B' || la0 == 'c') {
		///       do {
		///         la0 = LA(0);
		///         if (la0 == 'b' || la0 == 'B')
		///           b();
		///       } while(false);
		///       Match('c');
		///     }
		///   }
		/// }
		/// </code>
		/// A kleene star (*) always produces a "for(;;)" loop, while an optional item
		/// always produces a "do ... while(false)" pseudo-loop. Here there are two
		/// separate prediction phases: one for the outer loop <c>(a | b? 'c')*</c>,
		/// and one for <c>b?</c>.
		/// <para/>
		/// In this example, the loop appears at the end of the rule. In some such 
		/// cases, the "follow set" of the rule becomes relevant. In order for the 
		/// parser to decide whether to exit the loop or not, it may need to know what 
		/// can follow the loop. For instance, if <c>('a' 'b')*</c> is followed by 
		/// <c>('a'..'z'+)</c>, it is not always possible to tell whether to stay in 
		/// the loop or exit just by looking at the first input character. If LA(0) is 
		/// 'a', it is necessary to look at the second character; only if the second 
		/// character is 'b' is it possible to conclude that 'a' 'b' should be matched.
		/// <para/>
		/// Therefore, before generating a parser one of the steps is to build the 
		/// follow set of each rule, by looking for places where a rule appears inside
		/// other rules. A rule is not aware of its current caller, so it gathers 
		/// information from all call sites. When a rule is marked "public", it is 
		/// considered to be a starting rule, which causes the follow set to include
		/// $ (which means "end of input").
		/// <para/>
		/// The worst-case follow set is <c>_* $</c> (anything), because essentially
		/// the prediction step must look ahead by the maximum number of characters or
		/// tokens (called k, as in "LL(k)") to decide whether to stay in the loop or 
		/// exit, and even after looking k characters ahead it is often impossible to
		/// disambiguate (in particular, the maximum lookahead is required when an arm
		/// contains a loop.)
		/// <para/>
		/// To mitigate this problem, there are separate "k" values for follow sets 
		/// and token follow sets. When generating a lexer, the follow set of any 
		/// token tends to be close to <c>_* $</c>, i.e. tokens can be almost anything.
		/// Specifically, in computer languages the acceptable input is almost the 
		/// entire ASCII character set, plus perhaps unicode characters that can be
		/// used as identifiers. The default <see cref="TokenFollowSetK"/> is 1, which
		/// means "only look ahead one character to decide whether to exit a loop".
		/// You can also set it to 0, which means "don't pay attention to the follow
		/// set at all, just use the minimum possible lookahead to figure out whether 
		/// the non-exit arms could possibly match."
		/// <para/>
		/// By the way, if the follow set is <c>_* $</c> and you use a nongreedy()
		/// loop, the loop will never execute since <c>_* $</c> matches anything and
		/// is always preferred.
		/// <para/>
		/// Here's an example that needs more than one character of lookahead:
		/// <code>
		/// // public rule Foo ==> #[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
		/// public void Foo()
		/// {
		///   int la0, la1;
		///   la0 = LA(0);
		///   int alt = 0;
		///   if (la0 == 'x') {
		///     la1 = LA(1);
		///     if (la1 >= '0' && '9' >= la1) {
		///       Match();
		///       Match();
		///       MatchRange('0', '9');
		///     } else
		///       alt = 1;
		///   } else
		///     alt = 1;
		/// 
		///   if (alt == 1) {
		///     Match();
		///     for (;;) {
		///       la0 = LA(0);
		///       if (la0 >= 'a' && 'z' >= la0)
		///         Match();
		///       else
		///         break;
		///     }
		///   }
		/// }
		/// </code>
		/// Here, the prediction and matching phases are merged for the second 
		/// alternative, but separate for the first alternative (because it is chosen 
		/// in two different places in the prediction logic). Notice that the matching 
		/// for alt 2 starts with <c>Match()</c> twice, with no arguments, but is 
		/// followed by <c>MatchRange('a', 'z')</c>. This demonstrates communication 
		/// between prediction and matching: the matching phase can tell that LA(0) is 
		/// confirmed to be 'x', and LA(1) is confirmed to be '0'..'9', so an 
		/// unconditional match suffices. However, nothing is known about LA(2) so its 
		/// value must be checked, which is what MatchRange() is supposed to do.
		/// <para/>
		/// In some cases, LA(0) is irrelevant. Consider this example:
		/// <code>
		/// // public rule Foo ==> #[ '(' 'a'..'z'* ')' | '(' '0'..'9'+ ')' ];
		/// public void Foo()
		/// {
		///   int la0, la1;
		///   la1 = LA(1);
		///   if (la1 >= 'a' && 'z' >= la1) {
		///     Match('(');
		///     for (;;) {
		///       la0 = LA(0);
		///       if (la0 >= 'a' && 'z' >= la0)
		///         Match();
		///       else
		///         break;
		///     }
		///     Match(')');
		///   } else {
		///     Match('(');
		///     MatchRange('0', '9');
		///     for (;;) {
		///       la0 = LA(0);
		///       if (la0 >= '0' && '9' >= la0)
		///         Match();
		///       else
		///         break;
		///     }
		///     Match(')');
		///   }
		/// }
		/// </code>
		/// Here, the first character of both alternatives is always '(', so looking at
		/// LA(0) doesn't help choose which branch to take, and prediction skips ahead
		/// to LA(1).
		/// 
		/// 
		/// --------
		/// <para/>
		/// 
		/// And predicates.
		/// 
		/// An and-predicate specifies an extra condition on the input that must be 
		/// checked. Here is a simple example:
		/// <code>
		/// (&{flag} '0'..'9' | 'a'..'z')
		/// </code>
		/// This example says that '0'..'9' is only allowed if the expression <c>flag</c>
		/// evaluates to true, otherwise 'a'..'z' is required. LLPG, however, gives
		/// and-predicates lower priority, and always inverts the order of the 
		/// testing: it checks for '0'..'9' first, then checks <c>flag</c> 
		/// afterward. I chose to make LLPG work this way because in general, and-
		/// predicates are more expensive to check than character sets; if one of
		/// the alternatives rarely runs, it would be wasteful to check an expensive
		/// and-predicate before checking if the input character could possibly 
		/// match. Therefore, the generated code looks like this:
		/// <code>
		/// la0 = LA(0);
		/// if (la0 >= '0' && la0 &lt;= '9') {
		///    Check(flag);
		///    Match();
		/// } else
		///    MatchRange('a', 'z');
		/// </code>
		/// If you really need to make the and-predicate run first for some reason,
		/// I dunno. I got nothin'. Complain to me every month until I implement 
		/// something, maybe.
		/// <para/>
		/// A generated parser performs prediction in two interleaved parts: 
		/// character-set tests, and and-predicate tests. In this example,
		/// <code>
		/// ('0'..'9'+ | &{hexAllowed} '0' 'x' ('0'..'9'|'a'..'f')+)
		/// </code>
		/// The code will look like this:
		/// <code>
		/// la0 = LA(0);
		/// if (la0 == '0') {
		///   if (hexAllowed) {
		///     la1 = LA(1);
		///     if (la1 == 'x') {
		///       Match();
		///       Match();
		///       MatchRange('0', '9', 'a', 'f');
		///       ...
		///     } else
		///       goto match1;
		///   } else
		///     goto match1;
		/// } else
		///   goto match1;
		/// goto done;
		/// match1:;
		/// {
		///   MatchRange('0', '9');
		///   ...
		/// }
		/// done:;
		/// </code>
		/// Here you can see the interleaving: first the parser checks LA(0), then 
		/// it checks the and-predicate, then it checks LA(1).
		/// <para/>
		/// LLPG (let's call it 1.0) does not support any analysis of the 
		/// <i>contents</i> of an and-predicate. Thus, without loss of generality,
		/// these examples use semantic predicates &{...} instead of syntactic 
		/// predicates &(...); LLPG doesn't care either way.
		/// <para/>
		/// Even without analyzing the contents of an and-predicate, they can still
		/// make prediction extremely complicated. Consider this example:
		/// <code>
		/// (.&{a} (&{b} {B();} | &{c})
		///   &{d} (&{e} ('e'|'E'))?
		///   (&{f} ('f'|'t') | 'F')
		/// | &{c} (&{f} ('e'|'t') | 'f') 'g'
		/// | '!' )
		/// </code>
		/// In this example, the first branch requires 'a' and 'd' to be true, 
		/// there's a pair of zero-width alternatives that require 'b' or 'c' 
		/// to be true, {B()} must be executed if 'b' is true, 'e' must be true 
		/// if LA(0) is ('e'|'E'), 'f' must be true if LA(0) is 'f' and no 
		/// condition is required for 'F'. The second branch also allows 'e' or
		/// 'f', provided that 'c' is true, but requires 'f' if LA(0) is 'e'. 
		/// The generated code looks like this:
		/// <code>
		/// la0 = LA(0);
		/// if (la0 == 'e' || la0 == 'f') {
		///   if (a && d) {
		///     if (c) {
		///       if (e && la0 == 'e') {
		///         if (b) {
		///           if (f) {
		///             la1 = LA(1);
		///             if (la1 == 'g')
		///               goto match2;
		///             else
		///               goto match1;
		///           } else
		///             goto match1;
		///         } else
		///     }
		///   } else
		///     goto match2;
		/// }
		/// 
		/// if (la0 == 'e' || la0 == 'f' || la0 == 't') {
		///   // Cases:
		///   // Alt 0: &abde [eE]            &be [e]
		///   //        &abdf [ft]            &bf [ft]
		///   //        &abd  [F]  - exclude!
		///   //        &acde [eE]            &be [e]
		///   //        &acdf [ft]            &cf [ft]
		///   //        &acd  [F]  - exclude!
		///   // Alt 1: &cf   [et]            &cf [et]
		///   //        &c    [f]             &c  [f]
		///   if (a && d) {
		///     if (b) {
		///       if (e && (la0 == 'e')) {
		///         if (c) {
		///           la1 = LA(1);
		///           if (la1 == 'g')
		///             goto match2;
		///           else
		///             goto match1;
		///         } else
		///           goto match1;
		///       } else if (f && la0 == 'f') {
		///         if (c) {
		///           la1 = LA(1);
		///           if (la1 == 'g')
		///             goto match2;
		///           else
		///             goto match1;
		///         } else
		///           goto match1;
		///       } else
		///           goto match2;
		///     } else if (e && la0 == 'e') {
		///       if (c) {
		///         la1 = LA(1);
		///         if (la1 == 'g')
		///           goto match2;
		///         else
		///           goto match1;
		///       } else
		///         goto match1;
		///     } else if (f && la0 == 'f') {
		///       if (c) {
		///         la1 = LA(1);
		///         if (la1 == 'g')
		///           goto match2;
		///         else
		///           goto match1;
		///       } else
		///         goto match1;
		///     } else
		///       goto match2;
		///   } else
		///     goto match2;
		/// } else if (la0 == 'E' || la0 == 'F') {
		/// } else {
		///   Match('!');
		/// }
		/// </code>
		/// 
		/// 
		/// When the character sets of two alternatives overlap and the grammar 
		/// contains and-predicates, the and-predicates are used to disambiguate.
		/// 
		/// 
		/// </remarks>
		public Node GenerateCode(Symbol className, ISourceFile sourceFile)
		{
			DetermineFollowSets();

			_sourceFile = sourceFile;

			// TODO use class body provided by user
			var F = new GreenFactory(_sourceFile);
			var greenClass = F.Attr(F.Public, F.Symbol(S.Partial), F.Call(S.Class, F.Symbol(className), F.List(), F.Braces()));
			var result = Node.FromGreen(greenClass, -1);
			_classBody = result.Args[2];

			_csg.Begin(_classBody, _sourceFile);

			var generator = new GenerateCodeVisitor(this);
			foreach(var rule in _rules.Values)
				generator.Generate(rule);
			
			_csg.Done();
			return result;
		}

		#endregion

		protected IPGCodeSnippetGenerator _csg = new PGCodeSnippetGenerator();
		public IPGCodeSnippetGenerator SnippetGenerator
		{
			get { return _csg; }
			set { _csg = value ?? new PGCodeSnippetGenerator(); }
		}

		#region Prediction analysis code
		// Helper code and data structures for GenerateCodeVisitor.ComputePredictionTree()

		// The int in each pair is the alt number: 0..Arms.Count and Arms.Count for exit
		protected KthSet[] ComputeFirstSets(Alts alts)
		{
			bool hasExit = alts.Mode != LoopMode.None;
			var firstSets = new KthSet[alts.ArmCountPlusExit];

			int i;
			for (i = 0; i < alts.Arms.Count; i++)
				firstSets[i] = ComputeNextSet(new KthSet(alts.Arms[i], i), false);
			var exit = i;
			if (hasExit)
				firstSets[exit] = ComputeNextSet(new KthSet(alts.Next, -1), true);
			if ((uint)alts.DefaultArm < (uint)alts.Arms.Count) {
				InternalList.Move(firstSets, alts.DefaultArm, firstSets.Length - 1);
				exit--;
			}
			if (!(alts.Greedy ?? true))
				InternalList.Move(firstSets, exit, 0);
			return firstSets;
		}
		protected KthSet[] ComputeNextSets(List<KthSet> previous)
		{
			var result = new KthSet[previous.Count];
			for (int i = 0; i < previous.Count; i++)
				result[i] = ComputeNextSet(previous[i], previous[i].Alt == -1);
			return result;
		}
		protected KthSet ComputeNextSet(KthSet previous, bool addEOF)
		{
			var next = new KthSet(previous);
			for (int i = 0; i < previous.Cases.Count; i++)
				_computeNext.Do(next, previous.Cases[i].Position);
			MakeCanonical(next);
			ConsolidateDuplicatePositions(next);
			next.UpdateSet(addEOF);
			return next;
		}

		/// <summary>Represents a location in a grammar: a predicate and a 
		/// "return stack" which is a so-called persistent singly-linked 
		/// list. This type is used within <see cref="Transition"/>.</summary>
		protected class GrammarPos : IEquatable<GrammarPos>
		{
			public GrammarPos(Pred pred, GrammarPos @return = null)
			{
				Debug.Assert(pred != null); 
				Pred = pred; 
				Return = @return;
			}
			public readonly Pred Pred;
			public readonly GrammarPos Return;

			public override string ToString() // for debugging
			{
				if (Return != null)
					return string.Format("{0} (=> {1})", Pred, Return);
				return Pred.ToString();
			}
			public bool Equals(GrammarPos other) { return Equals(this, other); }
			public static bool Equals(GrammarPos a, GrammarPos b)
			{
				return a == null ? b == null : a.Pred == b.Pred && Equals(a.Return, b.Return);
			}
			public override bool Equals(object obj)
			{
				return obj is GrammarPos && Equals(obj as GrammarPos);
			}
			public override int GetHashCode()
			{
				int hc = Pred.GetHashCode();
				if (Return != null)
					hc ^= Return.GetHashCode() * 13;
				return hc;
			}
		}
			
		/// <summary>Represents a position in a grammar (<see cref="GrammarPos"/>) 
		/// plus the set of characters that leads to that position from the previous 
		/// position. This is a single case in a <see cref="KthSet"/>.</summary>
		/// <remarks>
		/// For example, suppose the grammar is
		/// <code>
		///		rule X ==> #[ 'a' Y 'z' ];
		///		rule Y ==> #[ 'a'..'y' 'b'..'z' ];
		/// </code>
		/// If the previous position is represented by the dot in <c>'a'.Y 'z'</c>,
		/// i.e. before Y, then <see cref="ComputeNextSet"/> will compute a Transition
		/// with Set=[a-y] and Position pointing to <c>.'b'..'z'</c>, with a return 
		/// stack that points to <c>'a' Y.'z'</c>
		/// </remarks>
		protected class Transition : ICloneable<Transition>
		{
			public Transition(Pred prevPosition, IPGTerminalSet set, GrammarPos position) : this(prevPosition, set, InternalList<AndPred>.Empty, position) { }
			public Transition(Pred prevPosition, IPGTerminalSet set, InternalList<AndPred> andPreds, GrammarPos position)
			{
				PrevPosition = prevPosition;
				Debug.Assert(position != null);
				Set = set; 
				Position = position;
				AndPreds = andPreds;
			}
			public IPGTerminalSet Set;
			public InternalList<AndPred> AndPreds;
			public GrammarPos Position;
			public Pred PrevPosition; // null if there were multiple starting positions

			public override string ToString() // for debugging
			{
				if (AndPreds.Count > 0)
					return string.Format("{2} {0} => {1}", Set, Position, StringExt.Join("", AndPreds));
				else
					return string.Format("{0} => {1}", Set, Position);
			}
			public Transition Clone()
			{
 				return new Transition(PrevPosition, Set, AndPreds.CloneAndTrim(), Position);
			}
		}

		/// <summary>Represents the possible interpretations of a single input 
		/// character, in terms of transitions in the grammar.</summary>
		/// <remarks>
		/// For example, suppose the grammar is
		/// <code>
		///     rule @for ==> #[ #for ($id #in $collection | $id $`=` range) ]
		///     rule range ==> #[ start $`..` stop ]
		/// </code>
		/// If the starting position is right after #for, then <see cref="ComputeNextSet"/>
		/// will generate two cases, one at <c>$id.#in $collection</c> and 
		/// another at <c>$id.$`=` stop</c>. In both cases, the Set is $id, so
		/// <see cref="KthSet.Set"/> will also be $id.
		/// </remarks>
		protected class KthSet : ICloneable<KthSet>
		{
			public KthSet(KthSet prev)
			{
				Prev = prev;
				LA = prev.LA + 1;
				Alt = prev.Alt;
			}
			public KthSet(Pred start, int alt) {
				Cases.Add(new Transition(null, null, new GrammarPos(start)));
				Alt = alt;
			}
			public int LA = -1;
			public List<Transition> Cases = new List<Transition>();
			public IPGTerminalSet Set;    // Union of tokens in all cases
			public Set<AndPred> AndReq;   // Intersection of AndPreds in all cases
			public KthSet Prev;           // Previous lookahead level
			public bool HasAnyAndPreds { get { return Cases.Any(t => !t.AndPreds.IsEmpty); } }
			public int Alt;
				
			public void UpdateSet(bool addEOF)
			{
				if (Cases.Count == 0) {
					Set = TrivialTerminalSet.Empty;
					AndReq = new Set<AndPred>();
				} else {
					Set = Cases[0].Set;
					var andI = new HashSet<AndPred>(Cases[0].AndPreds);
					for (int i = 1; i < Cases.Count; i++) {
						Set = Set.Union(Cases[i].Set);
						andI.IntersectWith(Cases[i].AndPreds);
					}
					AndReq = new Set<AndPred>(andI);
				}
				if (addEOF)
					Set = Set.WithEOF();
			}
			public override string ToString() // for debugging
			{
				return string.Format("la{0} = {1} ({2})", LA, Set.ToString(), Cases.Select(c => c.Set).Join("|"));
			}
			public KthSet Clone()
			{
 				KthSet copy = new KthSet(Prev) { LA = LA, Set = Set, Alt = Alt };
				for (int i = 0; i < Cases.Count; i++)
					copy.Cases.Add(Cases[i].Clone());
				return copy;
			}
		}
			
		protected void MakeCanonical(KthSet next)
		{
			var cases = next.Cases;
			for (int i = 0; i < cases.Count; i++)
				cases[i].Position = _getCanonical.Do(cases[i].Position);
		}

		protected ComputeNext _computeNext = new ComputeNext();
		protected GetCanonical _getCanonical = new GetCanonical();
		
		/// <summary>Computes the "canonical" interpretation of a position for
		/// prediction purposes, so that <see cref="ConsolidateDuplicatePositions"/> 
		/// can detect duplicates reliably. Call <see cref="Do"/>() to use.</summary>
		protected class GetCanonical : PredVisitor
		{
			/// <summary>Computes the "canonical" interpretation of a position.</summary>
			/// <remarks>
			/// For example, given
			/// <code>
			///		rule X ==> #[ 'a' Y 'z' ];
			///		rule Y ==> #[ 'a'..'y' 'b'..'z' ];
			/// </code>
			/// The position before the sequence <c>'a' Y 'z'</c> is equivalent to 
			/// the position before 'a', so the result points to 'a' rather than to
			/// the sequence itself.
			/// <para/>
			/// The position after 'b'..'z' is equivalent to the position before 'z',
			/// if Y was called from X. Therefore, given the position after 'b'..'z'
			/// (a pointer to <see cref="EndOfRule"/>), and return address before 'z',
			/// this method returns the position before 'z'.
			/// </remarks>
			public GrammarPos Do(GrammarPos input)
			{
				_result = input.Pred;
				_return = input.Return;
				Visit(input.Pred);
				if (_result != input.Pred)
					return new GrammarPos(_result, _return);
				else
					return input;
			}
			Pred _result;
			protected GrammarPos _return; // may be null

			public override void Visit(Seq seq)
			{
				Visit(seq.List[0]); // btw, empty sequences should not happen
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Predictor);
			}
			public override void Visit(EndOfRule end)
			{
				if (_return != null) {
					var returnTo = _return.Pred;
					_return = _return.Return; // Return!
					Visit(returnTo);
				}
			}
			public override void VisitOther(Pred pred)
			{
				_result = pred;
			}
		}

		/// <summary>Gathers a list of all one-token transitions starting from a 
		/// single position. Also gathers any and-predicates that must be traversed
		/// before completing a transition.</summary>
		/// <remarks>
		/// For example, given
		/// <code>
		///		rule X ==> #[ 'x' Y '0'..'9' 'x' ];
		///		rule Y ==> #[.('y'? | Z) ];
		///		rule Z ==> #[ ('z' | '0'..'9' '0'..'9'*) ];
		/// </code>
		/// If the dot (.) represents the current position, then this class 
		/// computes the possible <see cref="Transition"/>s, which are as follows:
		/// <code>
		///     Transition.Set   Transition.Position
		///     'y'              rule Y ==> #[ ('y'? | Z).];                 (EndOfRule)
		///     '0'..'9'         rule X ==> #[ 'x' Y '0'..'9'.'x' ];         (TerminalPred)
		///     'z'              rule Z ==> #[ ('z' | '0'..'9' '0'..'9'*).]; (EndOfRule)
		///     '0'..'9'         rule Z ==> #[ ('z' | '0'..'9'.'0'..'9'*) ]; (Alts)
		/// </code>
		/// Notice that there can be duplicate sets--different destinations for the
		/// same input character. This means that there is an LL(1) ambiguity. The
		/// ambiguity may (or may not, depending on the situation) be resolved by 
		/// looking ahead further (it is the responsibility of 
		/// <see cref="GenerateCodeVisitor.ComputePredictionTree"/> to do so).
		/// <para/>
		/// This class is derived from GetCanonical just to inherit some code from it.
		/// <para/>
		/// What to do with and-predicates? It's a tricky question. And-predicates 
		/// are not used nearly as often as normal terminals and nonterminals, yet 
		/// they can produce the most complicated prediction code. Consider Alts
		/// such as:
		/// <code>
		/// ( ( &{a} {f();} | &{b} {g();} ) &{c}
		///   ( &{a} 'a' | &{x} 'b' | &{x} 'c')
		/// | &{x} ( 'a' | &{y} 'b' 'c' )
		/// )
		/// </code>
		/// It's enough to make your head explode. TODO.
		/// </remarks>
		protected class ComputeNext : GetCanonical
		{
			public void Do(KthSet result, GrammarPos position)
			{
                Debug.Assert(_stack.Count == 0);
                
                _result = result;
				_return = position.Return;
				_andPreds = null;
				Visit(position.Pred);
                
                Debug.Assert(_stack.Count == 0);
			}
            List<Pred> _stack = new List<Pred>(); // to detect infinite loops
			KthSet _result;

            public new void Visit(Pred pred) {
                if (_stack.Count > 5) {
                    // Detect and block infinite loops. One known cause
                    // of an infinite loop is a nullable item inside a
                    // loop, e.g. ('a'? 'b'?)*
                    if (_stack.Contains(pred))
                        return;
                }
                _stack.Add(pred);
                pred.Call(this);
                _stack.RemoveAt(_stack.Count - 1);
            }

			APChain _andPreds;
			class APChain {
				public AndPred Pred;
				public APChain Prev;
			}
			static void MakeListOfAndPreds(APChain chain, ref InternalList<AndPred> list)
			{
				if (chain != null) {
					MakeListOfAndPreds(chain.Prev, ref list);
					list.Add(chain.Pred);
				}
			}

			public override void Visit(TerminalPred term)
			{
				var apList = InternalList<AndPred>.Empty;
				MakeListOfAndPreds(_andPreds, ref apList);
				_result.Cases.Add(new Transition(term, term.Set, apList, new GrammarPos(term.Next, _return)));
			}
			public override void Visit(RuleRef rref)
			{
				var old = _return;
				_return = new GrammarPos(rref.Next, _return);
				Visit(rref.Rule.Pred);
				_return = old;
			}
			public override void Visit(Alts alts)
			{
				var saved = _andPreds;
				foreach (var pred in alts.Arms) {
					Visit(pred);
					_andPreds = saved;
				}
				if (alts.HasExit)
					Visit(alts.Next);
			}
			public override void Visit(AndPred and)
			{
				_andPreds = new APChain { Prev = _andPreds, Pred = and };
				Visit(and.Next); // skip
			}
			public override void Visit(EndOfRule end)
			{
				if (_return != null) {
					var returnTo = _return.Pred;
					_return = _return.Return; // Return!
					Visit(returnTo);
				} else {
					// Nowhere to return to? Use the follow set of the rule.
					foreach (var pred in end.FollowSet)
						Visit(pred);
				}
			}
		}

		/// <summary>Used by <see cref="ConsolidateDuplicatePositions"/>.</summary>
		class ConsolidationComparer : IEqualityComparer<Transition>
		{
			public static readonly ConsolidationComparer Value = new ConsolidationComparer();
			public bool Equals(Transition x, Transition y)
			{
				return x.Position.Equals(y.Position) &&
					x.AndPreds.AllEqual(y.AndPreds);
			}
			public int GetHashCode(Transition obj)
			{
				return obj.Position.GetHashCode();
			}
		}
		
		/// <summary>Different paths through a grammar can lead to the same place.
		/// This method is an optimization that merges duplicate cases. If we don't 
		/// do this, the number of cases can sometimes get very large, very quickly.</summary>
		static void ConsolidateDuplicatePositions(KthSet set)
		{
			if (set.Cases.Count <= 1)
				return;
			
			var unique = new Dictionary<Transition, Transition>(ConsolidationComparer.Value);
			for (int i = set.Cases.Count-1; i >= 0; i--) {
				Transition c = set.Cases[i], c0;
				if (!unique.TryGetValue(c, out c0))
					unique[c] = c;
				else {
					c0.Set = c.Set.Union(c0.Set);
					set.Cases.RemoveAt(i);
				}
			}
			Debug.Assert(unique.Count == set.Cases.Count);
		}

		#endregion

		public struct Set<T> : IEnumerable<T>
		{
			HashSet<T> _set;
			public Set(HashSet<T> set) { _set = set; }
			public HashSet<T> InternalSet { get { return _set; } }
			public HashSet<T> ClonedHashSet()
			{
				return _set == null ? new HashSet<T>() : new HashSet<T>(_set);
			}

			public static Set<T> operator |(Set<T> a, Set<T> b)
			{
				if (a._set == null) return new Set<T>(b._set);
				if (b._set == null) return new Set<T>(a._set);
				HashSet<T> r = a.ClonedHashSet();
				r.UnionWith(b._set);
				return new Set<T>(r);
			}
			public static Set<T> operator &(Set<T> a, Set<T> b)
			{
				if (a._set == null || b._set == null) return new Set<T>();
				HashSet<T> r = a.ClonedHashSet();
				r.IntersectWith(b._set);
				return new Set<T>(r);
			}
			public static Set<T> operator -(Set<T> a, Set<T> b)
			{
				if (a._set == null || b._set == null) return a;
				HashSet<T> r = a.ClonedHashSet();
				r.ExceptWith(b._set);
				return new Set<T>(r);
			}
			public bool IsEmpty
			{
				get { return _set == null || _set.Count == 0; }
			}
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			public IEnumerator<T> GetEnumerator()
			{
				return _set == null ? (IEnumerator<T>)EmptyEnumerator<T>.Value : (IEnumerator<T>)_set.GetEnumerator();
			}
			public bool Contains(T item)
			{
				return _set != null && _set.Contains(item);
			}
			public bool Overlaps(IEnumerable<T> items)
			{
				return _set != null && _set.Overlaps(items);
			}
			public bool SetEquals(Set<T> other)
			{
				bool e1 = IsEmpty, e2 = other.IsEmpty;
				return e1 == e2 ? e1 || _set.SetEquals(other._set) : false;
			}
		}
	}

	internal static class HashSetExt
	{
		public static HashSet<T> Clone<T>(HashSet<T> set) { return new HashSet<T>(set); }
		public static HashSet<T> Union<T>(this HashSet<T> self, HashSet<T> other) { 
			var set = new HashSet<T>(self);
			set.UnionWith(other);
			return set;
		}
		public static HashSet<T> Intersection<T>(this HashSet<T> self, HashSet<T> other) { 
			var set = new HashSet<T>(self);
			set.IntersectWith(other);
			return set;
		}

	}
	public class Rule
	{
		public readonly Node Basis;
		public readonly EndOfRule EndOfRule = new EndOfRule();

		public Rule(Node basis, Symbol name, Pred pred, bool isStartingRule) 
		{
			Basis = basis; Pred = pred; Name = name;
			if (IsStartingRule = isStartingRule) {
				var eof = new TerminalPred(null, -1);
				eof.Next = eof; // everything needs a "Next"
				EndOfRule.FollowSet.Add(eof);
			}
		}
		public readonly Symbol Name;
		public Pred Pred;
		public bool IsToken, IsStartingRule;
		public int K; // max lookahead; <= 0 to use default
		/// <summary>A function that takes a method body (braced block) and wraps it in a method.</summary>
		public Func<Rule, Node, Node> MethodCreator;
		/// <summary>Uses <see cref="MethodCreator"/> to wrap a method body (braced 
		/// block) into a method suitable for adding to a parser class. If 
		/// MethodCreator is null, a simple default method signature is 
		/// used, e.g. <c>public void R() {...}</c> where R is the rule name.</summary>
		/// <param name="parserCode">The parsing code that was generated for this rule.</param>
		/// <returns>A method.</returns>
		public Node CreateMethod(Node parserCode)
		{
			if (MethodCreator != null)
				return MethodCreator(this, parserCode);
			else {
				var F = new GreenFactory(parserCode.SourceFile);
				//   ruleMethod = @{ public void \(F.Symbol(rule.Name))() { } }
				Node ruleMethod = Node.FromGreen(F.Attr(F.Public, F.Def(F.Void, F.Symbol(this.Name), F.List())));
				ruleMethod.Args.Add(parserCode);
				return ruleMethod;
			}
		}

		public static Alts operator |(Rule a, Pred b) { return (Alts)((RuleRef)a | b); }
		public static Alts operator |(Pred a, Rule b) { return (Alts)(a | (RuleRef)b); }
		public static Alts operator |(Rule a, Rule b) { return (Alts)((RuleRef)a | (RuleRef)b); }
		public static Alts operator /(Rule a, Pred b) { return (Alts)((RuleRef)a / b); }
		public static Alts operator /(Pred a, Rule b) { return (Alts)(a / (RuleRef)b); }
		public static Alts operator /(Rule a, Rule b) { return (Alts)((RuleRef)a / (RuleRef)b); }
		public static implicit operator Rule(RuleRef rref) { return rref.Rule; }
		public static implicit operator RuleRef(Rule rule) { return new RuleRef(null, rule); }
	}

	/*
	 * token Id() {
	 *   (('@', {_verbatim=1;})`?`, NormalIdStart, NormalIdCont`*`) |
	 *   ('@', {_verbatim=1;}, SqString);
	 * }
	 * rule IdStart() { Letter | '_'; }
	 * rule IdCont()  { IdStart | '0'..'9'; }
	 * rule Letter()  { ('a'..'z') | ('A'..'Z') | (&{Char.IsLetter(LA0)}, _); }
	 * 
	 * 
	 * 
	 * rule goo() => { foo 'z' }
	 * rule foo() => { nongreedy(('a' | 'b') num | 'c')* ('c' | 'd') | 'a'+; }
	 * rule num() => { ('0'..'9')+; }
	 * 
	 * static readonly InputSet num_set0 = Range('0','9');
	 * void foo()
	 * {
	 *   int alt;
	 *   char LA0, LA1;
	 *   LA0 = LA(0);
	 *   if (LA0 == 'a') {
	 *     LA1 = LA(1);
	 *     if (LA1 >= '0' && LA1 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = 1;
	 *   } else
	 *     alt = 1;
	 *   
	 *   if (alt == 0) {
	 *     for (;;) { // for `?` use do...while(false)
	 *       LA0 = LA(0);
	 *       if (LA0 >= 'a' && LA0 <= 'b')
	 *         alt = 0;
	 *       else if (LA0 == 'c') {
	 *         if (LA0 == 'z')
	 *           break;
	 *         else
	 *           alt = 1;
	 *       } else
	 *         break;
	 *       
	 *       if (alt == 0) {
	 *         Consume(); // alt == 0 already implies 'a'..'b'; no check needed
	 *         num();
	 *       }
	 *     }
	 *   } else {
	 *     Consume();
	 *   }
	 * }
	 * void num()
	 * {
	 *   int alt;
	 *   char LA0;
	 *   LA0 = LA(0);
	 *   Match(LA0 >= '0' && LA0 <= '9', "0..9");
	 *   for (;;) {
	 *     LA0 = LA(0);
	 *     if (LA0 >= '0' && LA0 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = -1;
	 *     if (alt == 0)
	 *       Consume();
	 *     else
	 *       break;
	 *   }
	 * }
	 * 
	 * 
	 * Fun time! So hey, what would this less-ambiguous C alternative look like? D?
	 * - Juxtaposition operator is not possible in general because x - y, x `@` y
	 *   would be ambiguous: could be (x) (-y), (x `@`) (y)
	 * - In boo style, can allow arbitrary macro names without parens e.g. 
	 *       boo.foo (bar) - 1: ... 
	 *   Or if braces normally start child blocks:
	 *       boo.foo (bar) - 1 { ... }
	 *   In that case, need something else like {{ }} to make a scope mid-statement.
	 *   presence of ':' indicates that 'boo.foo' must be a macro name;
	 *   "assert (x) > (y)" can't work this way, but "assert: (x) > (y)" can.
	 *   Labels would need some other syntax such as 
	 * 
	 * Distinguishing argument lists from expressions: arg list when
	 * 1. beginning of statement, multiple words before parens, and/or
	 * 2. parens followed by =>
	 * a b c d(e < f, g > h, j < k, l < m >> m) => { ... }
	 * Note: constructors and destructors may look like exprs; assume fn call
	 *       and disambiguate in postprocessing step.
	 * Note: var decl in expression requires '=' to disambiguate, either right
	 *       after the type and name (int x = ...) or outside the parenthesis 
	 *       (x, int y, a[i]) = ...
	 */

}