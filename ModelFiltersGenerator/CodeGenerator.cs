using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ModelFiltersGenerator
{
    internal static class CodeGenerator
    {
        internal static SyntaxToken PublicKeywordToken => Token(SyntaxKind.PublicKeyword);
        internal static SyntaxToken SemicolonToken => Token(SyntaxKind.SemicolonToken);

        internal static Document GenerateFilters(
            Document document,
            CompilationUnitSyntax root,
            string className,
            IEnumerable<PropertyInfo> properties)
        {
            var namespaceNode = root.ChildNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            if (namespaceNode == null)
            {
                return document;
            }

            var filterProperties = new SyntaxList<MemberDeclarationSyntax>(
                properties.Select(p => CreateAutoProperty(p.PropertyName, p.PropertyDeclaration.Type)));

            var filterClass = ClassDeclaration(className + "Filter")
                .WithMembers(filterProperties)
                .WithModifiers(TokenList(PublicKeywordToken));

            root = root.ReplaceNode(namespaceNode, namespaceNode.AddMembers(filterClass));
            return document.WithSyntaxRoot(root);
        }

        internal static PropertyDeclarationSyntax CreateAutoProperty(string name, TypeSyntax type)
        {
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);
            var setter = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);

            return PropertyDeclaration(type, name)
                .WithModifiers(TokenList(PublicKeywordToken))
                .WithAccessorList(AccessorList(List(new[] { getter, setter })))
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));
        }

        internal static CompilationUnitSyntax CreateRoot()
        {
            var root = CompilationUnit()
                .WithUsings(CreateUsings("System", "System.Linq", "System.Collections.Generic"))
                .WithMembers(List<MemberDeclarationSyntax>(new[]
                    {NamespaceDeclaration(IdentifierName("GeneratedNamespace"))}));

            return root;
        }

        internal static SyntaxList<UsingDirectiveSyntax> CreateUsings(params string[] usings)
        {
            var usingsList = new List<UsingDirectiveSyntax>();

            foreach (var @using in usings)
            {
                var segments = @using.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 0)
                {
                    continue;
                }

                var usingName = GetQualifiedName(segments);
                usingsList.Add(UsingDirective(usingName));
            }

            return List(usingsList);
        }

        private static NameSyntax GetQualifiedName(IReadOnlyList<string> segments)
        {
            if (segments.Count == 0)
            {
                return default(NameSyntax);
            }

            if (segments.Count == 1)
            {
                return IdentifierName(segments[0]);
            }

            var lastSegment = segments.Last();
            var otherSegments = segments.Take(segments.Count - 1).ToArray();

            return QualifiedName(GetQualifiedName(otherSegments), IdentifierName(lastSegment));
        }
    }
}
