using System.Runtime.InteropServices;
using System.Text;
using RGiesecke.DllExport;
using ArmaTools.ArrayParser;

namespace Hive
{
    public class RVEntry
    {
        [DllExport("_RVExtension@12", CallingConvention = CallingConvention.Winapi)]
        public static void RVExtension(StringBuilder output, int outputSize,
            [MarshalAs(UnmanagedType.LPStr)] string function)
        {
            outputSize--;
        }
        
    }
}