using System;
using System.Collections.Generic;

namespace DarkCaves.Utilities;

internal static class SavableObjectIdCsvParser
{
    public static HashSet<int> Parse(string csv)
    {
        HashSet<int> result = new();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return result;
        }

        string[] tokens = csv.Split(',');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (token.Length == 0)
            {
                continue;
            }

            if (int.TryParse(token, out int idValue))
            {
                result.Add(idValue);
                continue;
            }

            if (Enum.TryParse(token, true, out SavableObjectID enumValue))
            {
                result.Add((int)enumValue);
            }
        }

        return result;
    }
}
