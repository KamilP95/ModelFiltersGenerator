using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModelFiltersGenerator
{
    internal class PropertyInfo
    {
        public PropertyDeclarationSyntax PropertyDeclaration { get; set; }

        public ITypeSymbol Type { get; set; }

        public string PropertyName => PropertyDeclaration.Identifier.Text;
    }
}
