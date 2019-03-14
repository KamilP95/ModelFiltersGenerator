using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ModelFiltersGenerator.Generators
{
    internal static class Trivia
    {
        internal static SyntaxTrivia EndOfLine => SyntaxFactory.EndOfLine(Environment.NewLine);

        internal static SyntaxTrivia[] Indentation(int level = 1)
        {
            return Enumerable.Range(0, level).Select(_ => SyntaxFactory.Tab).ToArray();
        }
    }
}
