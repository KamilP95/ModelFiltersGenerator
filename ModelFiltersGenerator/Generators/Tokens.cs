using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ModelFiltersGenerator
{
    internal static class Tokens
    {
        internal static SyntaxToken PublicKeyword => SyntaxFactory.Token(SyntaxKind.PublicKeyword);
        internal static SyntaxToken StaticKeyword => SyntaxFactory.Token(SyntaxKind.StaticKeyword);
        internal static SyntaxToken ThisKeyword => SyntaxFactory.Token(SyntaxKind.ThisKeyword);
        internal static SyntaxToken Semicolon => SyntaxFactory.Token(SyntaxKind.SemicolonToken);
    }
}