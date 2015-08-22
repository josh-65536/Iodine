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
using System.Collections.Generic;
using Iodine.Compiler;

namespace Iodine.Runtime
{
	public class IodineObject
	{
		public static readonly IodineTypeDefinition ObjectTypeDef = new IodineTypeDefinition ("Object");

		public IodineObject Base {
			set;
			get;
		}

		public List<IodineInterface> Interfaces {
			set;
			get;
		}

		public IodineTypeDefinition TypeDef {
			private set;
			get;
		}

		public Dictionary<string, IodineObject> Attributes {
			get {
				return this.attributes;
			}
		}

		protected Dictionary<string, IodineObject> attributes = new Dictionary<string, IodineObject> ();

		public IodineObject (IodineTypeDefinition typeDef)
		{
			this.TypeDef = typeDef;
			this.Interfaces = new List<IodineInterface> ();
			this.attributes ["typeDef"] = typeDef;
		}

		public bool HasAttribute (string name)
		{
			bool res = this.attributes.ContainsKey (name);
			if (!res && this.Base != null)
				return this.Base.HasAttribute (name);
			return res;
		}

		public void SetAttribute (VirtualMachine vm, string name, IodineObject value)
		{
			if (this.Base != null && !this.attributes.ContainsKey (name)) {
				if (this.Base.HasAttribute (name)) {
					this.Base.SetAttribute (vm, name, value);
					return;
				}
			}

			if (this.attributes.ContainsKey ("__setAttribute__")) {
				IodineInstanceMethodWrapper method = this.attributes ["__setAttribute__"] as
					IodineInstanceMethodWrapper;
				if (method == null) {
					vm.RaiseException (new IodineTypeException ("Method"));
				} else if (method.Method.ParameterCount < 2 && !method.Method.Variadic) {
					vm.RaiseException (new IodineArgumentException (2));
				}
				method.Invoke (vm, new IodineObject[] { new IodineString (name), value });
			} else {
				SetAttribute (name, value);
			}
		}


		public void SetAttribute (string name, IodineObject value)
		{
			if (value is IodineMethod) {
				IodineMethod method = (IodineMethod)value;
				if (method.InstanceMethod) {
					this.attributes [name] = new IodineInstanceMethodWrapper (this, method);
				} else {
					this.attributes [name] = value;
				}
			} else if (value is IodineInstanceMethodWrapper) {
				IodineInstanceMethodWrapper wrapper = (IodineInstanceMethodWrapper)value;
				this.attributes [name] = new IodineInstanceMethodWrapper (this, wrapper.Method);
			} else {
				this.attributes [name] = value;
			}
		}

		public IodineObject GetAttribute (string name)
		{
			if (this.attributes.ContainsKey (name))
				return this.attributes [name];
			else if (this.Base != null && this.Base.Attributes.ContainsKey (name))
				return this.Base.GetAttribute (name);
			return null;
		}

		public virtual IodineObject GetAttribute (VirtualMachine vm, string name)
		{
			if (this.attributes.ContainsKey (name))
				return this.attributes [name];
			else if (this.Base != null && this.Base.Attributes.ContainsKey (name))
				return this.Base.GetAttribute (name);
			else if (this.attributes.ContainsKey ("__getAttribute__")) {
				IodineInstanceMethodWrapper method = this.attributes ["__getAttribute__"] as
					IodineInstanceMethodWrapper;
				if (method == null) {
					vm.RaiseException (new IodineTypeException ("Method"));
				} else if (method.Method.ParameterCount < 1 && !method.Method.Variadic) {
					vm.RaiseException (new IodineArgumentException (1));
				}
				return method.Invoke (vm, new IodineObject[] { new IodineString (name) });
			}
			vm.RaiseException (new IodineAttributeNotFoundException (name));
			return null;
		}

		public virtual IodineObject ToString (VirtualMachine vm)
		{
			return new IodineString (ToString ());
		}

		public virtual IodineObject Represent (VirtualMachine vm)
		{
			if (this.attributes.ContainsKey ("__repr__")) {
				return this.attributes ["__repr__"].Invoke (vm, new IodineObject[] { });
			}
			return ToString (vm);
		}

		public virtual void SetIndex (VirtualMachine vm, IodineObject key, IodineObject value)
		{
			
		}

		public virtual IodineObject GetIndex (VirtualMachine vm, IodineObject key)
		{
			return null;
		}

