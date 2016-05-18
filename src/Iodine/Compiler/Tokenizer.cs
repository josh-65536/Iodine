/**
* Copyright (c) 2015, GruntTheDivine All rights reserved.

* Redistribution and use in source and binary forms, with or without modification,
* are permitted provided that the following conditions are met:
* 
*  * Redistributions of source code must retain the above copyright notice, this list
*    of conditions and the following disclaimer.
* 
*  * Redistributions in binary form must reproduce the above copyright notice, this
*    list of conditions and the following disclaimer in the documentation and/or
*    other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its contributors may be
* used to endorse or promote products derived from this software without specific
* prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
* SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
* TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
* BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
* CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
* ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
* DAMAGE.
**/

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Numerics;

namespace Iodine.Compiler
{
    /// <summary>
    /// Iodine lexer class, tokenizes our source into a list of Token objects represented as a TokenStream object.
    /// </summary>
    public sealed class Tokenizer
    {
        private int position;
        private int sourceLen;
        private string source;
        private string file;
        private string lastDocStr = null;
        private ErrorSink errorLog;
        private SourceLocation location;

        public Tokenizer (ErrorSink errorLog, string source, string file = "")
        {
            this.errorLog = errorLog;
            this.source = source;
            this.file = file;
            position = 0;
            sourceLen = source.Length;
            location = new SourceLocation (0, 0, file);
        }

        public IEnumerable<Token> Scan ()
        {
            List<Token> tokens = new List<Token> ();
            EatWhiteSpaces ();
            while (PeekChar () != -1) {
                Token nextToken = NextToken ();
                if (nextToken != null) {
                    tokens.Add (nextToken);
                    lastDocStr = null;
                }
                EatWhiteSpaces ();
            }

            if (errorLog.ErrorCount > 0) {
                throw new SyntaxException (errorLog);
            }
            return tokens;
        }

        private Token NextToken ()
        {
            char ch = (char)PeekChar ();
            switch (ch) {
            case '#':
                return ReadComment ();
            case '\'':
            case '"':
                return ReadStringLiteral ();
            case '_':
                return ReadIdentifier ();
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return ReadNumber ();
            case '+':
            case '-':
            case '*':
            case '/':
            case '=':
            case '<':
            case '>':
            case '~':
            case '!':
            case '&':
            case '^':
            case '|':
            case '%':
            case '@':
            case '?':
            case '.':
                return ReadOperator ();
            case '{':
                ReadChar ();
                return new Token (TokenClass.OpenBrace, "{", lastDocStr, location);
            case '}':
                ReadChar ();
                return new Token (TokenClass.CloseBrace, "}", lastDocStr, location);
            case '(':
                ReadChar ();
                return new Token (TokenClass.OpenParan, "(", lastDocStr, location);
            case ')':
                ReadChar ();
                return new Token (TokenClass.CloseParan, ")", lastDocStr, location);
            case '[':
                ReadChar ();
                return new Token (TokenClass.OpenBracket, "[", lastDocStr, location);
            case ']':
                ReadChar ();
                return new Token (TokenClass.CloseBracket, "]", lastDocStr, location);
            case ';':
                ReadChar ();
                return new Token (TokenClass.SemiColon, ";", lastDocStr, location);
            case ':':
                ReadChar ();
                return new Token (TokenClass.Colon, ":", lastDocStr, location);
            case ',':
                ReadChar ();
                return new Token (TokenClass.Comma, ",", lastDocStr, location);
            default:
                if (char.IsLetter (ch)) {
                    return ReadIdentifier ();
                }
                errorLog.Add (Errors.UnexpectedToken, location, (char)ReadChar ());
                
                return null;
            }
        }

        private Token ReadComment ()
        {
            int ch = 0;
            do {
                ch = ReadChar ();
            } while (ch != -1 && ch != '\n');

            return null;
        }

        private Token ReadNumber ()
        {
            StringBuilder accum = new StringBuilder ();
            char ch = (char)PeekChar ();
            if (ch == '0' && PeekChar (1) == 'x')
                return ReadHexNumber (accum);
            do {
                if (ch == '.')
                    return ReadFloat (accum);
                accum.Append ((char)ReadChar ());
                ch = (char)PeekChar ();
            } while (char.IsDigit (ch) || ch == '.');
            return new Token (TokenClass.IntLiteral, accum.ToString (), lastDocStr, location);
        }

