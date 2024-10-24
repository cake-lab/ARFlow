using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARFlow
{
    public static class OtherUtils
    {
        public static void PrintDebug(object message)
        {
            try
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log(message);
                }
            }
            catch
            {
                //Debug.isDebugBuild throws error
                //not on main thread --> skip logging
            }
        }
    }

}
