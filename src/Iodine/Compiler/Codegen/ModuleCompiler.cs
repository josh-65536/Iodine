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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Iodine.Compiler.Ast;
using Iodine.Runtime;

namespace Iodine.Compiler
{
	public class ModuleCompiler : IAstVisitor
	{
		private SymbolTable symbolTable;
		private IodineModule module;
		private FunctionCompiler functionCompiler;

		public ModuleCompiler (SymbolTable symbolTable, IodineModule module)
		{
			this.symbolTable = symbolTable;
			this.module = module;
			functionCompiler = new FunctionCompiler (symbolTable, module.Initializer);
		}

		public void Accept (AstNode ast)
		{
			this.visitSubnodes (ast);
		}

		public void Accept (AstRoot ast)
		{
			visitSubnodes (ast);
		}

		public void Accept (Expression expr)
		{
			visitSubnodes (expr);
		}

		public void Accept (Statement stmt)
		{
			stmt.Visit (functionCompiler);
		}

		public void Accept (BinaryExpression binop)
		{
			binop.Visit (functionCompiler);
		}

		public void Accept (UnaryExpression unaryop)
		{
			unaryop.Visit (functionCompiler);
		}

		public void Accept (NameExpression ident)
		{
			ident.Visit (functionCompiler);
		}

		public void Accept (CallExpression call)
		{
			call.Visit (functionCompiler);
		}

		public void Accept (ArgumentList arglist)
		{
			arglist.Visit (functionCompiler);
		}

		public void Accept (KeywordArgumentList kwargs)
		{
			kwargs.Visit (functionCompiler);
		}

		public void Accept (GetExpression getAttr)
		{
			getAttr.Visit (functionCompiler);
		}

		public void Accept (IntegerExpression integer)
		{
			integer.Visit (functionCompiler);
		}

		public void Accept (FloatExpression num)
		{
			num.Visit (functionCompiler);
		}

		public void Accept (StringExpression str)
		{
			str.Visit (functionCompiler);
		}

		public void Accept (IfStatement ifStmt)
		{
			ifStmt.Visit (functionCompiler);
		}

		public void Accept (WhileStatement whileStmt)
		{
			whileStmt.Visit (functionCompiler);
		}

		public void Accept (DoStatement doStmt)
		{
			doStmt.Visit (functionCompiler);
		}

		public void Accept (ForStatement forStmt)
		{
			forStmt.Visit (functionCompiler);
		}

		public void Accept (ForeachStatement foreachStmt)
		{
			foreachStmt.Visit (functionCompiler);
		}

		public void Accept (TupleExpression tuple)
		{
			tuple.Visit (functionCompiler);
		}

		public void Accept (ContinueStatement cont)
		{
			cont.Visit (functionCompiler);
		}

		public void Accept (MatchExpression match)
		{
			match.VisitChildren (functionCompiler);
		}

		public void Accept (CaseExpression caseExpr)
		{
			caseExpr.VisitChildren (functionCompiler);
		}

		public void Accept (TernaryExpression ifExpr)
		{
			ifExpr.Visit (functionCompiler);
		}

		public void Accept (GivenStatement switchStmt)
		{
		}

		public void Accept (WhenStatement caseStmt)
		{
		}

		public void Accept (FunctionDeclaration funcDecl)
		{
			module.AddMethod (CompileMethod (funcDecl));
		}

		public void Accept (CodeBlock scope)
		{
			scope.Visit (functionCompiler);
		}

