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
using Iodine.Runtime;

namespace Iodine
{
	public class IodineTypeConverter
	{
		private static IodineTypeConverter _instance = null;

		public static IodineTypeConverter Instance {
			get {
				if (_instance == null) {
					_instance = new IodineTypeConverter ();
				}
				return _instance;
			}
		}

		private Dictionary<Type, ITypeConverter> conveters = new Dictionary<Type, ITypeConverter> ();

		public IodineTypeConverter ()
		{
			RegisterTypeConverter (typeof(Byte), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(Int16), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(UInt16), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(Int32), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(UInt32), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(Int64), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(UInt64), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(Boolean), new BoolTypeConverter ());
			RegisterTypeConverter (typeof(String), new StringTypeConverter ());
			RegisterTypeConverter (typeof(IodineString), new StringTypeConverter ());
			RegisterTypeConverter (typeof(IodineInteger), new IntegerTypeConverter ());
			RegisterTypeConverter (typeof(IodineBool), new BoolTypeConverter ());
		}

		public void RegisterTypeConverter (Type fromType, ITypeConverter converter)
		{
			conveters [fromType] = converter;
		}

		public bool ConvertToPrimative (IodineObject obj, out object result)
		{
			if (conveters.ContainsKey (obj.GetType ())) {
				return conveters [obj.GetType ()].TryToConvertToPrimative (obj, out result);
			}
			result = null;
			return false;
		}

		public bool ConvertFromPrimative (object obj, out IodineObject result)
		{
			if (conveters.ContainsKey (obj.GetType ())) {
				return conveters [obj.GetType ()].TryToConvertFromPrimative (obj, out result);
			}
			result = null;
			return false;
		}

		public dynamic CreateDynamicObject (IodineEngine engine, IodineObject obj)
		{
			return new IodineDynamicObject (obj, engine.VirtualMachine);
		}
	}
}