        private Token ReadHexNumber (StringBuilder accum)
        {
            ReadChar (); // 0
            ReadChar (); // x
            while (IsHexNumber ((char)PeekChar ())) {
                accum.Append ((char)ReadChar ());
                if ((char)PeekChar (1) == 'L' && IsHexNumber ((char)PeekChar (1))) {
                    errorLog.Add (Errors.IllegalSyntax, location);
                }
            }

            string val = string.Empty;
            Int64 val64;
            string numstr = accum.ToString ();
            var big = (char)PeekChar (0) == 'L';
            if (big) {
                val = BigInteger.Parse ("0" + numstr, System.Globalization.NumberStyles.HexNumber).ToString ();
            } else if (Int64.TryParse (numstr, System.Globalization.NumberStyles.HexNumber, null, out val64)) {
                val = val64.ToString ();
            } else {
                // BigInteger works in mysterious ways.
                // FFFFFF doesn't parse, 0FFFFFF does.
                val = BigInteger.Parse ("0" + numstr, System.Globalization.NumberStyles.HexNumber).ToString ();
            }
            if (string.IsNullOrEmpty (val))
                errorLog.Add (Errors.IllegalSyntax, location);
            return new Token (TokenClass.IntLiteral, val, lastDocStr, location);
        }

        private static bool IsHexNumber (char c)
        {
            return "ABCDEFabcdef0123456789".Contains (c.ToString ());
        }

        private Token ReadFloat (StringBuilder buffer)
        {
            ReadChar (); // .
            buffer.Append (".");
            char ch = (char)PeekChar ();
            do {
                buffer.Append ((char)ReadChar ());
                ch = (char)PeekChar ();
            } while (char.IsDigit (ch));
            return new Token (TokenClass.FloatLiteral, buffer.ToString (), lastDocStr, location);
        }

        private Token ReadStringLiteral ()
        {
            StringBuilder accum = new StringBuilder ();
            int delimiter = ReadChar ();
            int ch = (char)PeekChar ();
            while (ch != delimiter && ch != -1) {
                if (ch == '\\') {
                    ReadChar ();
                    accum.Append (ParseEscapeCode ());
                } else {
                    accum.Append ((char)ReadChar ());
                }
                ch = PeekChar ();
            }
            if (ReadChar () == -1) {
                errorLog.Add (Errors.UnterminatedStringLiteral, location);
            }
            return new Token (ch == '"' ? 
                TokenClass.InterpolatedStringLiteral :
                TokenClass.StringLiteral,
                accum.ToString (),
                lastDocStr,
                location
            );
        }

        private Token ReadBinaryStringLiteral ()
        {
            StringBuilder accum = new StringBuilder ();
            int delimiter = ReadChar ();
            int ch = (char)PeekChar ();
            while (ch != delimiter && ch != -1) {
                if (ch == '\\') {
                    ReadChar ();
                    accum.Append (ParseEscapeCode ());
                } else {
                    accum.Append ((char)ReadChar ());
                }
                ch = PeekChar ();
            }
            if (ReadChar () == -1) {
                errorLog.Add (Errors.UnterminatedStringLiteral, location);
            }

            return new Token (TokenClass.BinaryStringLiteral,
                accum.ToString (),
                lastDocStr,
                location
            );
        }

        private char ParseEscapeCode ()
        {
            char escape = (char)ReadChar ();
            switch (escape) {
            case '\'':
                return '\'';
            case '"':
                return '"';
            case 'n':
                return '\n';
            case 'b':
                return '\b';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'f':
                return '\f';
            case '\\':
                return '\\';
            }
            errorLog.Add (Errors.UnrecognizedEscapeSequence, location);
            return '\0';
        }

        private Token ReadIdentifier ()
        {
            StringBuilder accum = new StringBuilder ();
            char ch = (char)PeekChar ();
            do {
                accum.Append ((char)ReadChar ());
                ch = (char)PeekChar ();
            } while (char.IsLetterOrDigit (ch) || ch == '_');

            string final = accum.ToString ();

            if (final == "b" && (ch == '\"' || ch == '\'')) {
                return ReadBinaryStringLiteral ();
            }

            switch (final) {
            case "if":
            case "else":
            case "while":
            case "do":
            case "for":
            case "func":
            case "class":
            case "use":
            case "self":
            case "foreach":
            case "in":
            case "true":
            case "false":
            case "null":
            case "lambda":
            case "try":
            case "except":
            case "break":
            case "from":
            case "continue":
            case "super":
            case "enum":
            case "raise":
            case "contract":
            case "trait":
            case "mixin":
            case "given":
            case "case":
            case "yield":
            case "default":
            case "return":
            case "match":
            case "when":
            case "var":
            case "with":
            case "global":
            case "extend":
                return new Token (TokenClass.Keyword, accum.ToString (), lastDocStr, location);
            case "is":
            case "isnot":
            case "as":
                return new Token (TokenClass.Operator, accum.ToString (), lastDocStr, location);
            default:
                return new Token (TokenClass.Identifier, accum.ToString (), lastDocStr, location);
            }
        }

