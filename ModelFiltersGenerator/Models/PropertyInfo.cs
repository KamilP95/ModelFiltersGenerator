using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelFiltersGenerator.Analyzers;

namespace ModelFiltersGenerator.Models
{
    internal class PropertyInfo
    {
        public string Name { get; }

        public ITypeSymbol TypeInfo { get; }

        public TypeSyntax TypeSyntax { get; }

        public bool RangeFilter => TypeInfo.IsDateTime() || TypeInfo.IsNumericType();

        public bool Included { get; set; }

        public FilterType FilterType { get; set; }

        public PropertyInfo(string name, TypeSyntax typeSyntax, ITypeSymbol typeInfo)
        {
            Name = name;
            TypeInfo = typeInfo;
            TypeSyntax = typeSyntax;
            Included = typeInfo.IsString();
            FilterType = FilterType.Equals;
        }
    }

    internal enum FilterType
    {
        Equals,
        Contains,
        Range
    }
}
