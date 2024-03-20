namespace Qnit.Generators;

[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>")]
internal readonly record struct MethodToGenerate
{
    public readonly string Namespace;
    public readonly string TypeName;
    public readonly string MethodName;
    public readonly Modifiers Modifiers;
    public readonly DisposeType DisposeType;

    //public readonly string FullyQualifiedName;

    public MethodToGenerate(
        string ns,
        string typeName,
        string methodName,
        bool isTypeStatic,
        bool isValueType,
        bool isMethodStatic
        )
    {
        Namespace = ns;
        TypeName = typeName;
        MethodName = methodName;
        if (isTypeStatic)
            Modifiers |= Modifiers.TypeIsStatic;
        if (isValueType)
            Modifiers |= Modifiers.TypeIsValue;
        if (isMethodStatic)
            Modifiers |= Modifiers.MethodIsStatic;

        DisposeType = DisposeType.None;
    }
}

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "<Pending>")]
[Flags]
internal enum Modifiers
{
    None = 0,
    TypeIsStatic = 1,
    TypeIsValue = 2,
    MethodIsStatic = 4
}

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "<Pending>")]
internal enum DisposeType
{
    None = 0,
    Sync = 1,
    Async = 2
}
