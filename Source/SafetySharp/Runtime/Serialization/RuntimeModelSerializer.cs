// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using Modeling;
	using Modeling.Formulas;
	using Modeling.Formulas.Visitors;
	using Utilities;

	/// <summary>
	///   Serializes a <see cref="RuntimeModel" /> instance into a <see cref="Stream" />.
	/// </summary>
	internal static class RuntimeModelSerializer
	{
		/// <summary>
		///   Saves the serialized <paramref name="model" /> and the <paramref name="formulas" /> to the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the serialized specification should be written to.</param>
		/// <param name="model">The model that should be serialized into the <paramref name="stream" />.</param>
		/// <param name="formulas">The formulas that should be serialized into the <paramref name="stream" />.</param>
		public static void Save(Stream stream, Model model, params Formula[] formulas)
		{
			Requires.NotNull(stream, nameof(stream));
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
				SerializeModel(writer, model, formulas);
		}

		/// <summary>
		///   Loads a <see cref="RuntimeModel" /> from the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the model should be loaded from.</param>
		public static unsafe RuntimeModel Load(Stream stream)
		{
			Requires.NotNull(stream, nameof(stream));

			using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
			{
				// Deserialize the object table
				var model = new Model();
				var objectTable = model.SerializationRegistry.DeserializeObjectTable(reader);

				// Deserialize the object identifiers of the root components
				var rootCount = reader.ReadInt32();
				for (var i = 0; i < rootCount; ++i)
					model.RootComponents.Add((IComponent)objectTable.GetObject(reader.ReadInt32()));

				// Copy the serialized initial state from the stream
				var slotCount = reader.ReadInt32();
				var serializedState = stackalloc int[slotCount];

				for (var i = 0; i < slotCount; ++i)
					serializedState[i] = reader.ReadInt32();

				// Deserialize the model's initial state
				var deserializer = model.SerializationRegistry.CreateStateDeserializer(objectTable, SerializationMode.Full);
				deserializer(serializedState);

				// Deserialize the state formulas
				var stateFormulas = new StateFormula[reader.ReadInt32()];
				for (var i = 0; i < stateFormulas.Length; ++i)
				{
					// Deserialize the closure object and method name to generate the delegate
					var closure = objectTable.GetObject(reader.ReadInt32());
					var method = closure.GetType().GetMethod(reader.ReadString(), BindingFlags.NonPublic | BindingFlags.Instance);
					var expression = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), closure, method);

					// Deserialize the label name and instantiate the state formula
					stateFormulas[i] = new StateFormula(expression, reader.ReadString());
				}

				// Instantiate the runtime model
				return new RuntimeModel(model, objectTable, stateFormulas);
			}
		}

		/// <summary>
		///   Creates the object table for the <paramref name="model" /> and <paramref name="stateFormulas" />.
		/// </summary>
		private static ObjectTable CreateObjectTable(Model model, StateFormula[] stateFormulas)
		{
			var registry = model.SerializationRegistry;
			var modelObjects = model.RootComponents.SelectMany(component => registry.GetReferencedObjects(component));
			var formulaObjects = stateFormulas.SelectMany(formula => registry.GetReferencedObjects(formula.Expression.Target)).ToArray();

			var objects = modelObjects.Concat(formulaObjects).ToArray();
			return new ObjectTable(objects, stateFormulas.Select(formula => formula.Expression.Target));
		}

		/// <summary>
		///   Serializes the <paramref name="model" />.
		/// </summary>
		private static unsafe void SerializeModel(BinaryWriter writer, Model model, Formula[] formulas)
		{
			var stateFormulas = CollectStateFormulas(formulas);
			var objectTable = CreateObjectTable(model, stateFormulas);

			// Prepare the serialization of the model's initial state
			var slotCount = model.SerializationRegistry.GetStateSlotCount(objectTable, SerializationMode.Full);
			var serializer = model.SerializationRegistry.CreateStateSerializer(objectTable, SerializationMode.Full);

			// Serialize the object table
			model.SerializationRegistry.SerializeObjectTable(objectTable, writer);

			// Serialize object identifiers of the root components
			writer.Write(model.RootComponents.Count);
			foreach (var root in model.RootComponents)
				writer.Write(objectTable.GetObjectIdentifier(root));

			// Serialize the initial state
			var serializedState = stackalloc int[slotCount];
			serializer(serializedState);

			// Copy the serialized state to the stream
			writer.Write(slotCount);
			for (var i = 0; i < slotCount; ++i)
				writer.Write(serializedState[i]);

			SerializeFormulas(writer, objectTable, stateFormulas);
		}

		/// <summary>
		///   Serializes the <paramref name="stateFormulas" />.
		/// </summary>
		private static void SerializeFormulas(BinaryWriter writer, ObjectTable objectTable, StateFormula[] stateFormulas)
		{
			writer.Write(stateFormulas.Length);
			foreach (var formula in stateFormulas)
			{
				// Serialize the object identifier of the closure as well as the method name
				writer.Write(objectTable.GetObjectIdentifier(formula.Expression.Target));
				writer.Write(formula.Expression.Method.Name);

				// Serialize the state label name
				writer.Write(formula.Label);
			}
		}

		/// <summary>
		///   Collects all state formulas contained in the <paramref name="formulas" />.
		/// </summary>
		private static StateFormula[] CollectStateFormulas(Formula[] formulas)
		{
			var visitor = new CollectStateFormulasVisitor();
			foreach (var formula in formulas)
				visitor.Visit(formula);

			// Check that the state formula has a closure -- the current version of the C# compiler 
			// always does that, but future versions might not.
			foreach (var formula in visitor.StateFormulas)
				Assert.NotNull(formula.Expression.Target, "Unexpected state formula without closure object.");

			return visitor.StateFormulas.ToArray();
		}
	}
}