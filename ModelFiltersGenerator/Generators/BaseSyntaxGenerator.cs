using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ModelFiltersGenerator.Generators
{
    public static class BaseSyntaxGenerator
    {
        internal static NameSyntax QualifiedName(IReadOnlyList<string> segments)
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

            return SyntaxFactory.QualifiedName(QualifiedName(otherSegments), IdentifierName(lastSegment));
        }

        internal static NameSyntax QualifiedName(string name)
        {
            return QualifiedName(name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static ParameterSyntax Parameter(TypeSyntax type, string name)
        {
            return SyntaxFactory.Parameter(Identifier(name)).WithType(type);
        }

        internal static ParameterSyntax Parameter(string type, string name)
        {
            var typeSyntax = ParseTypeName(type);
            return Parameter(typeSyntax, name);
        }

        internal static GenericNameSyntax GenericType(string typeName, params string[] typeArguments)
        {
            var arguments = TypeArgumentList(
                SeparatedList<TypeSyntax>(typeArguments.Select(IdentifierName)));

            return GenericName(typeName).WithTypeArgumentList(arguments);
        }

        internal static ExpressionSyntax SimpleMemberAccess(IReadOnlyList<string> segments)
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


            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SimpleMemberAccess(otherSegments),
                IdentifierName(lastSegment));
        }

        internal static ExpressionSyntax SimpleMemberAccess(string expression)
        {
            return QualifiedName(expression.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static ArgumentListSyntax SeparatedArgumentList(params ExpressionSyntax[] arguments)
        {
            if (arguments.Length == 0)
            {
                return ArgumentList(SeparatedList<ArgumentSyntax>());
            }

            if (arguments.Length == 1)
            {
                return ArgumentList(SingletonSeparatedList(Argument(arguments[0])));
            }

            return ArgumentList(SeparatedList(arguments.Select(Argument)));
        }

        internal static CompilationUnitSyntax CompilationUnit(string namespaceName, params MemberDeclarationSyntax[] members)
        {
            var usings = List(new[] { "System", "System.Linq" }.Select(u => UsingDirective(QualifiedName(u))));
            var @namespace = NamespaceDeclaration(QualifiedName(namespaceName)).WithMembers(List(members));

            return SyntaxFactory.CompilationUnit()
                .WithUsings(usings)
                .WithMembers(SingletonList<MemberDeclarationSyntax>(@namespace));
        }
    }
}
