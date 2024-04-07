#if UNITY_EDITOR && UNITY_ANDROID

using System.IO;
using LC.Newtonsoft.Json.Linq;
using TapTap.AndroidDependencyResolver.Editor;
using UnityEditor.Android;
using UnityEngine;


namespace XDSDK.Mobile.ADSubPackage.Editor
{
    public class XdADSubPkgMobileDependenciesExtractor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => AndroidGradleProcessor.CALLBACK_ORDER - 51;

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            var xdConfigJsonPath = Utils.GetXDConfigPath();
            if (!File.Exists(xdConfigJsonPath))
            {
                return;
            }

            var configObj = JObject.Parse(File.ReadAllText(xdConfigJsonPath));

            var hasTtConfig = configObj["ad_config"]?["tt_config"] != null;
            var hasGdtConfig = configObj["ad_config"]?["gdt_config"] != null;

            if (hasTtConfig && hasGdtConfig)
            {
                return;
            }

            var unityLibraryGradleFilePath = Path.Combine(basePath, "build.gradle");
            if (!File.Exists(unityLibraryGradleFilePath)) return;

            var gradleContentsLines = File.ReadAllLines(unityLibraryGradleFilePath);
            for (var i = 0; i < gradleContentsLines.Length; i++)
            {
                if ((hasTtConfig || !gradleContentsLines[i].Contains("humesdk-1.0.0"))
                    && (hasGdtConfig || !gradleContentsLines[i].Contains("GDTActionSDK.min"))) continue;

                Debug.LogFormat($"disable library dependency: {gradleContentsLines[i]}");
                gradleContentsLines[i] = $"// {gradleContentsLines[i]}";
            }

            File.WriteAllLines(unityLibraryGradleFilePath, gradleContentsLines);
        }
    }
}
#endif