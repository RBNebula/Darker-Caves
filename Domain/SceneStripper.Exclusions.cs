using System;
using UnityEngine;

namespace DarkCaves.Domain;

internal sealed partial class SceneStripper
{
    private static bool IsExcludedFromTargeting(Transform? transform)
    {
        return IsAttachedToSavableObject(transform) ||
               IsInventoryItemPreviewHierarchy(transform) ||
               IsPlayerHierarchy(transform);
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
}
