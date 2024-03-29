#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LC.Newtonsoft.Json;
using LC.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using TapTap.AndroidDependencyResolver.Editor;
using XD.SDK.Common.Editor;

namespace XD.SDK.ADSubPackage
{
    public class XDGADSubGradleProvider : IPreprocessBuildWithReport
    {
        private readonly string _humeSDKFileName = "humesdk-1.0.0";
        private readonly string _gdtActionSDKFileName = "GDTActionSDK.min.1.8.9";
        public int callbackOrder => AndroidGradleProcessor.CALLBACK_ORDER - 51;

        public void OnPreprocessBuild(BuildReport report)
        {
            DeleteOldProvider();
            var provider = FixProvider();
            SaveProvider(provider);
        }

        private AndroidGradleContextProvider FixProvider()
        {
            AndroidGradleContextProvider result = new AndroidGradleContextProvider();
            result.Version = 1;
            result.Priority = 3;
            result.Use = true;
            result.ModuleName = "XD.ADSub";
            var tmp = GetGradleContext();
            result.AndroidGradleContext = tmp;
            return result;
        }

        private void DeleteOldProvider()
        {
            var folderPath = Path.Combine(Application.dataPath, "XDSDK", "Gen", "ADSub");
            if (!Directory.Exists(folderPath)) return;
            var path = Path.Combine(folderPath, "TapAndroidProvider.txt");
            if (File.Exists(path)) File.Delete(path);
        }

        private void SaveProvider(AndroidGradleContextProvider provider)
        {
            var folderPath = Path.Combine(Application.dataPath, "XDSDK", "Gen", "ADSub");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            var path = Path.Combine(folderPath, "TapAndroidProvider.txt");
            AndroidUtils.SaveProvider(path, provider);
        }

        public List<AndroidGradleContext> GetGradleContext()
        {
            var result = GetGradleContextByXDConfig();

            var jsonPath = XDGCommonEditorUtils.GetXDConfigPath("XDConfig");
            if (!File.Exists(jsonPath)) return result;
            var configObject = JObject.Parse(File.ReadAllText(jsonPath));
            if (configObject["ad_config"]?["tt_config"] == null) return result;
            var bytedanceDeps = new AndroidGradleContext
            {
                // 仓库地址  
                locationType = AndroidGradleLocationType.End,
                templateType = CustomTemplateType.BaseGradle,
                processContent = new List<string>
                {
                    "allprojects {\n    repositories {\n        maven { url 'https://artifact.bytedance.com/repository/Volcengine/' }\n    }\n}"
                }
            };
            result.Add(bytedanceDeps);

            return result;
        }

        private List<AndroidGradleContext> GetGradleContextByXDConfig()
        {
            var result = new List<AndroidGradleContext>();

            var jsonPath = XDGCommonEditorUtils.GetXDConfigPath("XDConfig");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("/Assets/XDConfig.json 配置文件不存在！");
                return result;
            }

            var configObject = JObject.Parse(File.ReadAllText(jsonPath));

            var thirdPartyDeps = new AndroidGradleContext();
            thirdPartyDeps.locationType = AndroidGradleLocationType.Builtin;
            thirdPartyDeps.locationParam = "DEPS";
            thirdPartyDeps.templateType = CustomTemplateType.UnityMainGradle;
            thirdPartyDeps.processContent = new List<string>();

            if (configObject["ad_config"]?["tt_config"] != null)
            {
                // 今日头条广告包 SDK
                // Applog 上报组件（必须）
                thirdPartyDeps.processContent.Add(
                    @"    implementation 'com.bytedance.applog:RangersAppLog-Lite-cn:6.14.3'");
                // 商业化组件（必须）6.14.2 及以上版本商业化组件独立，注：必须接入，以免平台限制应用上传
                thirdPartyDeps.processContent.Add(
                    @"    implementation 'com.bytedance.applog:RangersAppLog-All-convert:6.14.3'");
                // 今日头条分包 SDK
                SetNativeAarCompatible(_humeSDKFileName, true);
            }
            else
            {
                // 今日头条分包 SDK
                SetNativeAarCompatible(_humeSDKFileName, false);
            }

            if (configObject["ad_config"]?["gdt_config"] != null)
            {
                // 广点通分包 SDK
                thirdPartyDeps.processContent.Add(@"    implementation 'com.tencent.vasdolly:helper:3.0.4'");
                // 广点通广告 SDK
                SetNativeAarCompatible(_gdtActionSDKFileName, true);
            }
            else
            {
                // 广点通广告 SDK
                SetNativeAarCompatible(_gdtActionSDKFileName, false);
            }


            if (thirdPartyDeps.processContent.Count > 0)
            {
                result.Add(thirdPartyDeps);
            }

            return result;
        }

        /// <summary>
        /// 修改本地 aar 文件属性
        /// </summary>
        /// <param name="aarFileName">本地 aar 文件路径</param>
        /// <param name="androidCompatibleEnable">针对 Android 平台的兼容性是否可用</param>
        private static void SetNativeAarCompatible(string aarFileName, bool androidCompatibleEnable)
        {
            Debug.Log("SetNativeAarCompatible -----> " + aarFileName + ", enable: " + androidCompatibleEnable);
            try
            {
                var aarFilePath = XDGCommonEditorUtils.GetNativeAarFilePath(aarFileName);
                var pluginImporter = AssetImporter.GetAtPath(aarFilePath) as PluginImporter;
                if (pluginImporter == null)
                {
                    Debug.LogWarning("can't get native aar library AssetImporter: " + aarFilePath);
                    return;
                }

                pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, androidCompatibleEnable);
                pluginImporter.SetCompatibleWithAnyPlatform(false); // 禁用兼容性
                pluginImporter.SaveAndReimport();
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            Debug.LogWarning("can't disable native aar library: " + aarFileName);
        }
    }
}

#endif