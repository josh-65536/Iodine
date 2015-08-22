﻿/**
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
using System.Text;
using Iodine.Compiler;

namespace Iodine.Runtime
{
	public class IodineString : IodineObject
	{
		public static readonly IodineTypeDefinition TypeDefinition = new StringTypeDef ();

		class StringTypeDef : IodineTypeDefinition
		{
			public StringTypeDef ()
				: base ("Str")
			{
			}

			public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
			{
				if (args.Length <= 0) {
					vm.RaiseException (new IodineArgumentException (1));
				}
				if (args [0].HasAttribute ("_toStr")) {
					IodineString ret = args [0].GetAttribute ("_toStr").Invoke (vm, new IodineObject[]{ })
						as IodineString;
					return ret;
				}
				return new IodineString (args [0].ToString ());
			}
		}

		private int iterIndex = 0;

		public string Value {
			private set;
			get;
		}

		public IodineString (string val)
			: base (TypeDefinition)
		{
			Value = val;
			SetAttribute ("toLower", new InternalMethodCallback (toLower, this));
			SetAttribute ("toUpper", new InternalMethodCallback (toUpper, this));
			SetAttribute ("substr", new InternalMethodCallback (substring, this));
			SetAttribute ("getSize", new InternalMethodCallback (getSize, this));
			SetAttribute ("indexOf", new InternalMethodCallback (indexOf, this));
			SetAttribute ("contains", new InternalMethodCallback (contains, this));
			SetAttribute ("replace", new InternalMethodCallback (replace, this));
			SetAttribute ("startsWith", new InternalMethodCallback (startsWith, this));
			SetAttribute ("endsWith", new InternalMethodCallback (endsWith, this));
			SetAttribute ("split", new InternalMethodCallback (split, this));
			SetAttribute ("join", new InternalMethodCallback (join, this));
			SetAttribute ("trim", new InternalMethodCallback (trim, this));
			SetAttribute ("format", new InternalMethodCallback (format, this));
		}

		public override IodineObject PerformBinaryOperation (VirtualMachine vm, BinaryOperation binop, IodineObject rvalue)
		{
			IodineString str = rvalue as IodineString;
			string strVal = "";

			if (str == null) {
				if (rvalue is IodineChar) {
					strVal = rvalue.ToString ();
				} else if (rvalue is IodineNull) {
					return base.PerformBinaryOperation (vm, binop, rvalue);
				} else {
					vm.RaiseException ("Right value must be of type string!");
					return null;
				}
			} else {
				strVal = str.Value;
			}

			switch (binop) {
			case BinaryOperation.Equals:
				return new IodineBool (strVal == Value);
			case BinaryOperation.NotEquals:
				return new IodineBool (strVal != Value);
			case BinaryOperation.Add:
				return new IodineString (Value + strVal);
			default:
				return base.PerformBinaryOperation (vm, binop, rvalue);
			}
		}

		public override string ToString ()
		{
			return Value;
		}

		public override int GetHashCode ()
		{
			return Value.GetHashCode ();
		}

		public override IodineObject GetIndex (VirtualMachine vm, IodineObject key)
		{
			IodineInteger index = key as IodineInteger;
			if (index == null) {
				vm.RaiseException (new IodineTypeException ("Int"));
				return null;
			}
			if (index.Value >= Value.Length) {
				vm.RaiseException (new IodineIndexException ());
				return null;
			}
			return new IodineChar (Value [(int)index.Value]);
		}

		public override IodineObject IterGetNext (VirtualMachine vm)
		{
			return new IodineChar (Value [iterIndex - 1]);
		}

		public override bool IterMoveNext (VirtualMachine vm)
		{
			if (iterIndex >= Value.Length) {
				return false;
			}
			iterIndex++;
			return true;
		}

		public override void IterReset (VirtualMachine vm)
		{
			iterIndex = 0;
		}

		public override IodineObject Represent (VirtualMachine vm)
		{
			return new IodineString (String.Format ("\"{0}\"", Value));
		}

		private IodineObject toUpper (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			return new IodineString (Value.ToUpper ());
		}

		private IodineObject toLower (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			return new IodineString (Value.ToLower ());
		}

		private IodineObject substring (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}
			int start = 0;
			int len = 0;
			IodineInteger startObj = args [0] as IodineInteger;
			if (startObj == null) {
				vm.RaiseException (new IodineTypeException ("Int"));
				return null;
			}
			start = (int)startObj.Value;
			if (args.Length == 1) {
				len = this.Value.Length;
			} else {
				IodineInteger endObj = args [1] as IodineInteger;
				if (endObj == null) {
					vm.RaiseException (new IodineTypeException ("Int"));
					return null;
				}
				len = (int)endObj.Value;
			}

			if (start < Value.Length && len <= Value.Length) {
				return new IodineString (Value.Substring (start, len - start));
			}
			vm.RaiseException (new IodineIndexException ());
			return null;
		}

		private IodineObject getSize (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			return new IodineInteger (Value.Length);
		}

		private IodineObject indexOf (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}

			IodineChar ch = args [0] as IodineChar;
			char val;
			if (ch == null) {
				if (args [0] is IodineString) {
					val = args [0].ToString () [0];
				} else {
					vm.RaiseException (new IodineTypeException ("Char"));
					return null;
				}
			} else {
				val = ch.Value;
			}

			return new IodineInteger (Value.IndexOf (val));
		}

		private IodineObject contains (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}
			return new IodineBool (Value.Contains (args [0].ToString ()));
		}

		private IodineObject startsWith (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}
			return new IodineBool (Value.StartsWith (args [0].ToString ()));
		}

		private IodineObject endsWith (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}
			return new IodineBool (Value.EndsWith (args [0].ToString ()));
		}

		private IodineObject replace (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 2) {
				vm.RaiseException (new IodineArgumentException (2));
				return null;
			}
			IodineString arg1 = args [0] as IodineString;
			IodineString arg2 = args [1] as IodineString;
			if (arg1 == null || arg2 == null) {
				vm.RaiseException (new IodineTypeException ("Str"));
				return null;
			}
			return new IodineString (Value.Replace (arg1.Value, arg2.Value));
		}

		private IodineObject split (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			if (args.Length < 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return null;
			}

			IodineString selfStr = self as IodineString;
			IodineChar ch = args [0] as IodineChar;
			char val;
			if (ch == null) {
				if (args [0] is IodineString) {
					val = args [0].ToString () [0];
				} else {
					vm.RaiseException (new IodineTypeException ("Char"));
					return null;
				}
			} else {
				val = ch.Value;
			}
			IodineList list = new IodineList (new IodineObject[]{ });
			foreach (string str in selfStr.Value.Split (val)) {
				list.Add (new IodineString (str));
			}
			return list;
		}

		private IodineObject trim (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			return new IodineString (Value.Trim ());
		}

		private IodineObject join (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			StringBuilder accum = new StringBuilder ();
			IodineObject collection = args [0];
			collection.IterReset (vm);
			string last = "";
			string sep = "";
			while (collection.IterMoveNext (vm)) {
				IodineObject o = collection.IterGetNext (vm);
				accum.AppendFormat ("{0}{1}", last, sep);
				last = o.ToString ();
				sep = this.Value;
			}
			accum.Append (last);
			return new IodineString (accum.ToString ());
		}

		private IodineObject format (VirtualMachine vm, IodineObject self, IodineObject[] args)
		{
			string format = this.Value;
			IodineFormatter formatter = new IodineFormatter ();
			return new IodineString (formatter.Format (vm, format, args));
		}
	}
}

