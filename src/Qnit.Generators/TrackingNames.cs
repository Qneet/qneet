namespace Qnit.Generators;

/// <summary>
/// Names that are attached to incremental generator stages for tracking
/// </summary>
[SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "<Pending>")]
[SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "<Pending>")]
[SuppressMessage("Design", "MA0036: Make class static", Justification = "<Pending>")]
public class TrackingNames
{
    public const string InitialExtraction = nameof(InitialExtraction);
    public const string RemovingNulls = nameof(RemovingNulls);
}
