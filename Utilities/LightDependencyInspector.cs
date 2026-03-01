using System;
using UnityEngine;

namespace DarkCaves.Utilities;

internal static class LightDependencyInspector
{
    public static bool RequiresLightByAttribute(Type type)
    {
        object[] attrs = type.GetCustomAttributes(typeof(RequireComponent), true);
        for (int i = 0; i < attrs.Length; i++)
        {
            RequireComponent req = (RequireComponent)attrs[i];
            if (RequiresLight(req))
            {
                return true;
            }
        }

        return false;
    }

    public static bool RequiresLight(RequireComponent req)
    {
        Type lightType = typeof(Light);
        return req.m_Type0 == lightType || req.m_Type1 == lightType || req.m_Type2 == lightType;
    }
}
