// PUT THIS FILE IN A FOLDER NAMED "Editor" (e.g. Assets/Code/Editor/TrimAllIngredientIds.cs)
using UnityEditor;
using UnityEngine;

public static class TrimAllIngredientIds
{
    [MenuItem("Tools/Trim All Ingredient Ids")]
    private static void Run()
    {
        int fixedCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:IngredientData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var d = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            if (d == null) continue;

            string newId = d.id?.Trim();
            string newName = d.displayName?.Trim();

            if (newId != d.id || newName != d.displayName)
            {
                Debug.Log($"[TrimAll] {path}: id '{d.id}' -> '{newId}', name '{d.displayName}' -> '{newName}'", d);
                d.id = newId;
                d.displayName = newName;
                EditorUtility.SetDirty(d);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TrimAll] Done. Fixed {fixedCount} ingredient asset(s).");
    }
}