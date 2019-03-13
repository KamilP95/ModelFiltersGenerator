using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelFiltersGenerator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ModelFiltersGenerator.Generators
{
    internal static class FilterExtensionsGenerator
    {
        internal static ClassDeclarationSyntax FilterExtensionsClass(string modelClassName, IEnumerable<PropertyInfo> modelProperties)
        {
            var className = modelClassName + "FilterExtensions";

            var filterMethods = FilterExtensionMethods(modelProperties, modelClassName);

            return ClassDeclaration(className)
                .WithModifiers(TokenList(Tokens.PublicKeyword, Tokens.StaticKeyword))
                .WithMembers(List(filterMethods));
        }

        internal static IEnumerable<MemberDeclarationSyntax> FilterExtensionMethods(IEnumerable<PropertyInfo> modelProperties, string modelClassName)
        {
            var collectionType = BaseSyntaxGenerator.GenericType(nameof(IQueryable), modelClassName);
            var collectionName = modelClassName.ToCamelCase().Pluralize();
            var collectionParameter = BaseSyntaxGenerator.Parameter(collectionType, collectionName);

            var filterMethods = new List<MemberDeclarationSyntax>
            {
                SummaryFilterExtensionMethod(collectionParameter, modelProperties, modelClassName + "Filters")
            };


            foreach (var modelProperty in modelProperties)
            {
                if (modelProperty.RangeFilter)
                {
                    filterMethods.Add(RangeFromFilterExtensionMethod(collectionParameter, modelProperty));
                    filterMethods.Add(RangeToFilterExtensionMethod(collectionParameter, modelProperty));
                    continue;
                }

                if (modelProperty.TypeInfo.IsString())
                {
                    filterMethods.Add(StringContainsFilterExtensionMethod(collectionParameter, modelProperty));
                }

                filterMethods.Add(EqualsFilterExtensionMethod(collectionParameter, modelProperty));
            }

            return filterMethods;
        }

        internal static MethodDeclarationSyntax ExtensionMethod(
            string methodName,
            TypeSyntax returnType,
            ParameterSyntax thisParameter,
            IEnumerable<ParameterSyntax> otherParameters,
            BlockSyntax body)
        {
            var parameters = new List<ParameterSyntax>(otherParameters);
            parameters.Insert(0, thisParameter.WithModifiers(TokenList(Tokens.ThisKeyword)));

            return MethodDeclaration(returnType, methodName)
                .WithParameterList(ParameterList(SeparatedList(parameters)))
                .WithModifiers(TokenList(Tokens.PublicKeyword, Tokens.StaticKeyword))
                .WithBody(body)
                .WithTrailingTrivia(Tab)
                .WithLeadingTrivia(EndOfLine("\r\n"));
        }

        internal static MethodDeclarationSyntax FilterExtensionMethod(
            string propertyName,
            ParameterSyntax collectionParameter,
            ParameterSyntax filterParameter,
            BlockSyntax body)
        {
            return ExtensionMethod(
                $"FilterBy{propertyName}",
                collectionParameter.Type,
                collectionParameter,
                new[] { filterParameter },
                body);
        }

        internal static BlockSyntax FilterExtensionMethodBody(
            ExpressionSyntax condition,
            ExpressionSyntax filterExpression,
            ExpressionSyntax collectionExpression)
        {
            return Block(
                ReturnStatement(
                    ConditionalExpression(
                        condition.WithLeadingTrivia(EndOfLine("\r\n")),
                        filterExpression.WithTrailingTrivia(Tab).WithLeadingTrivia(EndOfLine("\r\n")),
                        collectionExpression.WithTrailingTrivia(Tab).WithLeadingTrivia(EndOfLine("\r\n"))))
            );
        }

        internal static MethodDeclarationSyntax RangeFromFilterExtensionMethod(
            ParameterSyntax collectionParameter,
            PropertyInfo modelProperty)
        {
            var filterParameterName = modelProperty.Name.ToCamelCase() + "From";
            var filterFromParameter = BaseSyntaxGenerator.Parameter(NullableType(modelProperty.TypeSyntax), filterParameterName);
            var collectionName = collectionParameter.Identifier.Text;

            var condition = NullableHasValueCheckExpression(filterParameterName);
            var filterExpression = LinqWhereExpression(
                collectionName,
                LambdaGenerator.GreaterOrEqualPredicate(collectionName.Substring(0, 1), modelProperty.Name, filterParameterName));

            var methodBody = FilterExtensionMethodBody(condition, filterExpression, IdentifierName(collectionName));

            return FilterExtensionMethod(
                modelProperty.Name + "From",
                collectionParameter,
                filterFromParameter,
                methodBody);
        }

        internal static MethodDeclarationSyntax RangeToFilterExtensionMethod(
            ParameterSyntax collectionParameter,
            PropertyInfo modelProperty)
        {
            var filterParameterName = modelProperty.Name.ToCamelCase() + "To";
            var filterToParameter = BaseSyntaxGenerator.Parameter(NullableType(modelProperty.TypeSyntax), filterParameterName);
            var collectionName = collectionParameter.Identifier.Text;

            var condition = NullableHasValueCheckExpression(filterParameterName);
            var filterExpression = LinqWhereExpression(
                collectionName,
                LambdaGenerator.LessOrEqualPredicate(collectionName.Substring(0, 1), modelProperty.Name, filterParameterName));

            var methodBody = FilterExtensionMethodBody(condition, filterExpression, IdentifierName(collectionName));

            return FilterExtensionMethod(
                modelProperty.Name + "To",
                collectionParameter,
                filterToParameter,
                methodBody);
        }

        internal static MethodDeclarationSyntax StringContainsFilterExtensionMethod(
            ParameterSyntax collectionParameter,
            PropertyInfo modelProperty)
        {
            var filterParameterName = modelProperty.Name.ToCamelCase();
            var filterParameter = BaseSyntaxGenerator.Parameter(modelProperty.TypeSyntax, filterParameterName);
            var collectionName = collectionParameter.Identifier.Text;

            var condition = StringNotEmptyCheckExpression(filterParameterName);
            var filterExpression = LinqWhereExpression(
                collectionName,
                LambdaGenerator.ContainsPredicate(collectionName.Substring(0, 1), modelProperty.Name, filterParameterName));

            var methodBody = FilterExtensionMethodBody(condition, filterExpression, IdentifierName(collectionName));

            return FilterExtensionMethod(
                modelProperty.Name,
                collectionParameter,
                filterParameter,
                methodBody);
        }

        internal static MethodDeclarationSyntax EqualsFilterExtensionMethod(
            ParameterSyntax collectionParameter,
            PropertyInfo modelProperty)
        {
            var filterParameterName = modelProperty.Name.ToCamelCase();
            var filterParameter = BaseSyntaxGenerator.Parameter(modelProperty.TypeSyntax, filterParameterName);
            var collectionName = collectionParameter.Identifier.Text;

            var condition = NullableHasValueCheckExpression(filterParameterName);
            var filterExpression = LinqWhereExpression(
                collectionName,
                LambdaGenerator.EqualsPredicate(collectionName.Substring(0, 1), modelProperty.Name, filterParameterName));

            var methodBody = FilterExtensionMethodBody(condition, filterExpression, IdentifierName(collectionName));

            return FilterExtensionMethod(
                modelProperty.Name,
                collectionParameter,
                filterParameter,
                methodBody);
        }

        internal static MethodDeclarationSyntax SummaryFilterExtensionMethod(
            ParameterSyntax collectionParameter,
            IEnumerable<PropertyInfo> modelProperties,
            string filterModelClass)
        {
            var collectionName = collectionParameter.Identifier.Text;
            var filterPropertiesNames = modelProperties
                .SelectMany(p =>
                    p.RangeFilter
                        ? new[] { p.Name + "From", p.Name = "To" }
                        : new[] { p.Name })
                .ToList();

            var body = Block(SingletonList(ReturnStatement(FilterChainInvocation(filterPropertiesNames))));

            return ExtensionMethod(
                "FilterBy",
                collectionParameter.Type,
                collectionParameter,
                new[] { BaseSyntaxGenerator.Parameter(filterModelClass, "filters") },
                body);

            InvocationExpressionSyntax FilterChainInvocation(List<string> filterProperties)
            {
                string propertyName;

                if (filterProperties.Count == 1)
                {
                    propertyName = filterProperties.First();
                    return InvocationExpression(
                        BaseSyntaxGenerator.SimpleMemberAccess($"{collectionName}.FilterBy{propertyName}"),
                        BaseSyntaxGenerator.SeparatedArgumentList(BaseSyntaxGenerator.SimpleMemberAccess($"filters.{propertyName}")));
                }

                propertyName = filterProperties.Last();
                return InvocationExpression(
                    FilterChainInvocation(filterProperties.GetRange(0, filterProperties.Count - 1)),
                    BaseSyntaxGenerator.SeparatedArgumentList(BaseSyntaxGenerator.SimpleMemberAccess($"filters.{propertyName}")));
            }
        }

        internal static ExpressionSyntax StringNotEmptyCheckExpression(string parameterName)
        {
            return PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                InvocationExpression(BaseSyntaxGenerator.SimpleMemberAccess("string.IsNullOrEmpty"))
                    .WithArgumentList(BaseSyntaxGenerator.SeparatedArgumentList(IdentifierName(parameterName))));
        }

        internal static ExpressionSyntax NullableHasValueCheckExpression(string parameterName)
        {
            return BaseSyntaxGenerator.SimpleMemberAccess(new[] { parameterName, nameof(Nullable<int>.HasValue) });
        }

        internal static ExpressionSyntax LinqWhereExpression(string collectionName, SimpleLambdaExpressionSyntax predicate)
        {
            return InvocationExpression(BaseSyntaxGenerator.SimpleMemberAccess(new[] { collectionName, "Where" }))
                .WithArgumentList(BaseSyntaxGenerator.SeparatedArgumentList(predicate));
        }
    }
}
