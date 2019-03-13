using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ModelFiltersGenerator.Generators
{
    public static class LambdaGenerator
    {
        internal static SimpleLambdaExpressionSyntax LambdaExpression(string parameterName, CSharpSyntaxNode body)
        {
            return SimpleLambdaExpression(Parameter(Identifier(parameterName)), body);
        }

        internal static SimpleLambdaExpressionSyntax EqualsPredicate(
            string lambdaParameter,
            string modelProperty,
            string filterProperty)
        {
            var body = BinaryExpression(
                SyntaxKind.EqualsExpression,
                BaseSyntaxGenerator.SimpleMemberAccess(new[] { lambdaParameter, modelProperty }),
                BaseSyntaxGenerator.QualifiedName(filterProperty));

            return LambdaExpression(lambdaParameter, body);
        }

        internal static SimpleLambdaExpressionSyntax GreaterOrEqualPredicate(
            string lambdaParameter,
            string modelProperty,
            string filterProperty)
        {
            var body = BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                BaseSyntaxGenerator.SimpleMemberAccess(new[] { lambdaParameter, modelProperty }),
                BaseSyntaxGenerator.QualifiedName(filterProperty));

            return LambdaExpression(lambdaParameter, body);
        }

        internal static SimpleLambdaExpressionSyntax LessOrEqualPredicate(
            string lambdaParameter,
            string modelProperty,
            string filterProperty)
        {
            var body = BinaryExpression(
                SyntaxKind.LessThanOrEqualExpression,
                BaseSyntaxGenerator.SimpleMemberAccess(new[] { lambdaParameter, modelProperty }),
                BaseSyntaxGenerator.QualifiedName(filterProperty));

            return LambdaExpression(lambdaParameter, body);
        }

        internal static SimpleLambdaExpressionSyntax ContainsPredicate(
            string lambdaParameter,
            string modelProperty,
            string filterProperty)
        {
            var body = InvocationExpression(BaseSyntaxGenerator.SimpleMemberAccess($"{lambdaParameter}.{modelProperty}.{nameof(Queryable.Contains)}"))
                .WithArgumentList(BaseSyntaxGenerator.SeparatedArgumentList(IdentifierName(filterProperty)));

            return LambdaExpression(lambdaParameter, body);
        }
    }
}
