namespace Qnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlineDataAttribute : Attribute
{
    public InlineDataAttribute(object obj) { }
    public InlineDataAttribute(object obj1, object obj2) { }
    public InlineDataAttribute(object obj1, object obj2, object obj3) { }
    public InlineDataAttribute(object obj1, object obj2, object obj3, object obj4) { }
}
