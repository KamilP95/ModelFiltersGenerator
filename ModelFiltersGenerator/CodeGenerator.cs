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

        internal static ClassDeclarationSyntax CreateFilterClass(string className, IEnumerable<PropertyInfo> properties)
        {
            var filterProperties = new SyntaxList<MemberDeclarationSyntax>(properties.SelectMany(CreateFilterProperties));

            var filterClass = ClassDeclaration(className)
                .WithMembers(filterProperties)
                .WithModifiers(TokenList(PublicKeywordToken));

            return filterClass;
        }

        internal static IEnumerable<PropertyDeclarationSyntax> CreateFilterProperties(PropertyInfo property)
        {
            var propertyType = property.TypeInfo.IsValueType
                ? NullableType(property.TypeSyntax)
                : property.TypeSyntax;

            if (property.RangeFilter)
            {
                return new[]
                {
                    CreateAutoProperty(property.Name + "From", propertyType),
                    CreateAutoProperty(property.Name + "To", propertyType)
                };
            }

            return new[] { CreateAutoProperty(property.Name, propertyType) };
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

        internal static CompilationUnitSyntax CreateRoot(string namespaceName, params MemberDeclarationSyntax[] members)
        {
            var usings = CreateUsings("System", "System.Linq", "System.Collections.Generic");
            var @namespace = CreateNamespace(namespaceName, members);

            var root = CompilationUnit()
                .WithUsings(usings)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(@namespace));

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

        internal static NamespaceDeclarationSyntax CreateNamespace(string name, params MemberDeclarationSyntax[] members)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }

            return NamespaceDeclaration(GetQualifiedName(name)).WithMembers(List(members));
        }

        internal static NameSyntax GetQualifiedName(IReadOnlyList<string> segments)
        {
            if (segments.Count == 0)
            {
                throw new ArgumentException("Segments must have at least one element.", nameof(segments));
            }

            if (segments.Count == 1)
            {
                return IdentifierName(segments[0]);
            }

            var lastSegment = segments.Last();
            var otherSegments = segments.Take(segments.Count - 1).ToArray();

            return QualifiedName(GetQualifiedName(otherSegments), IdentifierName(lastSegment));
        }

        internal static NameSyntax GetQualifiedName(string name)
        {
            return GetQualifiedName(name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