		public virtual IodineObject PerformBinaryOperation (VirtualMachine vm, BinaryOperation binop, IodineObject rvalue)
		{
			IodineObject[] arguments = new IodineObject[] { rvalue };
			string methodName = null;
			switch (binop) {
			case BinaryOperation.Add:
				methodName = "__add__";
				break;
			case BinaryOperation.Sub:
				methodName = "__sub__";
				break;
			case BinaryOperation.Mul:
				methodName = "__mul__";
				break;
			case BinaryOperation.Div:
				methodName = "__div__";
				break;
			case BinaryOperation.And:
				methodName = "__and__";
				break;
			case BinaryOperation.Xor:
				methodName = "__xor__";
				break;
			case BinaryOperation.Or:
				methodName = "__or__";
				break;
			case BinaryOperation.Mod:
				methodName = "__mod__";
				break;
			case BinaryOperation.Equals:
				if (HasAttribute ("___equals__")) {
					methodName = "__equals__";
					break;
				}
				return new IodineBool (this == rvalue);
			case BinaryOperation.NotEquals:
				if (HasAttribute ("__notEquals__")) {
					methodName = "__notEquals__";
					break;
				}
				return new IodineBool (this != rvalue);
			case BinaryOperation.RightShift:
				methodName = "__rightShift__";
				break;
			case BinaryOperation.LeftShift:
				methodName = "__leftShift__";
				break;
			case BinaryOperation.LessThan:
				methodName = "__lessThan__";
				break;
			case BinaryOperation.GreaterThan:
				methodName = "__greaterThan__";
				break;
			case BinaryOperation.LessThanOrEqu:
				methodName = "__lessThanOrEqu__";
				break;
			case BinaryOperation.GreaterThanOrEqu:
				methodName = "__greaterThanOrEqu__";
				break;
			case BinaryOperation.BoolAnd:
				methodName = "__logicalAnd__";
				break;
			case BinaryOperation.BoolOr:
				methodName = "__logicalOr__";
				break;
			}

			if (this.HasAttribute (methodName)) {
				return GetAttribute (vm, methodName).Invoke (vm, arguments);
			}
			vm.RaiseException (new IodineNotSupportedException (
				"The requested binary operator has not been implemented"));
			return null;
		}

		public virtual IodineObject PerformUnaryOperation (VirtualMachine vm, UnaryOperation op)
		{
			string methodName = null;
			switch (op) {
			case UnaryOperation.Negate:
				methodName = "__negate__";
				break;
			case UnaryOperation.Not:
				methodName = "__not__";
				break;
			case UnaryOperation.BoolNot:
				methodName = "__logicalNot__";
				break;
			}
			if (this.HasAttribute (methodName)) {
				return GetAttribute (vm, methodName).Invoke (vm, new IodineObject[] { });
			}
			vm.RaiseException (new IodineNotSupportedException (
				"The requested unary operator has not been implemented"));
			return null;
		}

		public virtual IodineObject Invoke (VirtualMachine vm, IodineObject[] arguments)
		{
			vm.RaiseException (new IodineNotSupportedException (
				"Object does not support invocation"));
			return null;
		}

		public virtual bool IsTrue ()
		{
			return false;
		}

		public virtual IodineObject IterGetNext (VirtualMachine vm)
		{
			return GetAttribute ("__iterGetNext__").Invoke (vm, new IodineObject[]{ });
		}

		public virtual bool IterMoveNext (VirtualMachine vm)
		{
			return GetAttribute ("__iterMoveNext__").Invoke (vm, new IodineObject[]{ }).IsTrue ();
		}

		public virtual void IterReset (VirtualMachine vm)
		{
			GetAttribute ("__iterReset__").Invoke (vm, new IodineObject[]{ });
		}

		public virtual void PrintTest ()
		{
		}

		public bool InstanceOf (IodineTypeDefinition def)
		{
			foreach (IodineInterface contract in this.Interfaces) {
				if (contract == def) {
					return true;
				}
			}
			IodineObject i = this;
			while (i != null) {
				if (i.TypeDef == def) {
					return true;
				}
				i = i.Base;
			}
			return false;
		}

		public override int GetHashCode ()
		{
			int accum = 17;
			unchecked {
				foreach (IodineObject obj in this.attributes.Values) {
					accum += 529 * obj.GetHashCode ();
				}
			}
			return accum;
		}

		public override string ToString ()
		{
			return this.TypeDef.Name;
		}
	}
}

