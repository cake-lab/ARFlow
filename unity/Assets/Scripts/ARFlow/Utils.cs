using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static void PrintDebug(string msg)
    {
        if (Debug.isDebugBuild)
        {
            Debug.Log(msg);
        }
    }
}
