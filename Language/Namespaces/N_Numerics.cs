using System.Numerics;
using Cranberry.Builtin;
using Cranberry.Errors;
using Cranberry.Types;

namespace Cranberry.Namespaces;

public class N_Numerics : CNamespace {
	public N_Numerics() : base("Numerics", true) {
		var table = new Dictionary<string, object>();

		// helper conversions / validators
		static double ToDouble(object? o) => Convert.ToDouble(o);

		static void ExpectCount(string name, object?[] args, int expected) {
			if (args.Length != expected)
				throw new RuntimeError($"{name}() expects {expected} argument{(expected == 1 ? "" : "s")}, got {args.Length}");
		}

		static void ExpectMinCount(string name, object?[] args, int min) {
			if (args.Length < min)
				throw new RuntimeError($"{name}() expects at least {min} argument{(min == 1 ? "" : "s")}, got {args.Length}");
		}

		static void ExpectNumber(string name, object? obj, int index) {
			if (!Misc.IsNumber(obj!))
				throw new RuntimeError($"{name}() expects argument #{index + 1} to be a number.");
		}
		
		void AddBinary(string name, Func<double, double, object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectCount(name, args, 2);
				ExpectNumber(name, args[0], 0);
				ExpectNumber(name, args[1], 1);
				return impl(ToDouble(args[0]), ToDouble(args[1]));
			});
		}

		void AddTernary(string name, Func<double, double, double, object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectCount(name, args, 3);
				ExpectNumber(name, args[0], 0);
				ExpectNumber(name, args[1], 1);
				ExpectNumber(name, args[2], 2);
				return impl(ToDouble(args[0]), ToDouble(args[1]), ToDouble(args[2]));
			});
		}

		// varargs, returns double (or whatever)
		void AddVarArgs(string name, Func<double[], object> impl) {
			table[name] = new InternalFunction(args => {
				ExpectMinCount(name, args, 1);
				var arr = new double[args.Length];
				for (int i = 0; i < args.Length; i++) {
					ExpectNumber(name, args[i], i);
					arr[i] = ToDouble(args[i]);
				}

				return impl(arr);
			});
		}
		
		AddBinary("Vector2", (x, y) => new Vector2((float)x, (float)y));
		AddTernary("Vector3", (x, y, z) => new Vector3((float)x, (float)y, (float)z));
		AddVarArgs("Matrix3x2", (args) => {
			// Note: Add the argument count check here (see improvement below)
			if (args.Length != 6)
			{
				throw new RuntimeError($"Matrix3x2() expects 6 arguments, got {args.Length}");
			}
    
			double m11 = args[0], m12 = args[1];
			double m21 = args[2], m22 = args[3];
			double m31 = args[4], m32 = args[5];
    
			// Corrected to use m31 and m32
			return new Matrix3x2((float)m11, (float)m12, (float)m21, (float)m22, (float)m31, (float)m32);
		});
		AddVarArgs("Matrix4x4", (args) => {
			// Add this check for strictness
			if (args.Length != 16)
			{
				throw new RuntimeError($"Matrix4x4() expects 16 arguments, got {args.Length}");
			}

			// Your existing logic is otherwise perfect
			double m11 = args[0], m12 = args[1], m13 = args[2], m14 = args[3];
			double m21 = args[4], m22 = args[5], m23 = args[6], m24 = args[7];
			double m31 = args[8], m32 = args[9], m33 = args[10], m34 = args[11];
			double m41 = args[12], m42 = args[13], m43 = args[14], m44 = args[15];

			return new Matrix4x4(
				(float)m11, (float)m12, (float)m13, (float)m14,
				(float)m21, (float)m22, (float)m23, (float)m24,
				(float)m31, (float)m32, (float)m33, (float)m34,
				(float)m41, (float)m42, (float)m43, (float)m44
			);
		});
		
		// push dictionary into the env
		env.Variables.Push(table);
	}
}