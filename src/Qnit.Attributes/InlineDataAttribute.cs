namespace Qnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public sealed class InlineDataAttribute : Attribute
{
    /*public string? Skip { get; set; }

    public string? DisplayName { get; set; }*/

    public InlineDataAttribute(object obj) { }
    public InlineDataAttribute(object obj1, object obj20) { }
    public InlineDataAttribute(object obj1, object obj2, object obj3) { }
    public InlineDataAttribute(object obj1, object obj2, object obj3, object obj4) { }
}
