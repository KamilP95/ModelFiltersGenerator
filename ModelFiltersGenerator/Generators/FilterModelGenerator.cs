using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelFiltersGenerator.Analyzers;
using ModelFiltersGenerator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ModelFiltersGenerator.Generators
{
    internal static class FilterModelGenerator
    {
        internal static ClassDeclarationSyntax FilterModelClass(string modelClassName, IEnumerable<PropertyInfo> modelProperties)
        {
            var className = modelClassName + "Filters";
            var filterProperties = FilterProperties(modelProperties);

            return ClassDeclaration(className)
                .WithModifiers(TokenList(Tokens.PublicKeyword))
                .WithMembers(List<MemberDeclarationSyntax>(filterProperties));
        }

        internal static IEnumerable<PropertyDeclarationSyntax> FilterProperties(IEnumerable<PropertyInfo> modelProperties)
        {
            var filterProperties = new List<PropertyDeclarationSyntax>();

            foreach (var property in modelProperties)
            {
                var propertyType = property.TypeInfo.IsString()
                                    ? property.TypeSyntax
                                    : NullableType(property.TypeSyntax);

                if (property.RangeFilter)
                {

                    filterProperties.Add(AutoProperty(property.Name + "From", propertyType));
                    filterProperties.Add(AutoProperty(property.Name + "To", propertyType));

                    continue;
                }

                filterProperties.Add(AutoProperty(property.Name, propertyType));
            }

            return filterProperties;
        }

        internal static PropertyDeclarationSyntax AutoProperty(string name, TypeSyntax type)
        {
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(Tokens.Semicolon);
            var setter = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(Tokens.Semicolon);

            return PropertyDeclaration(type, name)
                .WithModifiers(TokenList(Tokens.PublicKeyword))
                .WithAccessorList(AccessorList(List(new[] { getter, setter })))
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));
        }
    }
}
