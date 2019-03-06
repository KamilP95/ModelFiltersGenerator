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
        internal static SyntaxToken StaticKeywordToken => Token(SyntaxKind.StaticKeyword);
        internal static SyntaxToken ThisKeywordToken => Token(SyntaxKind.ThisKeyword);
        internal static SyntaxToken SemicolonToken => Token(SyntaxKind.SemicolonToken);

        internal static ClassDeclarationSyntax CreateFilterModelClass(string className, IEnumerable<PropertyInfo> properties)
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

                var usingName = CreateQualifiedName(segments);
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

            return NamespaceDeclaration(CreateQualifiedName(name)).WithMembers(List(members));
        }

        internal static NameSyntax CreateQualifiedName(IReadOnlyList<string> segments)
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

            return QualifiedName(CreateQualifiedName(otherSegments), IdentifierName(lastSegment));
        }

        internal static NameSyntax CreateQualifiedName(string name)
        {
            return CreateQualifiedName(name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static ClassDeclarationSyntax CreateFilterExtensionsClass(
            string modelClassName,
            IEnumerable<PropertyInfo> modelProperties)
        {
            var className = modelClassName + "FilterExtensions";

            var filterMethods = modelProperties
                .SelectMany(p => CreateFilterExtensionMethod(modelClassName, p))
                .Select(m => m.WithModifiers(TokenList(PublicKeywordToken, StaticKeywordToken)));

            var filterClass = ClassDeclaration(className)
                .WithModifiers(TokenList(PublicKeywordToken, StaticKeywordToken))
                .WithMembers(List<MemberDeclarationSyntax>(filterMethods));

            return filterClass;
        }

        internal static IEnumerable<MethodDeclarationSyntax> CreateFilterExtensionMethod(string modelClassName, PropertyInfo modelProperty)
        {
            var collectionType = CreateGenericType(nameof(IQueryable), modelClassName);
            var collectionParameter = Parameter(ParseToken(modelClassName.ToCamelCase()))
                .WithType(collectionType)
                .WithModifiers(TokenList(ThisKeywordToken));

            if (modelProperty.RangeFilter)
            {
                return CreateRangeFilterExtensionMethods(modelProperty, collectionType, collectionParameter);
            }

            var methodName = "FilterBy" + modelProperty.Name;
            var filterParameter = Parameter(ParseToken(modelProperty.Name.ToCamelCase()))
                .WithType(modelProperty.TypeInfo.SpecialType == CodeAnalyzer.SupprotedTypes.String
                    ? modelProperty.TypeSyntax
                    : NullableType(modelProperty.TypeSyntax));
            var parameters = ParameterList(SeparatedList(new[] { collectionParameter, filterParameter }));

            var methodBody = CreateMethodBody(modelProperty, collectionParameter.Identifier.Text, filterParameter.Identifier.Text);

            var method = MethodDeclaration(collectionType, methodName)
                .WithParameterList(parameters)
                .WithBody(methodBody)
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));

            return new[] { method };
        }

        private static BlockSyntax CreateMethodBody(PropertyInfo modelProperty, string collectionName, string filterName)
        {
            if (modelProperty.TypeInfo.IsString())
            {
                return CreateStringContainsFilterExtensionMethodBody(collectionName, modelProperty.Name, filterName);
            }

            if (modelProperty.TypeInfo.IsNumericType() || modelProperty.TypeInfo.IsBool())
            {
                return CreateEqualsFilterExtensionMethodBody(collectionName, modelProperty.Name, filterName);
            }

            throw new ArgumentException("Unsupprted property type");
        }

        private static IEnumerable<MethodDeclarationSyntax> CreateRangeFilterExtensionMethods(PropertyInfo modelProperty, TypeSyntax collectionType, ParameterSyntax collectionParameter)
        {
            var fromMethod = CreateFromRangeFilterExtensionMethod(modelProperty, collectionType, collectionParameter);
            var toMethod = CreateToRangeFilterExtensionMethod(modelProperty, collectionType, collectionParameter);

            return new[] { fromMethod, toMethod };
        }

        private static MethodDeclarationSyntax CreateFromRangeFilterExtensionMethod(PropertyInfo modelProperty, TypeSyntax collectionType, ParameterSyntax collectionParameter)
        {
            var methodName = "FilterBy" + modelProperty.Name + "From";
            var filterParameter = Parameter(ParseToken(modelProperty.Name.ToCamelCase() + "From"))
                .WithType(modelProperty.TypeInfo.SpecialType == CodeAnalyzer.SupprotedTypes.String
                    ? modelProperty.TypeSyntax
                    : NullableType(modelProperty.TypeSyntax));
            var parameters = ParameterList(SeparatedList(new[] { collectionParameter, filterParameter }));

            var methodBody = CreateGreaterOrEqualFilterExtensionMethodBody(collectionParameter.Identifier.Text, modelProperty.Name, filterParameter.Identifier.Text);

            var method = MethodDeclaration(collectionType, methodName)
                .WithParameterList(parameters)
                .WithBody(methodBody)
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));

            return method;
        }

        private static MethodDeclarationSyntax CreateToRangeFilterExtensionMethod(PropertyInfo modelProperty, TypeSyntax collectionType, ParameterSyntax collectionParameter)
        {
            var methodName = "FilterBy" + modelProperty.Name + "To";
            var filterParameter = Parameter(ParseToken(modelProperty.Name.ToCamelCase() + "To"))
                .WithType(modelProperty.TypeInfo.SpecialType == CodeAnalyzer.SupprotedTypes.String
                    ? modelProperty.TypeSyntax
                    : NullableType(modelProperty.TypeSyntax));
            var parameters = ParameterList(SeparatedList(new[] { collectionParameter, filterParameter }));

            var methodBody = CreateLessOrEqualFilterExtensionMethodBody(collectionParameter.Identifier.Text, modelProperty.Name, filterParameter.Identifier.Text);

            var method = MethodDeclaration(collectionType, methodName)
                .WithParameterList(parameters)
                .WithBody(methodBody)
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));

            return method;
        }

        internal static GenericNameSyntax CreateGenericType(string typeName, params string[] typeArguments)
        {
            var arguments = TypeArgumentList(
                SeparatedList<TypeSyntax>(typeArguments.Select(IdentifierName)));

            return GenericName(typeName).WithTypeArgumentList(arguments);
        }

        internal static BlockSyntax CreateStringContainsFilterExtensionMethodBody(string collectionName, string modelPropertyName, string filterName)
        {
            var condition = CreateStringIsNotEmptyCheckExpression(filterName);
            var collectionExpression = IdentifierName(collectionName);
            var filterLambda = CreateLambdaWithContainsPredicate(collectionName[0].ToString(), modelPropertyName, filterName);
            var filterExpression = CreateLinqWhereExpression(collectionName, filterLambda);

            return Block(CreateFilterExtensionMethodBody(condition, filterExpression, collectionExpression));
        }

        internal static BlockSyntax CreateEqualsFilterExtensionMethodBody(string collectionName, string modelPropertyName, string filterName)
        {
            var condition = CreateNullableHasValueCheckExpression(filterName);
            var collectionExpression = IdentifierName(collectionName);
            var filterLambda = CreateLambdaWithEqualsPredicate(collectionName[0].ToString(), modelPropertyName, filterName);
            var filterExpression = CreateLinqWhereExpression(collectionName, filterLambda);

            return Block(CreateFilterExtensionMethodBody(condition, filterExpression, collectionExpression));
        }

        internal static BlockSyntax CreateGreaterOrEqualFilterExtensionMethodBody(string collectionName, string modelPropertyName, string filterName)
        {
            var condition = CreateNullableHasValueCheckExpression(filterName);
            var collectionExpression = IdentifierName(collectionName);
            var filterLambda = CreateLambdaWithGreaterOrEqualPredicate(collectionName[0].ToString(), modelPropertyName, filterName);
            var filterExpression = CreateLinqWhereExpression(collectionName, filterLambda);

            return Block(CreateFilterExtensionMethodBody(condition, filterExpression, collectionExpression));
        }

        internal static BlockSyntax CreateLessOrEqualFilterExtensionMethodBody(string collectionName, string modelPropertyName, string filterName)
        {
            var condition = CreateNullableHasValueCheckExpression(filterName);
            var collectionExpression = IdentifierName(collectionName);
            var filterLambda = CreateLambdaWithLessOrEqualPredicate(collectionName[0].ToString(), modelPropertyName, filterName);
            var filterExpression = CreateLinqWhereExpression(collectionName, filterLambda);

            return Block(CreateFilterExtensionMethodBody(condition, filterExpression, collectionExpression));
        }

        internal static StatementSyntax CreateFilterExtensionMethodBody(
            ExpressionSyntax condition,
            ExpressionSyntax filterExpression,
            ExpressionSyntax collectionExpression)
        {
            return ReturnStatement(
                ConditionalExpression(
                    condition.WithTrailingTrivia(Tab).WithLeadingTrivia(EndOfLine("\r\n")),
                    filterExpression.WithTrailingTrivia(Tab).WithLeadingTrivia(EndOfLine("\r\n")),
                    collectionExpression.WithTrailingTrivia(Tab).WithLeadingTrivia(EndOfLine("\r\n"))));
        }

        internal static ExpressionSyntax CreateStringIsNotEmptyCheckExpression(string parameterName)
        {
            return PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        PredefinedType(Token(SyntaxKind.StringKeyword)),
                        IdentifierName(nameof(string.IsNullOrEmpty))))
                .WithArgumentList(
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(parameterName))))));
        }

        internal static ExpressionSyntax CreateNullableHasValueCheckExpression(string parameterName)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(parameterName),
                    IdentifierName("HasValue")));
        }

        internal static SimpleLambdaExpressionSyntax CreateLambdaWithContainsPredicate(
            string lambdaParameterName,
            string modelPropertyName,
            string filterName)
        {
            return SimpleLambdaExpression(
                    Parameter(
                        Identifier(lambdaParameterName)),
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(lambdaParameterName),
                                    IdentifierName(modelPropertyName)),
                                IdentifierName(nameof(Queryable.Contains))))
                        .WithArgumentList(
                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(filterName))))));
        }

        internal static SimpleLambdaExpressionSyntax CreateLambdaWithEqualsPredicate(
            string lambdaParameterName,
            string modelPropertyName,
            string filterName)
        {
            return SimpleLambdaExpression(
                Parameter(
                    Identifier(lambdaParameterName)),
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(lambdaParameterName),
                        IdentifierName(modelPropertyName)),
                    IdentifierName(filterName)));
        }

        internal static SimpleLambdaExpressionSyntax CreateLambdaWithGreaterOrEqualPredicate(
            string lambdaParameterName,
            string modelPropertyName,
            string filterName)
        {
            return SimpleLambdaExpression(
                Parameter(
                    Identifier(lambdaParameterName)),
                BinaryExpression(
                    SyntaxKind.GreaterThanOrEqualExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(lambdaParameterName),
                        IdentifierName(modelPropertyName)),
                    IdentifierName(filterName)));
        }

        internal static SimpleLambdaExpressionSyntax CreateLambdaWithLessOrEqualPredicate(
            string lambdaParameterName,
            string modelPropertyName,
            string filterName)
        {
            return SimpleLambdaExpression(
                Parameter(
                    Identifier(lambdaParameterName)),
                BinaryExpression(
                    SyntaxKind.LessThanOrEqualExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(lambdaParameterName),
                        IdentifierName(modelPropertyName)),
                    IdentifierName(filterName)));
        }

        internal static ExpressionSyntax CreateLinqWhereExpression(
            string collectionName,
            SimpleLambdaExpressionSyntax predicateExpression)
        {
            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(collectionName),
                        IdentifierName(nameof(Queryable.Where))))
                    .WithArgumentList(
                        ArgumentList(SingletonSeparatedList(Argument(predicateExpression))));
        }
    }
}
