using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Verifies the handwritten production source conventions using Roslyn syntax analysis.
    /// The checks run over the staged production sources and never depend on the host
    /// machine's file-system layout.
    /// </summary>
    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        static readonly int maximumParameterCount = 7;
        static readonly int conditionalLineLimit = 72;
        static readonly int signatureLineLimit = 96;

        static readonly SyntaxTree[] productionTrees = BuildSyntaxTrees(ArchitectureSourceRepository.ProductionFiles);

        [Test]
        public void GivenTheStagedSources_WhenEnumerated_ThenEveryCheckedProjectIsPresent()
        {
            IReadOnlyList<string> stagedProjects = [.. ArchitectureSourceRepository.Files
                .Select(file => file.ProjectName)
                .Distinct()
                .OrderBy(projectName => projectName, StringComparer.Ordinal)];

            Assert.Multiple(
                () =>
                {
                    Assert.That(stagedProjects, Does.Contain("StreamToM3U"));
                    Assert.That(stagedProjects, Does.Contain("StreamToM3U.UnitTests"));
                    Assert.That(productionTrees, Is.Not.Empty, "The staged production sources must contain at least one C# file.");
                });
        }

        [Test]
        public void GivenProductionSources_WhenEveryFileIsChecked_ThenTheFileNameStemMatchesTheDeclaredType()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                string typeName = GetDeclaredTypeNames(tree).Single();

                Assert.That(
                    Path.GetFileNameWithoutExtension(fileName),
                    Is.EqualTo(typeName),
                    $"The file '{fileName}' declares type '{typeName}'; the filename stem must match the type identifier exactly.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenEveryFileIsChecked_ThenItDeclaresExactlyOneTopLevelType()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                IReadOnlyList<string> declaredTypes = GetDeclaredTypeNames(tree);

                Assert.That(
                    declaredTypes.Count,
                    Is.EqualTo(1),
                    $"The file '{Path.GetFileName(tree.FilePath)}' must declare exactly one top-level type but declares: {string.Join(", ", declaredTypes)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenEveryFileIsChecked_ThenNoNestedTypeOrTopLevelStatementExists()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<GlobalStatementSyntax>(),
                    Is.Empty,
                    $"The file '{fileName}' contains top-level statements; every file requires an explicit namespace and type block.");

                foreach (BaseTypeDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    Assert.That(
                        declaration.DescendantNodes().OfType<BaseTypeDeclarationSyntax>(),
                        Is.Empty,
                        $"The file '{fileName}' declares a nested type inside '{declaration.Identifier.Text}'; extract it into its own file.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenEveryFileIsChecked_ThenItDeclaresABlockNamespaceMatchingItsPath()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                SyntaxTree tree = ParseFile(file);
                NamespaceDeclarationSyntax? namespaceDeclaration = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                Assert.That(
                    namespaceDeclaration,
                    Is.Not.Null,
                    $"The file '{file.RelativePath}' must declare an explicit block namespace.");

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            namespaceDeclaration!.Name.ToString(),
                            Is.EqualTo(file.ExpectedNamespace),
                            $"The file '{file.RelativePath}' declares a namespace that does not match its folder path '{file.ExpectedNamespace}'.");

                        Assert.That(
                            tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                            Is.Empty,
                            $"The file '{file.RelativePath}' uses a file-scoped namespace; block namespaces are required.");
                    });
            }
        }

        [Test]
        public void GivenProductionSources_WhenUsingDirectivesAreChecked_ThenTheyPrecedeTheNamespaceWithoutGlobalQualifiers()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)tree.GetRoot();
                BaseNamespaceDeclarationSyntax? namespaceDeclaration = compilationUnit.DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                Assert.That(
                    namespaceDeclaration,
                    Is.Not.Null,
                    $"The file '{fileName}' must declare a namespace.");

                BaseNamespaceDeclarationSyntax declaredNamespace = namespaceDeclaration!;
                int namespaceStart = declaredNamespace.SpanStart;

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            declaredNamespace.Name.ToString(),
                            Is.Not.Empty,
                            $"The file '{fileName}' must declare a named namespace.");

                        foreach (UsingDirectiveSyntax usingDirective in compilationUnit.Usings)
                        {
                            Assert.That(
                                usingDirective.Span.End,
                                Is.LessThanOrEqualTo(namespaceStart),
                                $"The file '{fileName}' must place every using directive above and outside the namespace.");

                            Assert.That(
                                usingDirective.GlobalKeyword.IsKind(SyntaxKind.None),
                                Is.True,
                                $"The file '{fileName}' declares a global using; add the using to each file that requires it.");
                        }
                    });
            }
        }

        [Test]
        public void GivenProductionSources_WhenControlFlowIsChecked_ThenEveryBodyUsesBraces()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (IfStatementSyntax statement in root.DescendantNodes().OfType<IfStatementSyntax>())
                {
                    Assert.That(statement.Statement, Is.InstanceOf<BlockSyntax>(), $"The file '{fileName}' contains an 'if' body without braces.");
                }

                foreach (ElseClauseSyntax clause in root.DescendantNodes().OfType<ElseClauseSyntax>())
                {
                    bool isElseIf = clause.Statement is IfStatementSyntax;

                    Assert.That(
                        isElseIf || clause.Statement is BlockSyntax,
                        Is.True,
                        $"The file '{fileName}' contains an 'else' body without braces.");
                }

                foreach (ForStatementSyntax statement in root.DescendantNodes().OfType<ForStatementSyntax>())
                {
                    Assert.That(statement.Statement, Is.InstanceOf<BlockSyntax>(), $"The file '{fileName}' contains a 'for' body without braces.");
                }

                foreach (ForEachStatementSyntax statement in root.DescendantNodes().OfType<ForEachStatementSyntax>())
                {
                    Assert.That(statement.Statement, Is.InstanceOf<BlockSyntax>(), $"The file '{fileName}' contains a 'foreach' body without braces.");
                }

                foreach (WhileStatementSyntax statement in root.DescendantNodes().OfType<WhileStatementSyntax>())
                {
                    Assert.That(statement.Statement, Is.InstanceOf<BlockSyntax>(), $"The file '{fileName}' contains a 'while' body without braces.");
                }

                foreach (DoStatementSyntax statement in root.DescendantNodes().OfType<DoStatementSyntax>())
                {
                    Assert.That(statement.Statement, Is.InstanceOf<BlockSyntax>(), $"The file '{fileName}' contains a 'do' body without braces.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenDeclarationsAreChecked_ThenNoVarTernaryTupleOrRefTypeIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (VariableDeclarationSyntax declaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>())
                {
                    Assert.That(
                        declaration.Type.IsVar,
                        Is.False,
                        $"The file '{fileName}' declares a variable with 'var'; explicit types are required.");
                }

                Assert.That(
                    root.DescendantNodes().OfType<ConditionalExpressionSyntax>(),
                    Is.Empty,
                    $"The file '{fileName}' contains a ternary expression; use an 'if' statement or a switch expression instead.");

                Assert.That(
                    root.DescendantNodes().OfType<TupleExpressionSyntax>(),
                    Is.Empty,
                    $"The file '{fileName}' contains a tuple expression; return a dedicated type instead.");

                Assert.That(
                    root.DescendantNodes().OfType<TupleTypeSyntax>(),
                    Is.Empty,
                    $"The file '{fileName}' declares a tuple type; return a dedicated type instead.");

                Assert.That(
                    root.DescendantNodes().OfType<RefTypeSyntax>(),
                    Is.Empty,
                    $"The file '{fileName}' uses a ref type; ref parameters and returns are prohibited.");

                foreach (ParameterSyntax parameter in root.DescendantNodes().OfType<ParameterSyntax>())
                {
                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                parameter.Default,
                                Is.Null,
                                $"The file '{fileName}' declares the optional parameter '{parameter.Identifier.Text}'; use overloads instead.");

                            Assert.That(
                                parameter.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword)),
                                Is.False,
                                $"The file '{fileName}' declares the ref parameter '{parameter.Identifier.Text}'; ref parameters are prohibited.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMembersAreChecked_ThenNoMemberExceedsSevenParameters()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (BaseMethodDeclarationSyntax method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
                {
                    Assert.That(
                        method.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(maximumParameterCount),
                        $"A method or constructor in '{fileName}' declares {method.ParameterList.Parameters.Count} parameters; the maximum is {maximumParameterCount}.");
                }

                foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (type.ParameterList is null)
                    {
                        continue;
                    }

                    Assert.That(
                        type.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(maximumParameterCount),
                        $"The primary constructor of '{type.Identifier.Text}' in '{fileName}' declares {type.ParameterList.Parameters.Count} parameters; the maximum is {maximumParameterCount}.");
                }

                foreach (ParenthesizedLambdaExpressionSyntax lambda in root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>())
                {
                    Assert.That(
                        lambda.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(maximumParameterCount),
                        $"A lambda in '{fileName}' declares {lambda.ParameterList.Parameters.Count} parameters; the maximum is {maximumParameterCount}.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenFieldsAreChecked_ThenNoConstFieldOrInitAccessorIsDeclared()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (FieldDeclarationSyntax field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    Assert.That(
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)),
                        Is.False,
                        $"The file '{fileName}' declares a const field; use a static expression-bodied property instead.");
                }

                foreach (AccessorDeclarationSyntax accessor in root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
                {
                    Assert.That(
                        accessor.Keyword.IsKind(SyntaxKind.InitKeyword),
                        Is.False,
                        $"The file '{fileName}' declares an init accessor; use a regular setter.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenConditionalTextIsChecked_ThenNoRegionOrNullableDirectiveExistsAndNoEmptyCatchIsDeclared()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string source = File.ReadAllText(file.FullPath);
                SyntaxNode root = ParseFile(file).GetRoot();

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            source.Contains("#region", StringComparison.Ordinal) || source.Contains("#endregion", StringComparison.Ordinal),
                            Is.False,
                            $"The file '{file.RelativePath}' contains a region directive; regions are prohibited.");

                        Assert.That(
                            source.Contains("#nullable enable", StringComparison.Ordinal),
                            Is.False,
                            $"The file '{file.RelativePath}' contains '#nullable enable'; nullable reference types are configured in the project file.");

                        Assert.That(
                            root.DescendantNodes().OfType<CatchClauseSyntax>().Where(clause => clause.Block.Statements.Count == 0),
                            Is.Empty,
                            $"The file '{file.RelativePath}' declares an empty catch block; convert the failure into an explicit result or a sanitised exception.");
                    });
            }
        }

        [Test]
        public void GivenProductionSources_WhenNullHandlingIsChecked_ThenNoNullCoalescingOperatorIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Where(expression => expression.IsKind(SyntaxKind.CoalesceExpression)),
                    Is.Empty,
                    $"The file '{fileName}' uses the null-coalescing operator; use an explicit if statement. The null-coalescing assignment operator remains permitted.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenMembersAreDeclared_ThenEveryMemberDeclaresExplicitAccessibilityAndNoPartialTypeIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    if (!RequiresExplicitAccessibility(member))
                    {
                        continue;
                    }

                    Assert.That(
                        HasAccessibilityModifier(member.Modifiers),
                        Is.True,
                        $"A member in '{fileName}' omits its accessibility modifier; every type and member must declare it explicitly, including private.");
                }

                foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    Assert.That(
                        type.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)),
                        Is.False,
                        $"The file '{fileName}' declares the partial type '{type.Identifier.Text}'; partial types are prohibited.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMethodsContainASingleStatement_ThenTheyAreExpressionBodied()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Body is null || method.Body.Statements.Count != 1)
                    {
                        continue;
                    }

                    StatementSyntax statement = method.Body.Statements[0];
                    bool isExpressionBodyCandidate =
                        statement is ReturnStatementSyntax ||
                        statement is ThrowStatementSyntax ||
                        statement is ExpressionStatementSyntax;

                    Assert.That(
                        isExpressionBodyCandidate,
                        Is.False,
                        $"The method '{method.Identifier.Text}' in '{fileName}' declares a single {statement.Kind()} statement; use an expression body instead.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenIndentationIsChecked_ThenNoTabOrConsecutiveBlankLineOrBlankLineBeforeAClosingDelimiterExists()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string[] lines = File.ReadAllLines(file.FullPath);

                for (int index = 0; index < lines.Length; index++)
                {
                    string line = lines[index];
                    string trimmedLine = line.Trim();
                    bool previousLineIsBlank = index > 0 && string.IsNullOrWhiteSpace(lines[index - 1]);

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                line.StartsWith('\t'),
                                Is.False,
                                $"The file '{file.RelativePath}' uses a tab for indentation near line {index + 1}.");

                            Assert.That(
                                string.IsNullOrWhiteSpace(line) && previousLineIsBlank,
                                Is.False,
                                $"The file '{file.RelativePath}' contains consecutive blank lines near line {index + 1}.");

                            Assert.That(
                                trimmedLine is "}" or "]" or ")" && previousLineIsBlank,
                                Is.False,
                                $"The file '{file.RelativePath}' contains a blank line before a closing delimiter near line {index + 1}.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenConditionsExceedSeventyTwoCharacters_ThenEveryClauseIsSplitOnItsOwnLine()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);

                foreach (IfStatementSyntax statement in tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>())
                {
                    string conditionText = statement.Condition.ToString();
                    bool isMultiClause = conditionText.Contains("&&", StringComparison.Ordinal) || conditionText.Contains("||", StringComparison.Ordinal);

                    if (!isMultiClause)
                    {
                        continue;
                    }

                    FileLinePositionSpan conditionSpan = statement.Condition.GetLocation().GetLineSpan();
                    bool isSingleLine = conditionSpan.StartLinePosition.Line == conditionSpan.EndLinePosition.Line;

                    if (!isSingleLine)
                    {
                        continue;
                    }

                    string conditionLine = statement.SyntaxTree.GetText().Lines[conditionSpan.StartLinePosition.Line].ToString().Trim();

                    Assert.That(
                        conditionLine.Length,
                        Is.LessThanOrEqualTo(conditionalLineLimit),
                        $"An 'if' condition in '{fileName}' near line {conditionSpan.StartLinePosition.Line + 1} exceeds {conditionalLineLimit} characters; place one clause per line with the operators at line end.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenSignatureLinesExceedNinetySixCharacters_ThenEveryItemIsPlacedOnItsOwnLine()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (BaseMethodDeclarationSyntax method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
                {
                    AssertParameterListFits(method.ParameterList, fileName);
                }

                foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (type.ParameterList is not null)
                    {
                        AssertParameterListFits(type.ParameterList, fileName);
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenAnInterfaceHasAnImplementation_ThenTheirParameterNamesMatch()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = Path.GetFileName(tree.FilePath);
                SyntaxNode root = tree.GetRoot();

                foreach (InterfaceDeclarationSyntax interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                {
                    string implementationName = interfaceDeclaration.Identifier.Text.Length > 1
                        ? interfaceDeclaration.Identifier.Text[1..]
                        : interfaceDeclaration.Identifier.Text;

                    ClassDeclarationSyntax? implementation = root
                        .DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(candidate => string.Equals(candidate.Identifier.Text, implementationName, StringComparison.Ordinal));

                    if (implementation is null)
                    {
                        continue;
                    }

                    foreach (MethodDeclarationSyntax interfaceMethod in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
                    {
                        MethodDeclarationSyntax? implementationMethod = implementation.Members
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(candidate =>
                                string.Equals(candidate.Identifier.Text, interfaceMethod.Identifier.Text, StringComparison.Ordinal) &&
                                candidate.ParameterList.Parameters.Count == interfaceMethod.ParameterList.Parameters.Count);

                        if (implementationMethod is null)
                        {
                            continue;
                        }

                        for (int index = 0; index < interfaceMethod.ParameterList.Parameters.Count; index++)
                        {
                            Assert.That(
                                implementationMethod.ParameterList.Parameters[index].Identifier.Text,
                                Is.EqualTo(interfaceMethod.ParameterList.Parameters[index].Identifier.Text),
                                $"The method '{implementationMethod.Identifier.Text}' in '{fileName}' must use the same parameter names as its interface contract.");
                        }
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenNullChecksAreMade_ThenPatternsAreUsedInsteadOfEqualityOperators()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (BinaryExpressionSyntax binary in tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
                {
                    bool comparesWithNull =
                        binary.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                        binary.Right.IsKind(SyntaxKind.NullLiteralExpression);

                    if (!comparesWithNull)
                    {
                        continue;
                    }

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                binary.IsKind(SyntaxKind.EqualsExpression),
                                Is.False,
                                $"The file '{fileName}' uses '== null'; use the 'is null' pattern instead.");

                            Assert.That(
                                binary.IsKind(SyntaxKind.NotEqualsExpression),
                                Is.False,
                                $"The file '{fileName}' uses '!= null'; use the 'is not null' pattern instead.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCaughtExceptionsAreRethrown_ThenBareRethrowIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (CatchClauseSyntax catchClause in tree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>())
                {
                    if (catchClause.Declaration is null || string.IsNullOrWhiteSpace(catchClause.Declaration.Identifier.Text))
                    {
                        continue;
                    }

                    string caughtIdentifier = catchClause.Declaration.Identifier.Text;

                    foreach (ThrowStatementSyntax throwStatement in catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>())
                    {
                        if (throwStatement.Expression is not IdentifierNameSyntax identifier)
                        {
                            continue;
                        }

                        Assert.That(
                            string.Equals(identifier.Identifier.Text, caughtIdentifier, StringComparison.Ordinal),
                            Is.False,
                            $"The file '{fileName}' rethrows the caught exception '{caughtIdentifier}'; use a bare 'throw;' to preserve the stack trace.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenBuiltInTypesAreReferenced_ThenLowercaseAliasesAreUsed()
        {
            string[] bclTypeNames =
            [
                "String",
                "Int32",
                "Int64",
                "Int16",
                "Boolean",
                "Object",
                "Double",
                "Single",
                "Decimal",
                "Byte",
                "Char"
            ];

            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (IdentifierNameSyntax identifier in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    Assert.That(
                        bclTypeNames,
                        Does.Not.Contain(identifier.Identifier.Text),
                        $"The file '{fileName}' uses the BCL type name '{identifier.Identifier.Text}'; use the lowercase keyword alias instead.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenBooleanMembersAreNamed_ThenPermittedPrefixesAreUsed()
        {
            string[] permittedPrefixes = ["Is", "Has", "Does", "Are", "Was", "Were", "Can"];
            string[] forbiddenTokens = ["flag", "check", "result"];

            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (PropertyDeclarationSyntax property in tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    if (!string.Equals(property.Type.ToString(), "bool", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string name = property.Identifier.Text;

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                permittedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)),
                                Is.True,
                                $"The boolean property '{name}' in '{fileName}' must start with one of: {string.Join(", ", permittedPrefixes)}.");

                            foreach (string forbiddenToken in forbiddenTokens)
                            {
                                Assert.That(
                                    name.Contains(forbiddenToken, StringComparison.OrdinalIgnoreCase),
                                    Is.False,
                                    $"The boolean property '{name}' in '{fileName}' contains the forbidden token '{forbiddenToken}'.");
                            }
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenPrivateFieldsAreDeclared_ThenTheyAreCamelCaseWithoutUnderscorePrefix()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (FieldDeclarationSyntax field in tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    bool isInternalOrPublic =
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.ProtectedKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword));

                    if (isInternalOrPublic || field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)))
                    {
                        continue;
                    }

                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                    {
                        string name = variable.Identifier.Text;

                        Assert.Multiple(
                            () =>
                            {
                                Assert.That(
                                    name.StartsWith('_'),
                                    Is.False,
                                    $"The private field '{name}' in '{fileName}' must not use an underscore prefix.");

                                Assert.That(
                                    char.IsLower(name[0]),
                                    Is.True,
                                    $"The private field '{name}' in '{fileName}' must be camelCase.");
                            });
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenPublicAndProtectedMembersAreDeclared_ThenTheyStartWithAnUppercaseLetter()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (MemberDeclarationSyntax member in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    bool isPublicOrProtected =
                        member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.ProtectedKeyword));

                    if (!isPublicOrProtected)
                    {
                        continue;
                    }

                    if (member is MethodDeclarationSyntax method && method.ExplicitInterfaceSpecifier is null)
                    {
                        Assert.That(
                            char.IsUpper(method.Identifier.Text[0]),
                            Is.True,
                            $"The public method '{method.Identifier.Text}' in '{fileName}' must start with an uppercase letter.");
                    }

                    if (member is PropertyDeclarationSyntax property && property.ExplicitInterfaceSpecifier is null)
                    {
                        Assert.That(
                            char.IsUpper(property.Identifier.Text[0]),
                            Is.True,
                            $"The public property '{property.Identifier.Text}' in '{fileName}' must start with an uppercase letter.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMemberKindsAreOrdered_ThenFieldsPrecedePropertiesPrecedeEventsPrecedeConstructorsPrecedeMethods()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    int lastRank = -1;

                    foreach (MemberDeclarationSyntax member in type.Members)
                    {
                        int currentRank = GetMemberKindRank(member);

                        if (currentRank < 0)
                        {
                            continue;
                        }

                        Assert.That(
                            currentRank,
                            Is.GreaterThanOrEqualTo(lastRank),
                            $"In '{fileName}', member '{GetMemberIdentifier(member)}' violates member-kind ordering: fields, then properties, then events, then constructors, then methods.");

                        lastRank = currentRank;
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenPrivateMethodsAvoidInstanceState_ThenTheyAreStatic()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    IReadOnlyList<string> instanceMembers = SourceAnalysis.GetInstanceMemberNames(classDeclaration);

                    if (instanceMembers.Count == 0)
                    {
                        continue;
                    }

                    foreach (MethodDeclarationSyntax method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
                    {
                        bool isPrivate = method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword));
                        bool isStatic = method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));

                        if (!isPrivate || isStatic)
                        {
                            continue;
                        }

                        SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

                        if (body is null)
                        {
                            continue;
                        }

                        bool usesThis = body.DescendantNodes().OfType<ThisExpressionSyntax>().Any();
                        bool usesInstanceMember = body
                            .DescendantNodes()
                            .OfType<IdentifierNameSyntax>()
                            .Any(identifier => instanceMembers.Contains(identifier.Identifier.Text, StringComparer.Ordinal));

                        Assert.That(
                            usesThis || usesInstanceMember,
                            Is.True,
                            $"The private method '{method.Identifier.Text}' in '{fileName}' does not access instance state; mark it static.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenAsynchronousMethodsDeclareACancellationToken_ThenItIsTheLastParameter()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.ParameterList.Parameters.Count <= 1)
                    {
                        continue;
                    }

                    for (int index = 0; index < method.ParameterList.Parameters.Count; index++)
                    {
                        if (!string.Equals(method.ParameterList.Parameters[index].Type?.ToString(), "CancellationToken", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        Assert.That(
                            index,
                            Is.EqualTo(method.ParameterList.Parameters.Count - 1),
                            $"The method '{method.Identifier.Text}' in '{fileName}' must place CancellationToken as its final parameter.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenStringPrefixOrSuffixChecksUseASingleCharacterString_ThenCharacterLiteralsAreUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                        invocation.ArgumentList.Arguments.Count != 1)
                    {
                        continue;
                    }

                    string methodName = memberAccess.Name.Identifier.Text;

                    if (!string.Equals(methodName, "StartsWith", StringComparison.Ordinal) &&
                        !string.Equals(methodName, "EndsWith", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal ||
                        !literal.IsKind(SyntaxKind.StringLiteralExpression) ||
                        literal.Token.ValueText.Length != 1)
                    {
                        continue;
                    }

                    Assert.That(
                        false,
                        $"The file '{fileName}' calls '{methodName}' with a single-character string; use a character literal instead.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCollectionPropertiesAreDeclared_ThenIEnumerableIsPreferredOutsideXmlSerialisedEntities()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);
                bool isXmlSerialisedEntity = fileName.EndsWith("Entity.cs", StringComparison.Ordinal);

                if (isXmlSerialisedEntity)
                {
                    continue;
                }

                foreach (PropertyDeclarationSyntax property in tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    string typeText = property.Type.ToString();
                    bool isInterfaceCollection = typeText.StartsWith("IEnumerable<", StringComparison.Ordinal);
                    bool isConcreteCollection =
                        typeText.StartsWith("List<", StringComparison.Ordinal) ||
                        typeText.StartsWith("IList<", StringComparison.Ordinal) ||
                        typeText.StartsWith("IReadOnlyList<", StringComparison.Ordinal) ||
                        typeText.StartsWith("HashSet<", StringComparison.Ordinal);

                    Assert.That(
                        isConcreteCollection && !isInterfaceCollection,
                        Is.False,
                        $"The property '{property.Identifier.Text}' in '{fileName}' uses collection type '{typeText}'; IEnumerable<T> is required.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenTargetTypeIsAlreadyDeclared_ThenTargetTypedConstructionIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ObjectCreationExpressionSyntax creation in tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    if (creation.Parent is not EqualsValueClauseSyntax equalsClause ||
                        equalsClause.Parent is not VariableDeclaratorSyntax variableDeclarator ||
                        variableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration ||
                        variableDeclaration.Type.IsVar)
                    {
                        continue;
                    }

                    string declaredTypeName = variableDeclaration.Type.ToString();
                    string createdTypeName = creation.Type.ToString();

                    Assert.That(
                        string.Equals(declaredTypeName, createdTypeName, StringComparison.Ordinal),
                        Is.False,
                        $"The variable '{variableDeclarator.Identifier.Text}' in '{fileName}' repeats type '{createdTypeName}' in 'new'; use target-typed 'new(...)' instead.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMappingMethodsAreDeclared_ThenTheyAreExpressionBodiedWithoutTemporaryLocals()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    bool isMappingMethod =
                        method.Identifier.Text.StartsWith("ToServiceModel", StringComparison.Ordinal) ||
                        method.Identifier.Text.StartsWith("ToDataObject", StringComparison.Ordinal);

                    if (!isMappingMethod)
                    {
                        continue;
                    }

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                method.Body,
                                Is.Null,
                                $"The mapping method '{method.Identifier.Text}' in '{fileName}' must be expression-bodied; temporary locals are prohibited.");

                            Assert.That(
                                method.ExpressionBody,
                                Is.Not.Null,
                                $"The mapping method '{method.Identifier.Text}' in '{fileName}' must be expression-bodied.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMappingExtensionClassesAreDeclared_ThenTheyAreInternalStaticAndCorrectlyNamed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool hasMappingMethods = classDeclaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(method =>
                            method.Identifier.Text.StartsWith("ToServiceModel", StringComparison.Ordinal) ||
                            method.Identifier.Text.StartsWith("ToDataObject", StringComparison.Ordinal));

                    if (!hasMappingMethods)
                    {
                        continue;
                    }

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)),
                                Is.True,
                                $"The mapping type '{classDeclaration.Identifier.Text}' in '{fileName}' must be static.");

                            Assert.That(
                                classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.InternalKeyword)),
                                Is.True,
                                $"The mapping type '{classDeclaration.Identifier.Text}' in '{fileName}' must be internal.");

                            Assert.That(
                                classDeclaration.Identifier.Text,
                                Does.EndWith("MappingExtensions"),
                                $"The mapping type '{classDeclaration.Identifier.Text}' in '{fileName}' must be named '<Noun>MappingExtensions'.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCustomExceptionTypesAreDeclared_ThenTheExceptionContractIsFollowed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool inheritsException = classDeclaration.BaseList is not null &&
                        classDeclaration.BaseList.Types.Any(type => type.Type.ToString().EndsWith("Exception", StringComparison.Ordinal));

                    if (!inheritsException)
                    {
                        continue;
                    }

                    string className = classDeclaration.Identifier.Text;

                    bool hasInnerExceptionOverload = classDeclaration.Members
                        .OfType<ConstructorDeclarationSyntax>()
                        .Any(constructor => constructor.ParameterList.Parameters.Any(parameter => string.Equals(parameter.Type?.ToString(), "Exception", StringComparison.Ordinal)));

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                className,
                                Does.EndWith("Exception"),
                                $"The custom exception '{className}' in '{fileName}' must end with 'Exception'.");

                            Assert.That(
                                hasInnerExceptionOverload,
                                Is.True,
                                $"The custom exception '{className}' in '{fileName}' must declare a constructor accepting an inner Exception.");
                        });
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenNuciLogIsUsed_ThenEveryCallSuppliesAnOperationAndPassesExceptionsDirectly()
        {
            string[] loggingMethods = ["Debug", "Info", "Notice", "Warn", "Error", "Fatal", "Verbose"];

            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    {
                        continue;
                    }

                    if (!loggingMethods.Contains(memberAccess.Name.Identifier.Text, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        string firstArgument = invocation.ArgumentList.Arguments[0].ToString();

                        Assert.That(
                            firstArgument.Contains("Operation", StringComparison.Ordinal),
                            Is.True,
                            $"A NuciLog call in '{fileName}' near line {SourceAnalysis.GetStartLine(invocation) + 1} must supply an Operation as its first argument.");
                    }

                    foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
                    {
                        string argumentText = argument.ToString();

                        Assert.Multiple(
                            () =>
                            {
                                Assert.That(
                                    argumentText.Contains(".Message", StringComparison.Ordinal),
                                    Is.False,
                                    $"A NuciLog call in '{fileName}' passes '.Message'; pass the exception object instead.");

                                Assert.That(
                                    argumentText.Contains(".StackTrace", StringComparison.Ordinal),
                                    Is.False,
                                    $"A NuciLog call in '{fileName}' passes '.StackTrace'; pass the exception object instead.");
                            });
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenBlockControlFlowIsAdjacentToStatements_ThenBlankLinesSurroundIt()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);
                IReadOnlyList<string> lines = SourceAnalysis.GetLines(tree);

                foreach (BlockSyntax block in tree.GetRoot().DescendantNodes().OfType<BlockSyntax>())
                {
                    for (int index = 0; index < block.Statements.Count; index++)
                    {
                        StatementSyntax statement = block.Statements[index];

                        if (!SourceAnalysis.IsBlockControlFlow(statement))
                        {
                            continue;
                        }

                        if (index > 0 && !SourceAnalysis.IsBlockControlFlow(block.Statements[index - 1]))
                        {
                            int startLine = SourceAnalysis.GetStartLine(statement);

                            Assert.That(
                                SourceAnalysis.IsBlankLine(lines, startLine - 1),
                                Is.True,
                                $"In '{fileName}' near line {startLine + 1}, block control flow requires a blank line above when preceded by non-control-flow statements.");
                        }

                        if (index < block.Statements.Count - 1 && !SourceAnalysis.IsBlockControlFlow(block.Statements[index + 1]))
                        {
                            int endLine = SourceAnalysis.GetEndLine(statement);

                            Assert.That(
                                SourceAnalysis.IsBlankLine(lines, endLine + 1),
                                Is.True,
                                $"In '{fileName}' near line {endLine + 1}, block control flow requires a blank line below when followed by non-control-flow statements.");
                        }
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenReturnOrThrowIsPrecededByStatements_ThenABlankLineSeparatesThemOrTheRulesApply()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);
                IReadOnlyList<string> lines = SourceAnalysis.GetLines(tree);

                foreach (BlockSyntax block in tree.GetRoot().DescendantNodes().OfType<BlockSyntax>())
                {
                    for (int index = 1; index < block.Statements.Count; index++)
                    {
                        StatementSyntax statement = block.Statements[index];

                        if (statement is not (ReturnStatementSyntax or ThrowStatementSyntax))
                        {
                            continue;
                        }

                        if (SourceAnalysis.IsBlockControlFlow(block.Statements[index - 1]))
                        {
                            continue;
                        }

                        int startLine = SourceAnalysis.GetStartLine(statement);

                        Assert.That(
                            SourceAnalysis.IsBlankLine(lines, startLine - 1),
                            Is.True,
                            $"In '{fileName}' near line {startLine + 1}, a return or throw statement requires a blank line above when preceded by non-control-flow statements.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenAnElseClauseIsFollowedByAnIf_ThenElseIfIsOnOneLine()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ElseClauseSyntax elseClause in tree.GetRoot().DescendantNodes().OfType<ElseClauseSyntax>())
                {
                    if (elseClause.Statement is not IfStatementSyntax elseIf)
                    {
                        continue;
                    }

                    int elseLine = SourceAnalysis.GetStartLine(elseClause);
                    int ifLine = SourceAnalysis.GetStartLine(elseIf);

                    Assert.That(
                        elseLine,
                        Is.EqualTo(ifLine),
                        $"In '{fileName}' near line {elseLine + 1}, 'else if' must remain on a single line.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenABlockIsOpened_ThenTheOpeningBraceOccupiesItsOwnLine()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (BlockSyntax block in tree.GetRoot().DescendantNodes().OfType<BlockSyntax>())
                {
                    if (block.Parent is MethodDeclarationSyntax || block.Parent is LocalFunctionStatementSyntax)
                    {
                        int braceLine = SourceAnalysis.GetStartLine(block);
                        int parentLine = SourceAnalysis.GetStartLine(block.Parent);

                        Assert.That(
                            braceLine,
                            Is.GreaterThan(parentLine),
                            $"In '{fileName}' near line {braceLine + 1}, the opening brace must occupy its own line.");
                    }

                    if (block.Parent is IfStatementSyntax ||
                        block.Parent is ForStatementSyntax ||
                        block.Parent is ForEachStatementSyntax ||
                        block.Parent is WhileStatementSyntax ||
                        block.Parent is DoStatementSyntax)
                    {
                        int braceLine = SourceAnalysis.GetStartLine(block);
                        int parentLine = SourceAnalysis.GetStartLine(block.Parent);

                        Assert.That(
                            braceLine,
                            Is.GreaterThan(parentLine),
                            $"In '{fileName}' near line {braceLine + 1}, the opening brace must occupy its own line.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenMethodsAreSeparated_ThenExactlyOneBlankLineAppearsBetweenThem()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);
                IReadOnlyList<string> lines = SourceAnalysis.GetLines(tree);

                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    IReadOnlyList<MethodDeclarationSyntax> methods = [.. type.Members.OfType<MethodDeclarationSyntax>()];

                    for (int index = 1; index < methods.Count; index++)
                    {
                        int previousEndLine = SourceAnalysis.GetEndLine(methods[index - 1]);
                        int currentStartLine = SourceAnalysis.GetStartLine(methods[index]);

                        if (currentStartLine - previousEndLine <= 1)
                        {
                            continue;
                        }

                        int blankLineCount = 0;

                        for (int lineIndex = previousEndLine + 1; lineIndex < currentStartLine; lineIndex++)
                        {
                            if (SourceAnalysis.IsBlankLine(lines, lineIndex))
                            {
                                blankLineCount += 1;
                            }
                        }

                        Assert.That(
                            blankLineCount,
                            Is.EqualTo(1),
                            $"In '{fileName}', methods '{methods[index - 1].Identifier.Text}' and '{methods[index].Identifier.Text}' must be separated by exactly one blank line.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenAValueIsIncrementedByOne_ThenTheCompoundAssignmentIsUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (AssignmentExpressionSyntax assignment in tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    {
                        continue;
                    }

                    if (assignment.Right is not BinaryExpressionSyntax binary ||
                        !binary.IsKind(SyntaxKind.AddExpression) && !binary.IsKind(SyntaxKind.SubtractExpression))
                    {
                        continue;
                    }

                    bool sameOperand =
                        string.Equals(assignment.Left.ToString(), binary.Left.ToString(), StringComparison.Ordinal) ||
                        string.Equals(assignment.Left.ToString(), binary.Right.ToString(), StringComparison.Ordinal);

                    bool isOne =
                        binary.Left.IsKind(SyntaxKind.NumericLiteralExpression) && binary.Left.ToString() == "1" ||
                        binary.Right.IsKind(SyntaxKind.NumericLiteralExpression) && binary.Right.ToString() == "1";

                    if (!sameOperand || !isOne)
                    {
                        continue;
                    }

                    Assert.That(
                        false,
                        $"In '{fileName}', assignment '{assignment}' repeats the operand; use the compound '+=' or '-=' form.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenNamedEnumerationsNamespaceIsUsed_ThenTheTypeIsPlacedBesideItsDomainModels()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                Assert.Multiple(
                    () =>
                    {
                        Assert.That(
                            file.ExpectedNamespace,
                            Does.Not.EndWith(".Enumerations"),
                            $"The file '{file.RelativePath}' resides in an '.Enumerations' namespace; place enumerations beside their domain models.");

                        Assert.That(
                            file.ExpectedNamespace,
                            Does.Not.EndWith(".Enums"),
                            $"The file '{file.RelativePath}' resides in an '.Enums' namespace; place enumerations beside their domain models.");
                    });
            }
        }

        [Test]
        public void GivenProductionSources_WhenDateValuesAreParsedOrFormatted_ThenACultureIsSupplied()
        {
            string[] dateMethods = ["Parse", "TryParse", "ParseExact", "TryParseExact", "ToString"];

            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    {
                        continue;
                    }

                    if (memberAccess.Expression is not IdentifierNameSyntax receiver ||
                        !string.Equals(receiver.Identifier.Text, "DateTime", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!dateMethods.Contains(memberAccess.Name.Identifier.Text, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    bool suppliesCulture = invocation.ArgumentList.Arguments
                        .Any(argument => argument.ToString().Contains("Culture", StringComparison.Ordinal) || argument.ToString().Contains("Format", StringComparison.Ordinal));

                    Assert.That(
                        suppliesCulture,
                        Is.True,
                        $"In '{fileName}', the DateTime call '{memberAccess.Name.Identifier.Text}' must supply a format provider or format string.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenJsonSerializerOptionsAreCreated_ThenTheyAreStaticReadonly()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ObjectCreationExpressionSyntax creation in tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    if (!creation.Type.ToString().StartsWith("JsonSerializerOptions", StringComparison.Ordinal) &&
                        !creation.Type.ToString().EndsWith("JsonSerializerOptions", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bool isStaticReadonlyField =
                        creation.Parent is EqualsValueClauseSyntax equalsClause &&
                        equalsClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                        variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration &&
                        variableDeclaration.Parent is FieldDeclarationSyntax field &&
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)) &&
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));

                    Assert.That(
                        isStaticReadonlyField,
                        Is.True,
                        $"In '{fileName}', JsonSerializerOptions must be cached in a static readonly field rather than allocated per call.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenALogInfoKeyIsSensitive_ThenTheSensitiveBaseConstructorIsUsed()
        {
            string[] sensitiveTokens = ["password", "secret", "token", "credential", "apikey", "apiKey"];

            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool derivesFromLogInfoKey = classDeclaration.BaseList is not null &&
                        classDeclaration.BaseList.Types.Any(type => type.Type.ToString().EndsWith("LogInfoKey", StringComparison.Ordinal));

                    if (!derivesFromLogInfoKey)
                    {
                        continue;
                    }

                    foreach (ConstructorInitializerSyntax initializer in classDeclaration.DescendantNodes().OfType<ConstructorInitializerSyntax>())
                    {
                        if (!initializer.IsKind(SyntaxKind.BaseConstructorInitializer))
                        {
                            continue;
                        }

                        bool hasSensitiveToken = sensitiveTokens.Any(token =>
                            initializer.ToString().Contains(token, StringComparison.OrdinalIgnoreCase));

                        if (!hasSensitiveToken)
                        {
                            continue;
                        }

                        Assert.That(
                            initializer.ArgumentList.Arguments.Any(argument => argument.ToString() == "true"),
                            Is.True,
                            $"The sensitive LogInfoKey constructor in '{fileName}' must pass true to base(name, true).");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenValuesAreCompared_ThenStaticEqualsIsPreferredOverOperators()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (BinaryExpressionSyntax binary in tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
                {
                    bool isEquality = binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression);

                    if (!isEquality)
                    {
                        continue;
                    }

                    bool comparesWithNull =
                        binary.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                        binary.Right.IsKind(SyntaxKind.NullLiteralExpression);

                    if (comparesWithNull)
                    {
                        continue;
                    }

                    bool comparesEnumOrBoolean =
                        binary.Left is MemberAccessExpressionSyntax memberAccess &&
                        (memberAccess.Name.Identifier.Text is "Provider" or "Status" || binary.Right is LiteralExpressionSyntax);

                    if (comparesEnumOrBoolean)
                    {
                        continue;
                    }

                    Assert.That(
                        false,
                        $"In '{fileName}' near line {SourceAnalysis.GetStartLine(binary) + 1}, operator comparison '{binary}' is used; prefer static Equals(a, b) or string.Equals(a, b).");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenDefaultValuesAreInitialised_ThenFrameworkDefaultsAreOmitted()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (PropertyDeclarationSyntax property in tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    if (property.Initializer is null)
                    {
                        continue;
                    }

                    string initialValue = property.Initializer.Value.ToString();

                    bool isRedundantDefault =
                        initialValue is "null" or "false" or "0" or "0.0" or "0f" or "0d" or "0m" or "'\\0'" ||
                        string.Equals(initialValue, "string.Empty", StringComparison.Ordinal) && property.Type.ToString() == "string";

                    if (!isRedundantDefault)
                    {
                        continue;
                    }

                    Assert.That(
                        string.Equals(property.Type.ToString(), "string", StringComparison.Ordinal) && string.Equals(initialValue, "string.Empty", StringComparison.Ordinal),
                        Is.False,
                        $"The property '{property.Identifier.Text}' in '{fileName}' initialises a framework default ('{initialValue}') that repeats the type default.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenOverloadsAreDeclared_ThenTheyAreAdjacentAndOrderedByParameterCount()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    List<MethodDeclarationSyntax> methods = [.. type.Members.OfType<MethodDeclarationSyntax>()];
                    Dictionary<string, List<int>> methodGroupPositions = [];

                    for (int index = 0; index < methods.Count; index++)
                    {
                        string name = methods[index].Identifier.Text;

                        if (!methodGroupPositions.ContainsKey(name))
                        {
                            methodGroupPositions[name] = [];
                        }

                        methodGroupPositions[name].Add(index);
                    }

                    foreach (KeyValuePair<string, List<int>> group in methodGroupPositions)
                    {
                        if (group.Value.Count < 2)
                        {
                            continue;
                        }

                        for (int index = 1; index < group.Value.Count; index++)
                        {
                            Assert.That(
                                group.Value[index],
                                Is.EqualTo(group.Value[index - 1] + 1),
                                $"In '{fileName}', overloads of '{group.Key}' must be adjacent.");
                        }

                        List<int> parameterCounts = [.. group.Value.Select(position => methods[position].ParameterList.Parameters.Count)];

                        Assert.That(
                            parameterCounts,
                            Is.Ordered,
                            $"In '{fileName}', overloads of '{group.Key}' must be ordered from fewest to most parameters.");
                    }
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCastsAreMade_ThenAsIsUsedWhereFailureIsPossible()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (CastExpressionSyntax cast in tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>())
                {
                    string targetType = cast.Type.ToString();

                    if (targetType is "int" or "long" or "double" or "decimal" or "float" or "byte" or "char" or "short")
                    {
                        continue;
                    }

                    if (cast.Expression is LiteralExpressionSyntax || cast.Expression is IdentifierNameSyntax && cast.Expression.ToString() == "this")
                    {
                        continue;
                    }

                    Assert.That(
                        false,
                        $"In '{fileName}' near line {SourceAnalysis.GetStartLine(cast) + 1}, a direct cast to '{targetType}' is used; prefer 'as' when the cast may fail.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenArrayArgumentsAreRepeated_ThenTheyAreStoredInStaticReadonlyFields()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ArrayCreationExpressionSyntax arrayCreation in tree.GetRoot().DescendantNodes().OfType<ArrayCreationExpressionSyntax>())
                {
                    if (arrayCreation.Initializer is null)
                    {
                        continue;
                    }

                    bool isFieldOrLocalInitialiser =
                        arrayCreation.Parent is EqualsValueClauseSyntax;

                    if (!isFieldOrLocalInitialiser)
                    {
                        continue;
                    }

                    EqualsValueClauseSyntax? equalsClause = arrayCreation.Parent as EqualsValueClauseSyntax;
                    VariableDeclaratorSyntax? declarator = equalsClause?.Parent as VariableDeclaratorSyntax;
                    VariableDeclarationSyntax? declaration = declarator?.Parent as VariableDeclarationSyntax;
                    FieldDeclarationSyntax? field = declaration?.Parent as FieldDeclarationSyntax;

                    if (field is null)
                    {
                        continue;
                    }

                    bool isStaticReadonly =
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)) &&
                        field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));

                    Assert.That(
                        isStaticReadonly,
                        Is.True,
                        $"The array field '{declarator!.Identifier.Text}' in '{fileName}' must be static readonly.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCollectionsAreInitialisedInline_ThenCollectionExpressionsAreUsed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ObjectCreationExpressionSyntax creation in tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    string createdType = creation.Type.ToString();

                    bool isCollectionType =
                        createdType.StartsWith("List<", StringComparison.Ordinal) ||
                        createdType.StartsWith("Dictionary<", StringComparison.Ordinal) ||
                        createdType.StartsWith("HashSet<", StringComparison.Ordinal);

                    if (!isCollectionType)
                    {
                        continue;
                    }

                    bool isFieldOrLocalInitialisation = creation.Parent is EqualsValueClauseSyntax;
                    bool hasInitializer = creation.Initializer is not null || creation.ArgumentList is not null && creation.ArgumentList.Arguments.Count == 0;

                    if (!isFieldOrLocalInitialisation || !hasInitializer)
                    {
                        continue;
                    }

                    Assert.That(
                        false,
                        $"In '{fileName}' near line {SourceAnalysis.GetStartLine(creation) + 1}, the inline collection '{createdType}' must use a collection expression '[...]'.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenInterfacesAreImplemented_ThenTheClassIsSealed()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (classDeclaration.BaseList is null)
                    {
                        continue;
                    }

                    bool implementsInterface = classDeclaration.BaseList.Types.Any(type => type.Type is IdentifierNameSyntax identifier && identifier.Identifier.Text.StartsWith('I'));

                    if (!implementsInterface)
                    {
                        continue;
                    }

                    Assert.That(
                        classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.SealedKeyword)),
                        Is.True,
                        $"The class '{classDeclaration.Identifier.Text}' in '{fileName}' implements an interface and must be sealed unless designed for inheritance.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenModelAndSettingsTypesAreDeclared_ThenTheyArePublicSealedClasses()
        {
            foreach (SyntaxTree tree in productionTrees)
            {
                string fileName = SourceAnalysis.FileName(tree);

                foreach (ClassDeclarationSyntax classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    string typeName = classDeclaration.Identifier.Text;

                    bool isModelOrSettings =
                        typeName.EndsWith("Settings", StringComparison.Ordinal) ||
                        typeName.EndsWith("Options", StringComparison.Ordinal) ||
                        typeName.EndsWith("Entity", StringComparison.Ordinal);

                    if (!isModelOrSettings)
                    {
                        continue;
                    }

                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(
                                classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)),
                                Is.True,
                                $"The model or settings type '{typeName}' in '{fileName}' must be public.");

                            Assert.That(
                                classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.SealedKeyword)),
                                Is.True,
                                $"The model or settings type '{typeName}' in '{fileName}' must be sealed.");
                        });
                }
            }
        }

        static void AssertParameterListFits(ParameterListSyntax parameterList, string fileName)
        {
            FileLinePositionSpan span = parameterList.GetLocation().GetLineSpan();

            if (span.StartLinePosition.Line != span.EndLinePosition.Line)
            {
                return;
            }

            string signatureLine = parameterList.SyntaxTree.GetText().Lines[span.StartLinePosition.Line].ToString().Trim();

            Assert.That(
                signatureLine.Length,
                Is.LessThanOrEqualTo(signatureLineLimit),
                $"A signature in '{fileName}' near line {span.StartLinePosition.Line + 1} exceeds {signatureLineLimit} characters; place every parameter on its own line.");
        }

        static bool RequiresExplicitAccessibility(MemberDeclarationSyntax member)
        {
            if (member is BaseNamespaceDeclarationSyntax || member is IncompleteMemberSyntax || member is GlobalStatementSyntax)
            {
                return false;
            }

            if (member is EnumMemberDeclarationSyntax)
            {
                return false;
            }

            if (member.Parent is InterfaceDeclarationSyntax)
            {
                return false;
            }

            if (member is MethodDeclarationSyntax method && method.ExplicitInterfaceSpecifier is not null)
            {
                return false;
            }

            if (member is PropertyDeclarationSyntax property && property.ExplicitInterfaceSpecifier is not null)
            {
                return false;
            }

            if (member is ConstructorDeclarationSyntax constructor && constructor.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            {
                return false;
            }

            return true;
        }

        static bool HasAccessibilityModifier(SyntaxTokenList modifiers)
            => modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword));

        static int GetMemberKindRank(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax)
            {
                return 0;
            }

            if (member is PropertyDeclarationSyntax)
            {
                return 1;
            }

            if (member is EventFieldDeclarationSyntax || member is EventDeclarationSyntax)
            {
                return 2;
            }

            if (member is ConstructorDeclarationSyntax || member is DestructorDeclarationSyntax)
            {
                return 3;
            }

            if (member is MethodDeclarationSyntax)
            {
                return 4;
            }

            return -1;
        }

        static string GetMemberIdentifier(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax field)
            {
                return field.Declaration.Variables.First().Identifier.Text;
            }

            if (member is PropertyDeclarationSyntax property)
            {
                return property.Identifier.Text;
            }

            if (member is MethodDeclarationSyntax method)
            {
                return method.Identifier.Text;
            }

            if (member is ConstructorDeclarationSyntax constructor)
            {
                return constructor.Identifier.Text;
            }

            return member.Kind().ToString();
        }

        static IReadOnlyList<string> GetDeclaredTypeNames(SyntaxTree tree)
            => [.. tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)
                .Select(declaration => declaration.Identifier.Text)
                .Concat(
                    tree.GetRoot()
                        .DescendantNodes()
                        .OfType<DelegateDeclarationSyntax>()
                        .Where(declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)
                        .Select(declaration => declaration.Identifier.Text))];

        static SyntaxTree[] BuildSyntaxTrees(IEnumerable<ArchitectureSourceFile> files)
            => [.. files.Select(ParseFile)];

        static SyntaxTree ParseFile(ArchitectureSourceFile file)
            => CSharpSyntaxTree.ParseText(
                File.ReadAllText(file.FullPath),
                new CSharpParseOptions(LanguageVersion.Latest),
                path: file.FullPath);
    }
}