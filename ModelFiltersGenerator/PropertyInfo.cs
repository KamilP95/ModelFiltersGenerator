using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModelFiltersGenerator
{
    internal class PropertyInfo
    {
        public string Name { get; set; }

        public ITypeSymbol TypeInfo { get; set; }

        public TypeSyntax TypeSyntax { get; set; }

        public bool RangeFilter { get; set; }
    }
}
