using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StreamToM3U.ArchitectureTests
{
    /// <summary>
    /// Provides shared Roslyn parsing and source-text helpers for the architecture checks.
    /// Every helper works on the staged sources only, so no check depends on the host
    /// file-system layout or the current working directory.
    /// </summary>
    public static class SourceAnalysis
    {
        static readonly Lazy<IReadOnlyList<SyntaxTree>> productionTrees = new(() => BuildTrees(ArchitectureSourceRepository.ProductionFiles));
        static readonly Lazy<IReadOnlyList<SyntaxTree>> testTrees = new(() => BuildTrees(ArchitectureSourceRepository.TestFiles));

        /// <summary>
        /// Gets the parsed production syntax trees in deterministic order.
        /// </summary>
        public static IReadOnlyList<SyntaxTree> ProductionTrees => productionTrees.Value;

        /// <summary>
        /// Gets the parsed test syntax trees in deterministic order.
        /// </summary>
        public static IReadOnlyList<SyntaxTree> TestTrees => testTrees.Value;

        /// <summary>
        /// Gets the production source files in deterministic order.
        /// </summary>
        public static IReadOnlyList<ArchitectureSourceFile> ProductionFiles
            => [.. ArchitectureSourceRepository.ProductionFiles];

        /// <summary>
        /// Gets the test source files in deterministic order.
        /// </summary>
        public static IReadOnlyList<ArchitectureSourceFile> TestFiles
            => [.. ArchitectureSourceRepository.TestFiles];

        /// <summary>
        /// Parses a staged source file with the latest language version.
        /// </summary>
        /// <param name="file">The staged file to parse.</param>
        public static SyntaxTree ParseFile(ArchitectureSourceFile file)
            => CSharpSyntaxTree.ParseText(
                File.ReadAllText(file.FullPath),
                new CSharpParseOptions(LanguageVersion.Latest),
                path: file.FullPath);

        /// <summary>
        /// Gets the bare file name of a parsed source tree.
        /// </summary>
        /// <param name="tree">The parsed tree.</param>
        public static string FileName(SyntaxTree tree)
            => Path.GetFileName(tree.FilePath) ?? tree.FilePath;

        /// <summary>
        /// Gets every type declaration whose parent is a namespace.
        /// </summary>
        /// <param name="tree">The parsed tree.</param>
        public static IReadOnlyList<BaseTypeDeclarationSyntax> GetTopLevelTypes(SyntaxTree tree)
            => [.. tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(declaration => declaration.Parent is BaseNamespaceDeclarationSyntax)];

        /// <summary>
        /// Gets the members declared directly inside a type.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        public static IReadOnlyList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax type)
            => [.. type.Members];

        /// <summary>
        /// Gets every source line of a parsed tree.
        /// </summary>
        /// <param name="tree">The parsed tree.</param>
        public static IReadOnlyList<string> GetLines(SyntaxTree tree)
            => [.. tree.GetText().Lines.Select(line => line.ToString())];

        /// <summary>
        /// Gets the first zero-based line of a syntax node.
        /// </summary>
        /// <param name="node">The node to locate.</param>
        public static int GetStartLine(SyntaxNode node)
            => node.GetLocation().GetLineSpan().StartLinePosition.Line;

        /// <summary>
        /// Gets the last zero-based line of a syntax node.
        /// </summary>
        /// <param name="node">The node to locate.</param>
        public static int GetEndLine(SyntaxNode node)
            => node.GetLocation().GetLineSpan().EndLinePosition.Line;

        /// <summary>
        /// Determines whether a zero-based line index is inside the supplied lines and blank.
        /// </summary>
        /// <param name="lines">The source lines.</param>
        /// <param name="lineIndex">The zero-based line index.</param>
        public static bool IsBlankLine(IReadOnlyList<string> lines, int lineIndex)
            => lineIndex >= 0 && lineIndex < lines.Count && string.IsNullOrWhiteSpace(lines[lineIndex]);

        /// <summary>
        /// Determines whether a syntax token declares one of the accessibility modifiers.
        /// </summary>
        /// <param name="modifiers">The token list to inspect.</param>
        public static bool HasAccessibilityModifier(SyntaxTokenList modifiers)
            => modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword));

        /// <summary>
        /// Gets the accessibility rank of a token list: public, then protected, then private.
        /// </summary>
        /// <param name="modifiers">The token list to inspect.</param>
        public static int GetAccessibilityRank(SyntaxTokenList modifiers)
        {
            if (modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
            {
                return 0;
            }

            if (modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ProtectedKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword)))
            {
                return 1;
            }

            return 2;
        }

        /// <summary>
        /// Determines whether a statement is a block control-flow statement.
        /// </summary>
        /// <param name="statement">The statement to inspect.</param>
        public static bool IsBlockControlFlow(StatementSyntax statement)
            => statement is IfStatementSyntax ||
                statement is ForStatementSyntax ||
                statement is ForEachStatementSyntax ||
                statement is WhileStatementSyntax ||
                statement is DoStatementSyntax ||
                statement is SwitchStatementSyntax;

        static SyntaxTree[] BuildTrees(IEnumerable<ArchitectureSourceFile> files)
            => [.. files.Select(ParseFile)];

        /// <summary>
        /// Gets the names of every instance field, property, event, and method declared by a type.
        /// </summary>
        /// <param name="type">The declaring type.</param>
        public static IReadOnlyList<string> GetInstanceMemberNames(TypeDeclarationSyntax type)
        {
            List<string> names = [];

            foreach (MemberDeclarationSyntax member in type.Members)
            {
                bool isStatic = member.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));

                if (isStatic)
                {
                    continue;
                }

                if (member is FieldDeclarationSyntax field)
                {
                    names.AddRange(field.Declaration.Variables.Select(variable => variable.Identifier.Text));
                }
                else if (member is PropertyDeclarationSyntax property)
                {
                    names.Add(property.Identifier.Text);
                }
                else if (member is MethodDeclarationSyntax method)
                {
                    names.Add(method.Identifier.Text);
                }
                else if (member is EventFieldDeclarationSyntax eventField)
                {
                    names.AddRange(eventField.Declaration.Variables.Select(variable => variable.Identifier.Text));
                }
            }

            return names;
        }
    }
}