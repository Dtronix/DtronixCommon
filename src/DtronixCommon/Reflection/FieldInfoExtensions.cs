using System.Reflection;
using System.Reflection.Emit;

namespace DtronixCommon.Reflection;

public static class FieldInfoExtensions
{
    public static Func<TClass, TField> CreateGetter<TClass, TField>(this FieldInfo field)
    {
        string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
        DynamicMethod setterMethod =
            new DynamicMethod(methodName, typeof(TField), new Type[1] { typeof(TClass) }, true);
        ILGenerator gen = setterMethod.GetILGenerator();
        if (field.IsStatic)
        {
            gen.Emit(OpCodes.Ldsfld, field);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
        }

        gen.Emit(OpCodes.Ret);
        return (Func<TClass, TField>)setterMethod.CreateDelegate(typeof(Func<TClass, TField>));
    }

    public static Action<TClass, TField> CreateSetter<TClass, TField>(this FieldInfo field)
    {
        string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
        DynamicMethod setterMethod =
            new DynamicMethod(methodName, null, new Type[2] { typeof(TClass), typeof(TField) }, true);
        ILGenerator gen = setterMethod.GetILGenerator();
        if (field.IsStatic)
        {
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stsfld, field);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
        }

        gen.Emit(OpCodes.Ret);
        return (Action<TClass, TField>)setterMethod.CreateDelegate(typeof(Action<TClass, TField>));
    }
}

