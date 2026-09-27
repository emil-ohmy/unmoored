using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Entry points for headless builds: tools/unity.sh build-mac | build-webgl
public static class BuildScript
{
    static readonly string BuildsDir = Path.GetFullPath(Path.Combine("..", "builds"));

    public static void BuildMac() =>
        Build(BuildTarget.StandaloneOSX, Path.Combine(BuildsDir, "mac", PlayerSettings.productName + ".app"));

    public static void BuildWebGL() =>
        Build(BuildTarget.WebGL, Path.Combine(BuildsDir, "webgl"));

    static void Build(BuildTarget target, string outputPath)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Console.WriteLine($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
        Console.WriteLine($"Build succeeded: {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
    }
}
