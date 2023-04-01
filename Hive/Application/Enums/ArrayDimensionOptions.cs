using System.ComponentModel;

namespace Hive.Application.Enums
{
    public enum ArrayDimensionOptions
    {
        [Description("Default Value, Do Nothing In Particular")]
        None,
        [Description("Will Force Return Array in to Multi-Dimesnional Even if only 1 Row was Read")]
        ForceMultiDimension,
        [Description("Will Force Return Array in to Single Dimension Regardless of How Many Rows were Read (Useful for Reading Only 1 Column for Multiple Rows)")]
        MultiToSingle
    }
}