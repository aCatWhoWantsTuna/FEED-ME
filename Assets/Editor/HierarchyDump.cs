using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dumps the open scene's hierarchy to a text file or copies it to the clipboard.
/// Editor-only: place this script inside an Editor/ folder.
/// </summary>
public static class HierarchyDump
{
    [MenuItem("Tools/Dump Scene Hierarchy (Save to File)")]
    static void DumpToFile()
    {
        Scene scene = SceneManager.GetActiveScene();
        string output = BuildHierarchyText(scene);

        string path = EditorUtility.SaveFilePanel(
            "Dump scene hierarchy", "", $"{scene.name}-hierarchy", "txt");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        File.WriteAllText(path, output);
        Debug.Log($"Saved scene hierarchy to {path}");
    }

    [MenuItem("Tools/Dump Scene Hierarchy (Copy to Clipboard)")]
    static void DumpToClipboard()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorGUIUtility.systemCopyBuffer = BuildHierarchyText(scene);
        Debug.Log("Scene hierarchy copied to clipboard!");
    }

    static string BuildHierarchyText(Scene scene)
    {
        var text = new StringBuilder();
        text.AppendLine(scene.name);

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Append(roots[i].transform, text, "", i == roots.Length - 1);
        }

        return text.ToString();
    }

    static void Append(Transform transform, StringBuilder text, string prefix, bool isLast)
    {
        text.Append(prefix)
            .Append(isLast ? "└── " : "├── ")
            .Append(transform.name);

        // Append the tag if it is not set to "Untagged"
        if (!transform.CompareTag("Untagged"))
        {
            text.Append($" <Tag: {transform.tag}>");
        }

        // Append all component names attached to this GameObject
        AppendComponents(transform, text);
        text.AppendLine();

        string childPrefix = prefix + (isLast ? "    " : "│   ");
        for (int i = 0; i < transform.childCount; i++)
        {
            Append(transform.GetChild(i), text, childPrefix, i == transform.childCount - 1);
        }
    }

    static void AppendComponents(Transform transform, StringBuilder text)
    {
        var names = new List<string>();
        foreach (Component component in transform.GetComponents<Component>())
        {
            // GetComponents returns a null entry if a MonoBehaviour script is missing
            names.Add(component == null ? "<Missing Script>" : component.GetType().Name);
        }
        text.Append(" [").Append(string.Join(", ", names)).Append(']');
    }
}