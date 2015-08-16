﻿using System;

namespace Iodine.Compiler
{
	public class InputStream
	{
		private int position;
		private int sourceLen;
		private string source;
		private string file;

		public Location Location {
			private set;
			get;
		}

		public InputStream (string source, string file)
		{
			this.source = source;
			this.position = 0;
			this.sourceLen = source.Length;
			this.file = file;
		}

		public void EatWhiteSpaces ()
		{
			while (char.IsWhiteSpace ((char)this.PeekChar ())) {
				this.ReadChar ();
			}
		}

		public bool MatchString (string str) 
		{
			for (int i = 0; i < str.Length; i++) {
				if (PeekChar (i) != str [i]) {
					return false;
				}
			}
			return true;
		}

		public void ReadChars (int n)
		{
			for (int i = 0; i < n; i++) {
				ReadChar ();
			}
		}

		public int ReadChar ()
		{
			if (position >= sourceLen) {
				return -1;
			}

			if (source[position] == '\n') {
				this.Location = new Location (this.Location.Line + 1, 0, this.file); 
			} else {
				this.Location = new Location (this.Location.Line, this.Location.Column + 1,
					this.file); 
			}
			return source[position++];
		}

		public int PeekChar ()
		{
			return PeekChar(0);
		}

		public int PeekChar (int n)
		{
			if (position + n >= sourceLen) {
				return -1;
			}
			return source[position + n];
		}
	}
}
