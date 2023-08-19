#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GUIDMapper : EditorWindow
{
    private MonoScript scriptA;
    private MonoScript scriptB;
    
    private List<string> m_Files;
    private string oldGUID;
    private List<string> m_FilteredFiles = new List<string>();
    [MenuItem("Tools/GUID Mapper")]
    public static void ShowTool()
    {
        GetWindow<GUIDMapper>().Show();
    }

    public void OnGUI()
    {
        GUILayout.Label("Replace references of scripts");
        scriptA = (MonoScript)EditorGUILayout.ObjectField(scriptA, typeof(MonoScript), false);
        GUILayout.Label("to", EditorStyles.boldLabel);

        scriptB = (MonoScript)EditorGUILayout.ObjectField(scriptB, typeof(MonoScript), false);
        EditorStyles.boldLabel.wordWrap = true;
        GUILayout.Label("WARNING: Ensure the new script has the same variables as the last one!", EditorStyles.boldLabel);
        EditorStyles.boldLabel.wordWrap = false;
        if (GUILayout.Button("Replace GUIDs"))
        {
            string guidOld = GetGUID(scriptA);
            string guidNew = GetGUID(scriptB);
            
            //DataPath in editor returns Project/Assets
            m_Files = FindYamlFiles(Application.dataPath);
            
            FilterBy(guidOld);
            Print();
            AssetDatabase.SaveAssets();
            if(EditorUtility.DisplayDialog("Confirm action", ConstructListFiles(), "Yes", "No"))
            {
                ReplaceWith(guidNew);
            }
            AssetDatabase.SaveAssets();
        }
    }
    public void Print()
    {
        Console.WriteLine();
        Console.WriteLine($"Total {m_FilteredFiles.Count} assets found:");
        foreach (var file in m_FilteredFiles)
        {
            Console.WriteLine("\t" + Path.GetFileName(file));
        }
    }
    string ConstructListFiles()
    {
        string list = $"Total of {m_FilteredFiles.Count} assets found containing the GUID:";
        foreach (var file in m_FilteredFiles)
        {
            list += $"\n\t{Path.GetFileName(file)}";
        }

        list += "\nDo you want to proceed?";

        return list;
    }
    public string GetGUID(MonoScript script)
    {
        string pathScript = AssetDatabase.GetAssetPath(script);
        pathScript = Path.ChangeExtension(@pathScript, ".cs.meta");
        //GUIDs in script meta files are always the 2nd line, we also remove the first 6 characters, which are: "guid: "
        string guid = File.ReadLines(@pathScript).ToList()[1].Remove(0, 6);
            
        Debug.Log($"Script GUID: {guid}", script);
        return guid;
    }
    
    private static List<string> FindYamlFiles(string path)
    {
        return FileSearch.FindFiles(path, (file) =>
        {
            using (var readingFile = new StreamReader(file))
            {
                var firstLine = readingFile.ReadLine();
                if (firstLine == null)
                    return false;
                return firstLine.Contains("%YAML");
            }
        });
    }
    
    private static string GetGuidExpression(string guid)
    {
        return $"guid: {guid}";
    }
    public void FilterBy(string guid)
    {
        // Find files that contains given GUID.
        oldGUID = "";
        m_FilteredFiles.Clear();

        // Just clear filter.
        if (string.IsNullOrEmpty(guid))
        {
            return;
        }

        // Filter by guid.
        oldGUID = guid;
        foreach (var file in m_Files)
        {
            var fileText = File.ReadAllText(file);
            if (fileText.Contains(GetGuidExpression(oldGUID)))
            {
                m_FilteredFiles.Add(file);
            }
        }
    }

    public void ReplaceWith(string guid)
    {
        // Replace old GUID with the new GUID given.
        foreach (var file in m_FilteredFiles)
        {
            var fileText = File.ReadAllText(file);
            fileText = fileText.Replace(GetGuidExpression(oldGUID), GetGuidExpression(guid));
            File.WriteAllText(file, fileText);
        }

        EditorUtility.DisplayDialog("Success", "GUIDs have been replaced, close this window to reload the assets.",
            "OK");
    }
}
public class FileSearch
{
    public static List<string> FindFiles(string path, Predicate<string> predicate)
    {
        var files = new List<string>();
        FindFilesRecursively(path, files, predicate);
        return files;
    }

    private static void FindFilesRecursively(string path, List<string> files, Predicate<string> predicate)
    {
        foreach (var d in Directory.GetDirectories(path))
        {
            foreach (var f in Directory.GetFiles(d))
            {
                if (predicate.Invoke(f))
                {
                    files.Add(f);
                }
            }
            FindFilesRecursively(d, files, predicate);
        }
    }
}
#endif