using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Recognissimo.Editor
{
    internal class PreBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
#if UNITY_ANDROID
            SetupAndroidBuild();
#endif
        }

        private static void SetupAndroidBuild()
        {
            if (PlayerSettings.Android.forceSDCardPermission) return;
            Debug.Log("Setting up request writing to external storage");
            PlayerSettings.Android.forceSDCardPermission = true;
        }
    }
}