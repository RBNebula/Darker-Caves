using System;
using UnityEngine;

namespace DarkCaves.Core;

internal sealed partial class SceneStripper
{
    private static readonly string[] _waterNameTokens =
    {
        "water",
        "caustic",
        "ocean",
        "river",
        "lake",
        "pond",
    };

    private static bool IsExcludedFromTargeting(Transform? transform)
    {
        return IsAttachedToSavableObject(transform) ||
               IsInventoryItemPreviewHierarchy(transform) ||
               IsPlayerHierarchy(transform) ||
               IsWaterHierarchy(transform);
    }

    private static bool IsAttachedToSavableObject(Transform? transform)
    {
        if (transform == null)
        {
            return false;
        }

        Transform? current = transform;
        while (current != null)
        {
            if (HasSavableObjectOnGameObject(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsInventoryItemPreviewHierarchy(Transform? transform)
    {
        if (transform == null)
        {
            return false;
        }

        Transform? current = transform;
        while (current != null)
        {
            string objectName = current.name ?? string.Empty;
            if (objectName.IndexOf("InventoryItemPreview", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsPlayerHierarchy(Transform? transform)
    {
        if (transform == null)
        {
            return false;
        }

        Transform? current = transform;
        while (current != null)
        {
            if (current.GetComponent<PlayerController>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasSavableObjectOnGameObject(GameObject gameObject)
    {
        MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is not ISaveLoadableObject)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsWaterHierarchy(Transform? transform)
    {
        if (transform == null)
        {
            return false;
        }

        Transform? current = transform;
        while (current != null)
        {
            if (current.GetComponent<WaterVolume>() != null)
            {
                return true;
            }

            if (HasWaterLikeComponentOnGameObject(current.gameObject))
            {
                return true;
            }

            if (ContainsWaterToken(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasWaterLikeComponentOnGameObject(GameObject gameObject)
    {
        Component[] components = gameObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();
            string typeName = type.FullName ?? type.Name;
            if (ContainsWaterToken(typeName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsWaterToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string source = value!;
        for (int i = 0; i < _waterNameTokens.Length; i++)
        {
            if (source.IndexOf(_waterNameTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}

