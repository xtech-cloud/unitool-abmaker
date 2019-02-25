using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
