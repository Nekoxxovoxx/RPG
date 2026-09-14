using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorSelectionReloadGuard
{
    static EditorSelectionReloadGuard()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= ClearSelectionBeforeAssemblyReload;
        AssemblyReloadEvents.beforeAssemblyReload += ClearSelectionBeforeAssemblyReload;
        EditorApplication.delayCall += ClearSelectionAfterReload;
    }

    private static void ClearSelectionBeforeAssemblyReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Selection.objects = Array.Empty<UnityEngine.Object>();
    }

    private static void ClearSelectionAfterReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Selection.objects = Array.Empty<UnityEngine.Object>();
        CloseAnimatorGraphWindows();
    }

    private static void CloseAnimatorGraphWindows()
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

        for (int i = 0; i < windows.Length; i++)
        {
            EditorWindow window = windows[i];

            if (window == null)
                continue;

            string typeName = window.GetType().FullName ?? window.GetType().Name;

            if (!typeName.Contains("AnimatorControllerTool") && !typeName.Contains("AnimatorWindow"))
                continue;

            window.Close();
        }
    }
}