		public void Accept (UseStatement useStmt)
		{
			string import = !useStmt.Relative ? useStmt.Module : Path.Combine (
				                Path.GetDirectoryName (useStmt.Location.File),
				                useStmt.Module);
			
			if (useStmt.Wildcard) {
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.LoadConst, module.DefineConstant (
					new IodineString (import)));
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.BuildTuple, 0);
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.LoadGlobal, module.DefineConstant (
					new IodineName ("require")));
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.Invoke, 2);
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.Pop);
			} else {
				IodineObject[] items = new IodineObject [useStmt.Imports.Count];

				module.Initializer.EmitInstruction (useStmt.Location, Opcode.LoadConst, module.DefineConstant (
					new IodineString (import)));
				if (items.Length > 0) {
					for (int i = 0; i < items.Length; i++) {
						items [i] = new IodineString (useStmt.Imports [i]);
						module.Initializer.EmitInstruction (useStmt.Location, Opcode.LoadConst, module.DefineConstant (
							new IodineString (useStmt.Imports [i])));
					}
					module.Initializer.EmitInstruction (useStmt.Location, Opcode.BuildTuple, items.Length);
				}
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.LoadGlobal, module.DefineConstant (
					new IodineName ("require")));
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.Invoke, items.Length == 0 ? 1 : 2);
				module.Initializer.EmitInstruction (useStmt.Location, Opcode.Pop);
			}
			
		}

		public void Accept (ClassDeclaration classDecl)
		{
			module.SetAttribute (classDecl.Name, CompileClass (classDecl));
		}

		public void Accept (InterfaceDeclaration contractDecl)
		{
			IodineInterface contract = new IodineInterface (contractDecl.Name);
			foreach (AstNode node in contractDecl.Children) {
				FunctionDeclaration decl = node as FunctionDeclaration;
				contract.AddMethod (new IodineMethod (module, decl.Name, decl.InstanceMethod,
					decl.Parameters.Count, 0));
			}
			module.SetAttribute (contractDecl.Name, contract);
		}

		public void Accept (ReturnStatement returnStmt)
		{
			returnStmt.Visit (functionCompiler);
		}

		public void Accept (YieldStatement yieldStmt)
		{
			yieldStmt.Visit (functionCompiler);
		}

		public void Accept (IndexerExpression indexer)
		{
			indexer.Visit (functionCompiler);
		}

		public void Accept (ListExpression list)
		{
			list.Visit (functionCompiler);
		}

		public void Accept (HashExpression hash)
		{
			hash.Visit (functionCompiler);
		}

		public void Accept (SelfStatement self)
		{
			self.Visit (functionCompiler);
		}

		public void Accept (TrueExpression ntrue)
		{
			ntrue.Visit (functionCompiler);
		}

		public void Accept (FalseExpression nfalse)
		{
			nfalse.Visit (functionCompiler);
		}

		public void Accept (NullExpression nil)
		{
			nil.Visit (functionCompiler);
		}

		public void Accept (LambdaExpression lambda)
		{
			lambda.Visit (functionCompiler);
		}

		public void Accept (TryExceptStatement tryExcept)
		{
			tryExcept.Visit (functionCompiler);
		}

		public void Accept (RaiseStatement raise)
		{
			raise.Value.Visit (this);
		}

		public void Accept (BreakStatement brk)
		{
			brk.Visit (functionCompiler);
		}

		public void Accept (WithStatement withStmt)
		{
			withStmt.Visit (functionCompiler);
		}

		private void visitSubnodes (AstNode root)
		{
			foreach (AstNode node in root) {
				node.Visit (this);
			}
		}

		public void Accept (SuperCallExpression super)
		{
		}


		public void Accept (EnumDeclaration enumDecl)
		{
			module.SetAttribute (enumDecl.Name, CompileEnum (enumDecl));
		}

		public void Accept (ListCompExpression list)
		{
			list.Visit (functionCompiler);
		}

		public IodineClass CompileClass (ClassDeclaration classDecl)
		{
			IodineMethod constructor = CompileMethod (classDecl.Constructor);
			if (classDecl.Constructor.Children [0].Children.Count == 0 ||
				!(classDecl.Constructor.Children [0].Children [0] is SuperCallExpression)) {
				if (classDecl.Base.Count > 0) {
					foreach (string subclass in classDecl.Base) {
						string[] contract = subclass.Split ('.');
						constructor.EmitInstruction (classDecl.Location, Opcode.LoadGlobal,
							constructor.Module.DefineConstant (new IodineName (contract [0])));
						for (int j = 1; j < contract.Length; j++) {
							constructor.EmitInstruction (classDecl.Location, Opcode.LoadAttribute,
								constructor.Module.DefineConstant (new IodineName (contract [0])));
						}
						constructor.EmitInstruction (classDecl.Location, Opcode.InvokeSuper, 0);
					}
				}
			}
			IodineMethod initializer = new IodineMethod (module, "__init__", false, 0, 0);
			IodineClass clazz = new IodineClass (classDecl.Name, initializer, constructor);
			FunctionCompiler compiler = new FunctionCompiler (symbolTable,
				clazz.Initializer);
			
			for (int i = 1; i < classDecl.Children.Count; i++) {
				if (classDecl.Children [i] is FunctionDeclaration) {
					FunctionDeclaration func = classDecl.Children [i] as FunctionDeclaration;
					if (func.InstanceMethod)
						clazz.AddInstanceMethod (CompileMethod (func));
					else {
						clazz.SetAttribute (func.Name, CompileMethod (func));
					}
				} else if (classDecl.Children [i] is ClassDeclaration) {
					ClassDeclaration subclass = classDecl.Children [i] as ClassDeclaration;
					clazz.SetAttribute (subclass.Name, CompileClass (subclass));
				} else if (classDecl.Children [i] is EnumDeclaration) {
					EnumDeclaration enumeration = classDecl.Children [i] as EnumDeclaration;
					clazz.SetAttribute (enumeration.Name, CompileEnum (enumeration));
				} else if (classDecl.Children [i] is BinaryExpression) {
					BinaryExpression expr = classDecl.Children [i] as BinaryExpression;
					NameExpression name = expr.Left as NameExpression;
					expr.Right.Visit (compiler);
					initializer.EmitInstruction (classDecl.Location, Opcode.LoadGlobal,
						module.DefineConstant (new
						IodineName (classDecl.Name)));
					initializer.EmitInstruction (classDecl.Location, Opcode.StoreAttribute,
						module.DefineConstant (new
						IodineName (name.Value)));
				} else {
					classDecl.Children [i].Visit (compiler);
				}
			}
			clazz.Initializer.FinalizeLabels ();
			return clazz;
		}

		private IodineEnum CompileEnum (EnumDeclaration enumDecl)
		{
			IodineEnum ienum = new IodineEnum (enumDecl.Name);
			foreach (string name in enumDecl.Items.Keys) {
				ienum.AddItem (name, enumDecl.Items [name]);
			}
			return ienum;
		}

		private IodineMethod CompileMethod (FunctionDeclaration funcDecl)
		{
			symbolTable.NextScope ();
			IodineMethod methodBuilder = new IodineMethod (module, funcDecl.Name, funcDecl.InstanceMethod,
				                             funcDecl.Parameters.Count,
				                             symbolTable.CurrentScope.SymbolCount);
			FunctionCompiler compiler = new FunctionCompiler (symbolTable, 
				                            methodBuilder);
			methodBuilder.Variadic = funcDecl.Variadic;
			methodBuilder.AcceptsKeywordArgs = funcDecl.AcceptsKeywordArgs;
			for (int i = 0; i < funcDecl.Parameters.Count; i++) {
				methodBuilder.Parameters [funcDecl.Parameters [i]] = symbolTable.GetSymbol
					(funcDecl.Parameters [i]).Index;
			}
			funcDecl.Children [0].Visit (compiler);
			AstNode lastNode = funcDecl.Children [0].LastOrDefault ();
			if (lastNode != null) {
				methodBuilder.EmitInstruction (lastNode.Location, Opcode.LoadNull);
			} else {
				methodBuilder.EmitInstruction (funcDecl.Location, Opcode.LoadNull);
			}
			methodBuilder.FinalizeLabels ();
			symbolTable.LeaveScope ();
			return methodBuilder;
		}
	}
}

