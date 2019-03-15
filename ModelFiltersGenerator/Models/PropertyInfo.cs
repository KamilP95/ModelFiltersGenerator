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

        public PropertyInfo(string name, TypeSyntax typeSyntax, ITypeSymbol typeInfo)
        {
            Name = name;
            TypeInfo = typeInfo;
            TypeSyntax = typeSyntax;
        }
    }
}
