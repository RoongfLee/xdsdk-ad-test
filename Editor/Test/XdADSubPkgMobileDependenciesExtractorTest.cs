using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XDSDK.Mobile.ADSubPackage.Editor;

namespace XDSDK.Mobile.ADSubPackage.Editor
{
    [TestFixture]
    [TestOf(typeof(XdADSubPkgMobileDependenciesExtractor))]
    public class XdADSubPkgMobileDependenciesExtractorTest
    {

        [Test]
        public void OnPostGenerateGradleAndroidProject_CalledWithValidPath_CallsLogic()
        {
            var basePath = "Assets/Temp/unityLibrary";
            var gradleFilePath = Path.Combine(basePath, "build.gradle");

            Directory.CreateDirectory(basePath);

            using (var writer = new StreamWriter(gradleFilePath))
            {
                writer.WriteLine("implementation \"androidx.browser:browser:1.4.0\"");
                writer.WriteLine(" implementation(name: 'GDTActionSDK.min.1.8.9', ext:'aar')");
                writer.WriteLine("implementation(name: 'humesdk-1.0.0', ext:'aar')");
                writer.WriteLine("implementation \"androidx.appcompat:appcompat:1.3.1\"");
            }

            var dependenciesExtractor = new XdADSubPkgMobileDependenciesExtractor();
            dependenciesExtractor.OnPostGenerateGradleAndroidProject(basePath);

            var lines = File.ReadAllLines(gradleFilePath);
            Assert.IsTrue(Array.Exists(lines, line => line.Contains("// 我被替换了")));
        }
    }
}