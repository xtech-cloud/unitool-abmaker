using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Text.RegularExpressions;

[System.Serializable]
public class SceneObject
{
    public string res = "";
    public string name = "";
    public Vector3 position = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
    public Vector3 scale = Vector3.one;
}

public class SceneMap
{
    public List<SceneObject> objs = new List<SceneObject>();
}

public static class BuildTools
{
    //--------------------------------
    // Settings
    //--------------------------------

    static string outpath = System.IO.Path.Combine(Application.dataPath, "../../_assets/");
    static string outpath_manifest = System.IO.Path.Combine(outpath, "meta");
    static string outpath_scenemap = System.IO.Path.Combine(outpath, "scene");

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
    public static void ExportManifests()
    {
        Directory.CreateDirectory(outpath_manifest);
        exportManifests();
    }

    [MenuItem("BuildTools/Refresh")]
    public static void Refresh()
    {
        refresh();
    }

    [MenuItem("BuildTools/Scene/Export")]
    public static void ExportScene()
    {
        Directory.CreateDirectory(outpath_scenemap);

        SceneMap sceneMap = new SceneMap();

        GameObject[] objs = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
        foreach(GameObject go in objs)
        {
            if(PrefabUtility.GetPrefabType(go) != PrefabType.PrefabInstance)
                continue;
            
            Object parentObject = PrefabUtility.GetPrefabParent(go); 
            string assetPath = AssetDatabase.GetAssetPath(parentObject);

            if(!assetPath.StartsWith("Assets/Packages"))
                continue;

            string[] paths = assetPath.Split('/');
            if(paths.Length != 5)
                continue;

            SceneObject obj = new SceneObject();
            obj.name = go.name;
            obj.res = string.Format("{0}/{1}", paths[3], paths[4]);
            obj.position = go.transform.position;
            obj.rotation = go.transform.rotation.eulerAngles;
            obj.scale = go.transform.localScale;

            double px = System.Math.Round(obj.position.x, 4);
            double py = System.Math.Round(obj.position.y, 4);
            double pz = System.Math.Round(obj.position.z, 4);
            double rx = System.Math.Round(obj.rotation.x, 4);
            double ry = System.Math.Round(obj.rotation.y, 4);
            double rz = System.Math.Round(obj.rotation.z, 4);
            double sx = System.Math.Round(obj.scale.x, 4);
            double sy = System.Math.Round(obj.scale.y, 4);
            double sz = System.Math.Round(obj.scale.z, 4);
            obj.position = new Vector3((float)px, (float)py, (float)pz);
            obj.rotation = new Vector3((float)rx, (float)ry, (float)rz);
            obj.scale = new Vector3((float)sx, (float)sy, (float)sz);

            sceneMap.objs.Add(obj);
        }
        string json = JsonUtility.ToJson(sceneMap, true);
        File.WriteAllText(Path.Combine(outpath_scenemap, "scene.json"), json);
        Debug.Log("export scene at " + outpath_scenemap);
    }

    private static void refresh()
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

                    List<string> files = new List<string>();
                    ABPack pack = null;

                    string mfPath = Path.Combine(package, "_manifest.asset");
                    string mfAsset = string.Format("Assets/Packages/{0}/{1}/_manifest.asset", groupname, packagename);
                    if(!File.Exists(mfPath))
                    {
                        pack = ScriptableObject.CreateInstance<ABPack>();
                        pack.uuid = packagename;
                        pack.path = groupname;
                        AssetDatabase.CreateAsset(pack, mfAsset);
                    }
                    else
                    {
                        pack = AssetDatabase.LoadAssetAtPath<ABPack>(mfAsset);
                        pack.uuid = packagename;
                        pack.path = groupname;
                    }

                    foreach (string file in Directory.GetFiles(package))
                    {
                        string filename = Path.GetFileName(file);

                        //ignore unity's meta file
                        if (file.EndsWith(".meta"))
                        {
                            continue;
                        }

                        if (Path.GetFileName(file).Equals("_manifest.asset"))
                        {
                            continue;
                        }

                        string assetPath = string.Format("Assets/Packages/{0}/{1}/{2}", groupname, packagename, filename);
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

    private static void exportManifests()
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
        pack.path = group;
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
            AssetDatabase.MoveAsset(selectPath, filePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

}
