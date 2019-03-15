using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelFiltersGenerator.Models;

namespace ModelFiltersGenerator.Analyzers
{
    internal static class CodeAnalyzer
    {
        internal static IEnumerable<PropertyInfo> GetPropertiesInfo(SyntaxNode classNode, SemanticModel semanticModel)
        {
            var properties = classNode
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !prop.ContainsDiagnostics
                               && !prop.Modifiers.Any(SyntaxKind.StaticKeyword)
                               && !prop.Modifiers.Any(SyntaxKind.AbstractKeyword))
                .Select(prop => new PropertyInfo
                (
                    name: prop.Identifier.Text,
                    typeSyntax: prop.Type,
                    typeInfo: semanticModel.GetDeclaredSymbol(prop).Type
                ))
                .Where(p => p.TypeInfo.IsSupported());

            return properties;
        }

        internal static bool IsClassNameToken(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.IdentifierToken) && token.Parent.IsKind(SyntaxKind.ClassDeclaration);
        }

        internal static string GetNamespaceName(this CompilationUnitSyntax root)
        {
            return root
                .DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name
                .ToString() ?? string.Empty;
        }
    }
}
