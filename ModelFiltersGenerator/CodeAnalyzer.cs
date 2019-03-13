using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelFiltersGenerator.Models;

namespace ModelFiltersGenerator
{
    internal static class CodeAnalyzer
    {
        internal static class SupprotedTypes
        {
            internal static SpecialType Boolean => SpecialType.System_Boolean;
            internal static SpecialType Byte => SpecialType.System_Byte;
            internal static SpecialType DateTime => SpecialType.System_DateTime;
            internal static SpecialType Decimal => SpecialType.System_Decimal;
            internal static SpecialType Double => SpecialType.System_Double;
            internal static SpecialType Int16 => SpecialType.System_Int16;
            internal static SpecialType Int32 => SpecialType.System_Int32;
            internal static SpecialType Int64 => SpecialType.System_Int64;
            internal static SpecialType String => SpecialType.System_String;

            internal static IEnumerable<SpecialType> All => new[]
                {Boolean, Byte, DateTime, Decimal, Double, Int16, Int32, Int64, String};
        };

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
                    Name = prop.Identifier.Text,
                    TypeSyntax = prop.Type,
                    TypeInfo = semanticModel?.GetDeclaredSymbol(prop).Type,
                    RangeFilter = semanticModel?.GetDeclaredSymbol(prop).Type.SpecialType == SupprotedTypes.DateTime
                })
                .Where(p => SupprotedTypes.All.Contains(p.TypeInfo.SpecialType));

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

        internal static bool IsNumericType(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Byte
                   || type.SpecialType == SupprotedTypes.Double
                   || type.SpecialType == SupprotedTypes.Decimal
                   || type.SpecialType == SupprotedTypes.Int16
                   || type.SpecialType == SupprotedTypes.Int32
                   || type.SpecialType == SupprotedTypes.Int64;
        }

        internal static bool IsString(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.String;
        }

        internal static bool IsBool(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Boolean;
        }
    }
}
