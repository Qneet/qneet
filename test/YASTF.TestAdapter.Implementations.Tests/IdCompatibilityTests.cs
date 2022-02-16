using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace YASTF.TestAdapter.Implementations.Tests;

[TestClass]
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "<Pending>")]
public class CompatibilityTests
{
    [DataTestMethod]
    [DataRow(new[] { "eea339da-6b5e-0d4b-3255-bfef95601890", "" })]
    [DataRow(new[] { "740b9afc-3350-4257-ca01-5bd47799147d", "adapter://", "name1" })]                                                                          // less than one block
    [DataRow(new[] { "119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://namesamplenam.testname" })]                                                             // 1 full block
    [DataRow(new[] { "2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://namesamplenamespace.testname" })]                                                       // 1 full block and extra
    [DataRow(new[] { "119c5b31-c0fb-1c12-6d1a-d617bb2bd996", "adapter://", "name", "samplenam", ".", "testname" })]                                             // 1 full block
    [DataRow(new[] { "2a4c33ec-6115-4bd7-2e94-71f2fd3a5ee3", "adapter://", "name", "samplenamespace", ".", "testname" })]                                       // 1 full block and extra
    [DataRow(new[] { "1fc07043-3d2d-1401-c732-3b507feec548", "adapter://namesamplenam.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" })]                             // 2 full blocks
    [DataRow(new[] { "24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://namesamplenamespace.testnameaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" })]                       // 2 full blocks and extra
    [DataRow(new[] { "1fc07043-3d2d-1401-c732-3b507feec548", "adapter://", "name", "samplenam", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" })]         // 2 full blocks
    [DataRow(new[] { "24e8a50b-2766-6a12-f461-9f8e4fa1cbb5", "adapter://", "name", "samplenamespace", ".", "testname", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" })]   // 2 full blocks and extra

    public void IdCompatibilityTests(string[] data)
    {
        // Arrange
        var expectedId = new Guid(data[0]);

        // Act
        var idProvider = new TestIdProvider();
        foreach (var d in data.Skip(1))
        {
            idProvider.AppendString(d);
        }
        var id = idProvider.GetId();

        // Assert
        Assert.AreEqual(expectedId, id);
    }
}
