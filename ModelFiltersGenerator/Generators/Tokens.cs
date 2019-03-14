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
        internal static SyntaxToken Dot => SyntaxFactory.Token(SyntaxKind.DotToken);
        internal static SyntaxToken QuestionMark => SyntaxFactory.Token(SyntaxKind.QuestionToken);
        internal static SyntaxToken Colon => SyntaxFactory.Token(SyntaxKind.ColonToken);
        internal static SyntaxToken CloseParenthesis => SyntaxFactory.Token(SyntaxKind.CloseParenToken);
    }
}