        private Token ReadOperator ()
        {
            char op = (char)ReadChar ();
            string nextTwoChars = op + ((char)PeekChar ()).ToString ();
            string nextThreeChars = op + ((char)PeekChar ()).ToString () + ((char)PeekChar (1)).ToString ();

            switch (nextThreeChars) {
            case "/**":
                ReadChar ();
                ReadChar ();
                ReadDocComment ();
                return null;
            case "<<=":
                ReadChar ();
                ReadChar ();
                return new Token (TokenClass.Operator, nextThreeChars, lastDocStr, location);
            case ">>=":
                ReadChar ();
                ReadChar ();
                return new Token (TokenClass.Operator, nextThreeChars, lastDocStr, location);
            case "...":
                ReadChar ();
                ReadChar ();
                return new Token (TokenClass.Operator, nextThreeChars, lastDocStr, location);
            }

            switch (nextTwoChars) {
            case ".?":
                return new Token (TokenClass.MemberDefaultAccess, ".?", lastDocStr, location);
            case ">>":
            case "<<":
            case "&&":
            case "||":
            case "==":
            case "!=":
            case "=>":
            case "<=":
            case ">=":
            case "+=":
            case "-=":
            case "*=":
            case "/=":
            case "%=":
            case "^=":
            case "&=":
            case "|=":
            case "??":
            case "..":
                ReadChar ();
                return new Token (TokenClass.Operator, nextTwoChars, lastDocStr, location);
            case "/*":
                ReadChar ();
                ReadLineComment ();
                return null;
            }

            switch (op) {
            case '.':
                return new Token (TokenClass.MemberAccess, ".", lastDocStr, location);
            default:
                return new Token (TokenClass.Operator, op.ToString (), lastDocStr, location);
            }
        }

        private void ReadLineComment ()
        {
            while (PeekChar () != -1) {

                char ch = (char)ReadChar ();

                if (ch == '*') {
                    if ((char)PeekChar () == '/') {
                        ReadChar ();
                        return;
                    }
                }
            }

            errorLog.Add (Errors.UnexpectedEndOfFile, location);
        }

        private void ReadDocComment ()
        {
            StringBuilder accum = new StringBuilder ();
            while (PeekChar () != -1) {

                char ch = (char)ReadChar ();

                accum.Append (ch);

                if (ch == '*') {
                    if ((char)PeekChar () == '/') {
                        ReadChar ();

                        string doc = accum.ToString ();
                        var lines = doc.Split ('\n')
                            .Select (p => p.Trim ())
                            .Where (p => p.StartsWith ("*"))
                            .Select (p => p.Substring (p.IndexOf ('*') + 1).Trim ());
                        lastDocStr = String.Join ("\n", lines);
                        return;
                    }
                }

            }

            errorLog.Add (Errors.UnexpectedEndOfFile, location);
        }

        private bool EatWhiteSpaces ()
        {
            bool hadNewLine = false;
            while (char.IsWhiteSpace ((char)PeekChar ())) {
                char ch = (char)ReadChar ();
                if (ch == '\n') {
                    hadNewLine = true;
                }
            }
            return hadNewLine;
        }

        private bool MatchString (string str)
        {
            for (int i = 0; i < str.Length; i++) {
                if (PeekChar (i) != str [i]) {
                    return false;
                }
            }
            return true;
        }

        private void ReadChars (int n)
        {
            for (int i = 0; i < n; i++) {
                ReadChar ();
            }
        }

        private int ReadChar ()
        {
            if (position >= sourceLen) {
                return -1;
            }

            if (source [position] == '\n') {
                location = new SourceLocation (location.Line + 1, 0, this.file); 
            } else {
                location = new SourceLocation (location.Line,
                    location.Column + 1,
                    this.file
                ); 
            }
            return source [position++];
        }

        private int PeekChar ()
        {
            return PeekChar (0);
        }

        private int PeekChar (int n)
        {
            if (position + n >= sourceLen) {
                return -1;
            }
            return source [position + n];
        }
    }
}

