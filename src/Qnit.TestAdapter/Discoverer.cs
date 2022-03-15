using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Qnit.TestAdapter;

internal readonly struct Discoverer
{
    // Suffix "Tests"
    private static ReadOnlyMemory<byte> s_testSuffixBytes => new(new byte[] { 54, 64, 73, 74, 73});
    private const string TestsSuffix = "Tests";

    private const byte DotChar = 46; // symbol '.'

    private readonly IMessageLogger m_messageLogger;

    public Discoverer(IMessageLogger messageLogger)
    {
        m_messageLogger = messageLogger;
    }

    public void Discover<TTestCaseCollector>(string source, TTestCaseCollector testCaseCollector, CancellationToken cancellationToken) where TTestCaseCollector : ITestCaseCollector
    {
        if (!TestSource.Supported(source))
        {
            m_messageLogger.SendMessage(TestMessageLevel.Error, $"Test source \"{source}\" not supported");
            return;
        }

        using var assemblyFile = File.OpenRead(source);
        using var peReader = new PEReader(assemblyFile);
        if (peReader.HasMetadata && peReader.IsEntireImageAvailable && peReader.PEHeaders.CorHeader != null)
        {
            var metadataReader = peReader.GetMetadataReader();
            if (metadataReader.IsAssembly)
            {
                var sourceUtf8Bytes = new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(source));
                var idProvider = new TestIdProvider();

                var namespaceCache = new NamespaceCache(metadataReader);

                var typeDefinitions = metadataReader.TypeDefinitions;
                foreach (var typeHandle in typeDefinitions)
                {
                    var type = metadataReader.GetTypeDefinition(typeHandle);

                    if (IsTestClass(type) && EndWithTests(metadataReader, type.Name))
                    {
                        var namespaceUtf8Bytes = metadataReader.GetReadOnlySpanSpan(type.Namespace);
                        var classNameUtf8Bytes = metadataReader.GetReadOnlySpanSpan(type.Name);

                        var className = metadataReader.GetString(type.Name);
                        var classNameWithNamespace = type.Namespace.IsNil
                            ? className
                            : string.Concat(namespaceCache.GetName(type.Namespace), ".", className);

                        foreach (var methodHandle in type.GetMethods())
                        {
                            var method = metadataReader.GetMethodDefinition(methodHandle);
                            if (IsTestMethod(metadataReader, method))
                            {
                                var methodNameUtf8Bytes = metadataReader.GetReadOnlySpanSpan(method.Name);

                                var methodName = metadataReader.GetString(method.Name);

                                var methodFullyQualifiedName = string.Concat(classNameWithNamespace, ".", methodName);
                                var testCase = new TestCase(methodFullyQualifiedName, ExecutorUri.Uri, source);

                                testCase.SetPropertyValue(ManagedNameConstants.ManagedTypeProperty, classNameWithNamespace);
                                testCase.SetPropertyValue(ManagedNameConstants.ManagedMethodProperty, methodName);

                                idProvider.Append(ExecutorUri.Utf8Bytes.Span);
                                idProvider.Append(sourceUtf8Bytes);
                                idProvider.Append(namespaceUtf8Bytes);
                                idProvider.Append(DotChar);
                                idProvider.Append(classNameUtf8Bytes);
                                idProvider.Append(DotChar);
                                idProvider.Append(methodNameUtf8Bytes);

                                testCase.Id = idProvider.GetIdAndReset();

                                testCaseCollector.AddTestCase(testCase);
                            }
                        }

                    }
                }
            }
        }
    }

    // Test class is public, static or instance, not generic, has suffix Tests, not nested
    private static bool IsTestClass(TypeDefinition type)
    {
        var attributes = type.Attributes;
        var visibility = attributes & TypeAttributes.VisibilityMask;
        var classSemantics = attributes & TypeAttributes.ClassSemanticsMask;
        if (!((visibility == TypeAttributes.Public)
              && classSemantics != TypeAttributes.Interface
              && (!attributes.HasFlag(TypeAttributes.Abstract) || (attributes.HasFlag(TypeAttributes.Abstract) &&
                                                                   attributes.HasFlag(TypeAttributes.Sealed)))))
        {
            return false;
        }

        if (attributes.HasFlag(TypeAttributes.SpecialName))
        {
            return false;
        }

        if (type.GetGenericParameters().Count > 0)
        {
            return false;
        }

        if (type.IsNested)
        {
            return false;
        }

        return true;

    }

    private static bool EndWithTests(MetadataReader metadataReader, StringHandle stringHandle)
    {
        if (IsVirtual(stringHandle))
        {
            return metadataReader.GetString(stringHandle).EndsWith(TestsSuffix, StringComparison.Ordinal);
        }
        else
        {
            var memoryBlock = metadataReader.GetBlobReader(stringHandle);
            if (memoryBlock.Length >= s_testSuffixBytes.Length)
            {
                var s = memoryBlock.AsReadonlySpan(memoryBlock.Length - s_testSuffixBytes.Length);
                return s.SequenceEqual(s_testSuffixBytes.Span);
            }
            else
            {
                return false;
            }
        }
    }

    private static bool IsVirtual(StringHandle stringHandle) => MetadataTokens.GetHeapOffset(stringHandle) != -1;

    // Tst method is public, static or instance, not generic, return void, does not have parameters
    private static bool IsTestMethod(MetadataReader metadataReader, MethodDefinition method)
    {
        var attributes = method.Attributes;
        var visibility = attributes & MethodAttributes.MemberAccessMask;
        if (visibility != MethodAttributes.Public
            || attributes.HasFlag(MethodAttributes.SpecialName)
            || attributes.HasFlag(MethodAttributes.Abstract)
            || attributes.HasFlag(MethodAttributes.Virtual))
        {
            return false;
        }

        var signatureBlobReader = metadataReader.GetBlobReader(method.Signature);
        var signatureHeader = signatureBlobReader.ReadSignatureHeader();
        if (signatureHeader.IsGeneric)
            return false;
        if (signatureHeader.CallingConvention != SignatureCallingConvention.Default)
        {
            //&& signatureHeader.CallingConvention != SignatureCallingConvention.VarArgs)
            return false;
        }

        var parameterCount = signatureBlobReader.ReadCompressedInteger();
        if (parameterCount > 0)
        {
            return false;
        }

        var returnTypeCode = signatureBlobReader.ReadCompressedInteger();
        if (returnTypeCode != (int)SignatureTypeCode.Void)
        {
            return false;
        }

        return true;
    }
}
