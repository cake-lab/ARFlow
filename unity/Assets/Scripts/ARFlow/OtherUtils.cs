using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARFlow
{
    public static class OtherUtils
    {
        public static void PrintDebug(string msg)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log(msg);
            }
        }
    }

}
