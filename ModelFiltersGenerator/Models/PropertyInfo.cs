using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModelFiltersGenerator.Models
{
    internal class PropertyInfo
    {
        public string Name { get; }

        public ITypeSymbol TypeInfo { get; }

        public TypeSyntax TypeSyntax { get; }

        public bool RangeFilter { get; }

        public PropertyInfo(string name, TypeSyntax typeSyntax, ITypeSymbol typeInfo, bool rangeFilter)
        {
            Name = name;
            TypeInfo = typeInfo;
            TypeSyntax = typeSyntax;
            RangeFilter = rangeFilter;
        }
    }
}
