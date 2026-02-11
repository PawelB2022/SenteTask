using System.Globalization;

namespace DbMetaTool.Firebird;

/// <summary>
/// Converts Firebird internal field type information to SQL type declarations.
/// </summary>
public static class FirebirdTypeMapper
{
    public static string FieldTypeToSql(
        short fieldType,
        short fieldSubType,
        short fieldLength,
        short? precision,
        short? scale,
        short? charLength)
    {
        var scaleAbs = scale.HasValue ? Math.Abs(scale.Value) : 0;

        return fieldType switch
        {
            7 => "SMALLINT",
            8 => "INTEGER",
            16 => fieldSubType switch
            {
                1 => precision.HasValue ? $"NUMERIC({precision.Value},{scaleAbs})" : "NUMERIC",
                2 => precision.HasValue ? $"DECIMAL({precision.Value},{scaleAbs})" : "DECIMAL",
                _ => "BIGINT"
            },
            10 => "FLOAT",
            27 => "DOUBLE PRECISION",
            12 => "DATE",
            13 => "TIME",
            35 => "TIMESTAMP",
            14 => $"CHAR({(charLength ?? fieldLength).ToString(CultureInfo.InvariantCulture)})",
            37 => $"VARCHAR({(charLength ?? fieldLength).ToString(CultureInfo.InvariantCulture)})",
            261 => "BLOB",
            _ => "BLOB"
        };
    }
}
