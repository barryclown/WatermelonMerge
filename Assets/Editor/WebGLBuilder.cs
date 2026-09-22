using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 無人值守 WebGL 建置入口：Unity.exe -batchmode -executeMethod WebGLBuilder.Build
public static class WebGLBuilder
{
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/Merge.unity" };

        // Brotli＋解壓 fallback：任何純靜態主機（GitHub Pages）都能載，不需伺服器設 Content-Encoding
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultWebScreenWidth = 486;   // 9:16 直式，配合手機版面設計
        PlayerSettings.defaultWebScreenHeight = 864;
        PlayerSettings.runInBackground = false;

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            // 輸出到目前使用者的 桌面\claude\webgl-build，不寫死使用者路徑
            locationPathName = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "claude", "webgl-build", "watermelon"),
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Console.WriteLine("WEBGL_BUILD_RESULT=" + s.result + " sizeMB=" + (s.totalSize / (1024f * 1024f)).ToString("F1") + " errors=" + s.totalErrors);
        EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
