using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModelFiltersGenerator
{
    internal static class CodeAnalyzer
    {
        internal static bool IsClassNameToken(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.IdentifierToken) && token.Parent.IsKind(SyntaxKind.ClassDeclaration);
        }

        internal static IEnumerable<PropertyInfo> GetPropertiesInfo(SyntaxNode classNode, SemanticModel semanticModel)
        {
            var properties = classNode
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => !prop.ContainsDiagnostics
                               && !prop.Modifiers.Any(SyntaxKind.StaticKeyword)
                               && !prop.Modifiers.Any(SyntaxKind.AbstractKeyword))
                .Select(prop => new PropertyInfo
                {
                    PropertyDeclaration = prop,
                    Type = semanticModel.GetDeclaredSymbol(prop).Type
                });

            return properties;
        }
    }
}
