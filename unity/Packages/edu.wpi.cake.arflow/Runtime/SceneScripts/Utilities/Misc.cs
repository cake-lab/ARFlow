using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CakeLab.ARFlow.Utilities;

public static class Misc
{
    public static void PrettyPrintDictionary(Dictionary<string, bool> dict)
    {
        string log = "";
        foreach (var kvp in dict)
        {
            //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            log += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        }
        InternalDebug.Log(log);
    }
}
