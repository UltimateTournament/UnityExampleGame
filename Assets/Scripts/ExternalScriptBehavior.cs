
using System.Runtime.InteropServices;
using UnityEngine;


namespace Assets.Scripts.Core
{
    public class ExternalScriptBehavior
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern void Log(string str);

        [DllImport("__Internal")]
        public static extern bool IsSecure();

        [DllImport("__Internal")]
        public static extern string Hostname();

        [DllImport("__Internal")]
        public static extern int Port();

#else
        public static void Log(string str)
        {
            Debug.Log(str);
        }
        public static bool IsSecure()
        {
            return false;
        }
        public static string Hostname()
        {
            return "localhost";
        }
        public static int Port()
        {
            return 7778;
        }
#endif

    }
}