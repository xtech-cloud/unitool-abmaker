using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Text.RegularExpressions;

[System.Serializable]
public class ABFile
{
    public string name = "";
    public string alias = "";
}

public class ABPack : ScriptableObject
{

    public string uuid = "";

    public string path = "";

    public string alias = "";
    public List<ABFile> files = new List<ABFile>(0);
}

public static class BuildTools
{
    //--------------------------------
    // Settings
    //--------------------------------

    static string outpath = System.IO.Path.Combine(Application.dataPath, "../../_assets/");
    static string outpath_manifest = System.IO.Path.Combine(outpath, "meta");

    [MenuItem("BuildTools/AssetBundle/WebGL")]
    public static void BuildAssetBundleForWebGL()
    {
        buildAssetBundle(BuildTarget.WebGL);
    }

    [MenuItem("BuildTools/AssetBundle/Win32")]
    public static void BuildAssetBundleForWin32()
    {
        buildAssetBundle(BuildTarget.StandaloneWindows);
    }

    [MenuItem("BuildTools/AssetBundle/Android")]
    public static void BuildAssetBundleForAndroid()
    {
        buildAssetBundle(BuildTarget.Android);
    }

    private static void buildAssetBundle(BuildTarget _target)
    {
        Directory.CreateDirectory(outpath);
        string path = System.IO.Path.Combine(outpath, convertToPlatform(_target));
        Directory.CreateDirectory(path);
        Debug.Log(string.Format("build assetbundle at {0}", path));
        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.ForceRebuildAssetBundle, _target);

        //remove unused files
        File.Delete(Path.Combine(path, convertToPlatform(_target)));
        File.Delete(Path.Combine(path, convertToPlatform(_target)) + ".manifest");
        foreach (string file in Directory.GetFiles(path))
        {
            if (!file.EndsWith(".manifest"))
                continue;
            string target = Path.Combine(path, Path.GetFileName(file));
            File.Delete(target);
        }
    }

    private static string convertToPlatform(BuildTarget _target)
    {
        if (BuildTarget.StandaloneWindows == _target)
            return "win32";
        if (BuildTarget.StandaloneWindows64 == _target)
            return "win64";
        if (BuildTarget.WebGL == _target)
            return "webgl";
        if (BuildTarget.Android == _target)
            return "android";
        return "unknow";
    }



    [MenuItem("BuildTools/Manifests/Export")]
    public static void BuildManifests()
    {
        Directory.CreateDirectory(outpath_manifest);
        buildManifests();
    }

    [MenuItem("BuildTools/Refresh")]
    public static void Refresh()
    {
        refresh();
    }

    private static void refresh()
    {

        int count = 0;

        string path = System.IO.Path.Combine(Application.dataPath, "Packages");

        DirectoryInfo test = new DirectoryInfo(path);
        if (test.Exists)
        {
            foreach (string group in Directory.GetDirectories(path))
            {
                string groupname = Path.GetFileName(group);

                foreach (string package in Directory.GetDirectories(group))
                {
                    string packagename = Path.GetFileName(package);

                    List<string> files = new List<string>();
                    ABPack pack = null;
                    foreach (string file in Directory.GetFiles(package))
                    {
                        string filename = Path.GetFileName(file);

                        string assetPath = string.Format("Assets/Packages/{0}/{1}/{2}", groupname, packagename, filename);

                        //ignore unity's meta file
                        if (file.EndsWith(".meta"))
                        {
                            continue;
                        }

                        if (Path.GetFileName(file).Equals("_manifest.asset"))
                        {
                            pack = AssetDatabase.LoadAssetAtPath<ABPack>(assetPath);
                            pack.uuid = packagename;
                            pack.path = groupname;
                            continue;
                        }

                        AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(packagename, "");
                        files.Add(Path.GetFileName(file));
                    }

                    if (null == pack)
                    {
                        continue;
                    }

                    Dictionary<string, ABFile> abFiles = new Dictionary<string, ABFile>();
                    foreach(ABFile abFile in pack.files)
                    {
                        abFiles.Add(abFile.name, abFile);
                    }
                    pack.files.Clear();

                    foreach (string file in files)
                    {
                        ABFile abFile = new ABFile();
                        abFile.name = Path.GetFileNameWithoutExtension(file);
                        if(abFiles.ContainsKey(abFile.name))
                        {
                            abFile.alias = abFiles[abFile.name].alias;
                        }
                        pack.files.Add(abFile);
                    }

                    count += 1;
                }
            }
        }
        else
        {
            Debug.Log("Without this directory...");
        }
        
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("refresh finish");
    }

    private static void buildManifests()
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Packages");

        DirectoryInfo test = new DirectoryInfo(path);
        if (test.Exists)
        {
            foreach (string group in Directory.GetDirectories(path))
            {
                string groupname = Path.GetFileName(group);
                foreach (string package in Directory.GetDirectories(group))
                {
                    string packagename = Path.GetFileName(package);

                    foreach (string file in Directory.GetFiles(package))
                    {
                        string filename = Path.GetFileName(file);
                        
                        if(!Path.GetFileName(file).Equals("_manifest.asset"))
                            continue;

                        string mfPath = string.Format("Assets/Packages/{0}/{1}/{2}", groupname, packagename, filename);
                        ABPack pack = AssetDatabase.LoadAssetAtPath<ABPack>(mfPath);
                        if(null == pack)
                            continue;

                        string outPath = Path.Combine(outpath_manifest, packagename + ".mf");
                        string json = JsonUtility.ToJson(pack, true);
                        File.WriteAllText(outPath, json);
                        Debug.Log(string.Format("Write {0}.mf to {1}", packagename, outPath));
                    }
                }
            }
        }
        else
        {
            Debug.Log("Without this directory...");
        }
    }

    [MenuItem("Assets/New Asset")]
    public static void New()
    {
        string[] selection = Selection.assetGUIDs;
        if (selection.Length > 1)
            return;

        string selectPath = AssetDatabase.GUIDToAssetPath(selection[0]);

        string uuid = System.Guid.NewGuid().ToString().Replace("-", "");
        string group = selectPath.Replace("Assets/Packages/", "");

        Directory.CreateDirectory(Path.Combine(selectPath, uuid));

        ABPack pack = ScriptableObject.CreateInstance<ABPack>();
        pack.uuid = uuid;
        pack.path = group.Replace(".", "/");
        string mfPath = string.Format("Assets/Packages/{0}/{1}/_manifest.asset", group, uuid);
        AssetDatabase.CreateAsset(pack, mfPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Process")]
    public static void Process()
    {
        string[] selections = Selection.assetGUIDs;

        string outpath = System.IO.Path.Combine(Application.dataPath, "_out");
        Directory.CreateDirectory(outpath);

        foreach(string selection in selections)
        {
            string uuid = selection;
            Directory.CreateDirectory(Path.Combine(outpath, uuid));

            string selectPath = AssetDatabase.GUIDToAssetPath(selection);

            string mfPath = string.Format("Assets/_out/{0}/_manifest.asset", uuid);
            ABPack pack = ScriptableObject.CreateInstance<ABPack>();
            pack.uuid = uuid;
            pack.path = "";
            AssetDatabase.CreateAsset(pack, mfPath);

            string filePath = string.Format("Assets/_out/{0}/{1}", uuid, Path.GetFileName(selectPath));
            AssetDatabase.CopyAsset(selectPath, filePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

}
