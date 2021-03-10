using System;
using System.Reflection;
using System.Reflection.Emit;

static class ReflectionExtensions
{
	public static bool IsProperty(this MethodBase method)
	{
		var properties = method.DeclaringType.GetProperties();
		foreach (var prop in properties)
		{
			if (method == prop.GetMethod) return true;
			if (method == prop.SetMethod) return true;
		}
		return false;
	}

	public static MethodBuilder DefineMethodOverride(this TypeBuilder typeB, MethodInfo declMethod)
	{
		ParameterInfo[] parms = declMethod.GetParameters();

		Type[] parmTypes = new Type[parms.Length];
		for (int i = 0; i<parms.Length ; i++)
			parmTypes[i] = parms[i].ParameterType;

		MethodAttributes attrs = declMethod.Attributes ^ MethodAttributes.Abstract;
		attrs ^= MethodAttributes.NewSlot;
		attrs |= MethodAttributes.Final;
		MethodBuilder method_builder = typeB.DefineMethod(declMethod.Name,
								  attrs,
								  declMethod.ReturnType,
								  parmTypes);

		for (int i = 0; i<parms.Length ; i++)
			method_builder.DefineParameter (i + 1, parms[i].Attributes, parms[i].Name);

		typeB.DefineMethodOverride(method_builder, declMethod);

		return method_builder;
	}
}
