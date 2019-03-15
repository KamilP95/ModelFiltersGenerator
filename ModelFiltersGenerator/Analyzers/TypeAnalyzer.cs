using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModelFiltersGenerator.Analyzers
{
    internal static class TypeAnalyzer
    {
        internal static class SupprotedTypes
        {
            internal static SpecialType Boolean => SpecialType.System_Boolean;
            internal static SpecialType Byte => SpecialType.System_Byte;
            internal static SpecialType DateTime => SpecialType.System_DateTime;
            internal static SpecialType Decimal => SpecialType.System_Decimal;
            internal static SpecialType Double => SpecialType.System_Double;
            internal static SpecialType Int16 => SpecialType.System_Int16;
            internal static SpecialType Int32 => SpecialType.System_Int32;
            internal static SpecialType Int64 => SpecialType.System_Int64;
            internal static SpecialType String => SpecialType.System_String;
            internal static SpecialType Enum => SpecialType.System_Enum;

            internal static IEnumerable<SpecialType> All => new[]
                {Boolean, Byte, DateTime, Decimal, Double, Int16, Int32, Int64, String, Enum};
        }

        internal static bool IsNumericType(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Byte
                   || type.SpecialType == SupprotedTypes.Double
                   || type.SpecialType == SupprotedTypes.Decimal
                   || type.SpecialType == SupprotedTypes.Int16
                   || type.SpecialType == SupprotedTypes.Int32
                   || type.SpecialType == SupprotedTypes.Int64;
        }

        internal static bool IsRealNumbersType(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Double
                   || type.SpecialType == SupprotedTypes.Decimal;
        }

        internal static bool IsInteagerType(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Byte
                   || type.SpecialType == SupprotedTypes.Int16
                   || type.SpecialType == SupprotedTypes.Int32
                   || type.SpecialType == SupprotedTypes.Int64;
        }

        internal static bool IsDateTime(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.DateTime;
        }

        internal static bool IsString(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.String;
        }

        internal static bool IsBool(this ITypeSymbol type)
        {
            return type.SpecialType == SupprotedTypes.Boolean;
        }

        internal static bool IsSupported(this ITypeSymbol type)
        {
            return SupprotedTypes.All.Contains(type.SpecialType) || type.TypeKind == TypeKind.Enum;
        }
    }
}
