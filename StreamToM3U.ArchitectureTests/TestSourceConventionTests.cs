using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies the structural and attribution conventions of the unit-test sources using
    /// Roslyn syntax analysis over the staged test sources.
    /// </summary>
    [TestFixture]
    public sealed class TestSourceConventionTests
    {
        static readonly int maximumParameterCount = 7;

        static readonly string[] testAttributeNames = ["Test", "TestCase", "TestCaseSource"];

        static readonly SyntaxTree[] testTrees = BuildSyntaxTrees(ArchitectureSourceRepository.TestFiles);

        [Test]
        public void GivenTestSources_WhenEveryFileIsChecked_ThenItDeclaresExactlyOneTopLevelType()
        {
            Assert.That(testTrees, Is.Not.Empty, "The test sources must be staged for checking.");

            foreach (SyntaxTree tree in testTrees)
            {
                IReadOnlyList<string> declaredTypes = [.. tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)
                    .Select(declaration => declaration.Identifier.Text)];

                Assert.That(
                    declaredTypes,
                    Has.Count.EqualTo(1),
                    $"The test source '{tree.FilePath}' must declare exactly one top-level type but declares: {string.Join(", ", declaredTypes)}.");
            }
        }

        [Test]
        public void GivenTestSources_WhenEveryFileIsChecked_ThenTheFileNameStemMatchesTheDeclaredType()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string typeName = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Single(declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)
                    .Identifier
                    .Text;

                Assert.That(
                    System.IO.Path.GetFileNameWithoutExtension(tree.FilePath),
                    Is.EqualTo(typeName),
                    $"The test source '{tree.FilePath}' must use the declared type identifier as its filename stem.");
            }
        }

        [Test]
        public void GivenTestSources_WhenNamespacesAreChecked_ThenTheyUseBlockNamespaces()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                SyntaxNode root = tree.GetRoot();

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            root.DescendantNodes().OfType<NamespaceDeclarationSyntax>(),
                            Is.Not.Empty,
                            $"The test source '{tree.FilePath}' must declare an explicit block namespace.");

                        Assert.That(
                            root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                            Is.Empty,
                            $"The test source '{tree.FilePath}' must not use a file-scoped namespace.");

                        Assert.That(
                            root.DescendantNodes().OfType<GlobalStatementSyntax>(),
                            Is.Empty,
                            $"The test source '{tree.FilePath}' must not contain top-level statements.");
                    });
            }
        }

        [Test]
        public void GivenTestSources_WhenEveryFileIsChecked_ThenNoNestedTypeOrForbiddenDirectiveExists()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);
                string source = tree.GetText().ToString();
                SyntaxNode root = tree.GetRoot();

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Any(
                                declaration => declaration.Parent is TypeDeclarationSyntax),
                            Is.False,
                            $"The test source '{fileName}' declares a nested type; dummy implementations must be top-level internal classes in separate files.");

                        Assert.That(
                            source.Contains("#region", StringComparison.Ordinal) || source.Contains("#endregion", StringComparison.Ordinal),
                            Is.False,
                            $"The test source '{fileName}' contains a region directive; comment banners and regions are prohibited.");

                        Assert.That(
                            source,
                            Does.Not.Contain("Xunit"),
                            $"The test source '{fileName}' must use NUnit, not xUnit.");

                        Assert.That(
                            source,
                            Does.Not.Contain("Microsoft.VisualStudio.TestTools"),
                            $"The test source '{fileName}' must use NUnit, not MSTest.");
                    });
            }
        }

        [Test]
        public void GivenEveryTestFixture_WhenChecked_ThenItUsesNUnitAttributesAndContainsTests()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);

                foreach (ClassDeclarationSyntax fixture in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool hasFixtureAttribute = HasAttribute(fixture.AttributeLists, "TestFixture");
                    bool hasTestMethod = fixture.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(method => testAttributeNames.Any(name => HasAttribute(method.AttributeLists, name)));

                    if (!hasFixtureAttribute)
                    {
                        Assert.That(
                            hasTestMethod,
                            Is.False,
                            $"The test class '{fixture.Identifier.Text}' in '{fileName}' declares test methods without a TestFixture attribute.");

                        continue;
                    }

                    Assert.That(
                        hasTestMethod,
                        Is.True,
                        $"The test fixture '{fixture.Identifier.Text}' in '{fileName}' must contain a Test or TestCase method.");
                }
            }
        }

        [Test]
        public void GivenTestFixtureClasses_WhenNamed_ThenTheyFollowTheSubjectTestsPattern()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);

                foreach (ClassDeclarationSyntax fixture in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (!HasAttribute(fixture.AttributeLists, "TestFixture"))
                    {
                        continue;
                    }

                    Assert.That(
                        fixture.Identifier.Text,
                        Does.EndWith("Tests"),
                        $"The test fixture '{fixture.Identifier.Text}' in '{fileName}' must be named '<Subject>Tests'.");
                }
            }
        }

        [Test]
        public void GivenTestMethods_WhenNamed_ThenTheyFollowTheGivenWhenThenPattern()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    bool isTest = testAttributeNames.Any(name => HasAttribute(method.AttributeLists, name));

                    if (!isTest)
                    {
                        continue;
                    }

                    string methodName = method.Identifier.Text;

                    Assert.That(
                        methodName.StartsWith("Given", StringComparison.Ordinal) &&
                        methodName.Contains("_When", StringComparison.Ordinal) &&
                        methodName.Contains("_Then", StringComparison.Ordinal),
                        Is.True,
                        $"The test method '{methodName}' in '{fileName}' must follow the 'Given<Context>_When<Action>_Then<Outcome>' pattern.");
                }
            }
        }

        [Test]
        public void GivenTestMethods_WhenTheyDeclareAssertions_ThenEachBusinessTestAssertsSomething()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    bool isTest = testAttributeNames.Any(name => HasAttribute(method.AttributeLists, name));

                    if (!isTest || method.Body is null)
                    {
                        continue;
                    }

                    bool assertsOutcome = method.Body
                        .DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(invocation =>
                            invocation.Expression.ToString().StartsWith("Assert.", StringComparison.Ordinal) ||
                            invocation.Expression.ToString().StartsWith("downloader.Verify", StringComparison.Ordinal) ||
                            invocation.Expression.ToString().StartsWith("logger.Verify", StringComparison.Ordinal) ||
                            invocation.Expression.ToString().StartsWith("repository.Verify", StringComparison.Ordinal) ||
                            invocation.Expression.ToString().StartsWith("urlRetriever.Verify", StringComparison.Ordinal) ||
                            invocation.Expression.ToString().StartsWith("fileDownloader.Verify", StringComparison.Ordinal));

                    Assert.That(
                        assertsOutcome,
                        Is.True,
                        $"The test method '{method.Identifier.Text}' in '{fileName}' declares no assertion or verification.");
                }
            }
        }

        [Test]
        public void GivenParameterisedTests_WhenTheyDeclareTestCases_ThenTheTestCaseAttributeDeclaresAtLeastTwoRows()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                string fileName = System.IO.Path.GetFileName(tree.FilePath);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    int testCaseCount = method.AttributeLists
                        .SelectMany(attributeList => attributeList.Attributes)
                        .Count(attribute => string.Equals(attribute.Name.ToString(), "TestCase", StringComparison.Ordinal));

                    if (testCaseCount == 0)
                    {
                        continue;
                    }

                    Assert.That(
                        testCaseCount,
                        Is.GreaterThanOrEqualTo(2),
                        $"The parameterised test '{method.Identifier.Text}' in '{fileName}' must declare at least two cases.");
                }
            }
        }

        [Test]
        public void GivenTestAndProductionMembers_WhenTheirParametersAreChecked_ThenNoMemberExceedsSevenParameters()
        {
            IEnumerable<ArchitectureSourceFile> stagedFiles = ArchitectureSourceRepository.Files;

            foreach (ArchitectureSourceFile file in stagedFiles)
            {
                SyntaxTree tree = ParseFile(file);

                foreach (BaseMethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
                {
                    Assert.That(
                        method.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(maximumParameterCount),
                        $"A member in '{file.RelativePath}' declares {method.ParameterList.Parameters.Count} parameters; the maximum is {maximumParameterCount}.");
                }

                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (type.ParameterList is null)
                    {
                        continue;
                    }

                    Assert.That(
                        type.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(maximumParameterCount),
                        $"The primary constructor of '{type.Identifier.Text}' in '{file.RelativePath}' declares {type.ParameterList.Parameters.Count} parameters; the maximum is {maximumParameterCount}.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenObjectCreationIsChecked_ThenNoVarIsUsed()
        {
            foreach (SyntaxTree tree in testTrees)
            {
                foreach (VariableDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<VariableDeclarationSyntax>())
                {
                    Assert.That(
                        declaration.Type.IsVar,
                        Is.False,
                        $"The test source '{tree.FilePath}' declares a variable with 'var'; explicit types are required.");
                }
            }
        }

        [Test]
        public void GivenTestDoubles_WhenTheyAreDeclared_ThenTheyAreInternalAndResideInTheTestDoublesFolder()
        {
            IEnumerable<ArchitectureSourceFile> testDoubleFiles = ArchitectureSourceRepository.TestFiles
                .Where(file => file.RelativePath.Contains("TestDoubles", StringComparison.Ordinal));

            foreach (ArchitectureSourceFile file in testDoubleFiles)
            {
                Assert.That(
                    file.RelativePath.StartsWith("TestDoubles/", StringComparison.Ordinal) ||
                    file.RelativePath.Contains("/TestDoubles/", StringComparison.Ordinal),
                    Is.True,
                    $"The test double '{file.RelativePath}' must reside in a TestDoubles folder.");

                BaseTypeDeclarationSyntax declaration = ParseFile(file).GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Single(candidate => candidate.Parent is BaseNamespaceDeclarationSyntax);

                Assert.That(
                    declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.InternalKeyword)),
                    Is.True,
                    $"The test double '{declaration.Identifier.Text}' in '{file.RelativePath}' must be internal.");
            }
        }

        static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute =>
                {
                    string name = attribute.Name.ToString();
                    return string.Equals(name, attributeName, StringComparison.Ordinal) ||
                        string.Equals(name, $"{attributeName}Attribute", StringComparison.Ordinal);
                });

        static SyntaxTree[] BuildSyntaxTrees(IEnumerable<ArchitectureSourceFile> files)
            => [.. files.Select(ParseFile)];

        static SyntaxTree ParseFile(ArchitectureSourceFile file)
            => CSharpSyntaxTree.ParseText(
                System.IO.File.ReadAllText(file.FullPath),
                new CSharpParseOptions(LanguageVersion.Latest),
                path: file.FullPath);
    }
}