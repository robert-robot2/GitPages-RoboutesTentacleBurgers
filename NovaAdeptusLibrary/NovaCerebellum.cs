

using System.Collections.Generic;
using System.Text.Json;

namespace NovaAdeptusLibrary
{
    public static class NovaCerebellum
    {
        public static Dictionary<string, List<string>> Examples { get; set; } = new();
     
        public static int TotalExamples()
        {
            int count = 0;
            foreach (var list in Examples.Values)
                count += list.Count;
            return count;
        }
    }
}