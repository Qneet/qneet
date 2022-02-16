using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Qnit.TestAdapter;

internal static class ManagedNameConstants
{
    /// <summary>
    /// Label to use on ManagedType test property.
    /// </summary>
    internal const string ManagedTypeLabel = "ManagedType";

    /// <summary>
    /// Label to use on ManagedMethod test property.
    /// </summary>
    internal const string ManagedMethodLabel = "ManagedMethod";

    /// <summary>
    /// Property id to use on ManagedType test property.
    /// </summary>
    internal const string ManagedTypePropertyId = "TestCase." + ManagedTypeLabel;

    /// <summary>
    /// Property id to use on ManagedMethod test property.
    /// </summary>
    internal const string ManagedMethodPropertyId = "TestCase." + ManagedMethodLabel;

    /// <summary>
    /// Label to use on Hierarchy test property.
    /// </summary>
    public const string HierarchyLabel = "Hierarchy";

    /// <summary>
    /// Property id to use on Hierarchy test property.
    /// </summary>
    public const string HierarchyPropertyId = "TestCase." + HierarchyLabel;

    /// <summary>
    /// Meanings of the indices in the Hierarchy array.
    /// </summary>
    public static class Levels
    {
        /// <summary>
        /// Total length of Hierarchy array.
        /// </summary>
        public const int TotalLevelCount = 2;

        /// <summary>
        /// Index of the namespace element of the array.
        /// </summary>
        public const int NamespaceIndex = 0;

        /// <summary>
        /// Index of the class element of the array.
        /// </summary>
        public const int ClassIndex = 1;
    }

    internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
        id: ManagedTypePropertyId,
        label: ManagedTypeLabel,
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(TestCase));

    internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
        id: ManagedMethodPropertyId,
        label: ManagedMethodLabel,
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string),
        validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
        attributes: TestPropertyAttributes.Hidden,
        owner: typeof(TestCase));

    internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
        id: HierarchyPropertyId,
        label: HierarchyLabel,
        category: string.Empty,
        description: string.Empty,
        valueType: typeof(string[]),
        validateValueCallback: null,
        attributes: TestPropertyAttributes.Immutable,
        owner: typeof(TestCase));
}