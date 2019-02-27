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
    }
}
