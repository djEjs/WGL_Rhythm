using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.Text.RegularExpressions;

public sealed class WebGLBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public static void Build()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            "Build/WebGL",
            BuildTarget.WebGL,
            BuildOptions.None);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
        {
            return;
        }

        ApplyCacheBust(report.summary.outputPath);
    }

    private static void ApplyCacheBust(string outputPath)
    {
        var indexPath = Path.Combine(outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            return;
        }

        var version = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var html = File.ReadAllText(indexPath);

        html = Regex.Replace(
            html,
            "var buildUrl = \"Build\";\\s*var loaderUrl = buildUrl \\+ \"/WGL\\.loader\\.js\"(?: \\+ \"\\?v=\" \\+ buildVersion)?;",
            $"var buildUrl = \"Build\";\n      var buildVersion = \"{version}\";\n      var loaderUrl = buildUrl + \"/WGL.loader.js?v=\" + buildVersion;");

        html = Regex.Replace(html, "dataUrl: buildUrl \\+ \"/WGL\\.data\\.unityweb\"(?: \\+ \"\\?v=\" \\+ buildVersion)?,", "dataUrl: buildUrl + \"/WGL.data.unityweb?v=\" + buildVersion,");
        html = Regex.Replace(html, "frameworkUrl: buildUrl \\+ \"/WGL\\.framework\\.js\\.unityweb\"(?: \\+ \"\\?v=\" \\+ buildVersion)?,", "frameworkUrl: buildUrl + \"/WGL.framework.js.unityweb?v=\" + buildVersion,");
        html = Regex.Replace(html, "codeUrl: buildUrl \\+ \"/WGL\\.wasm\\.unityweb\"(?: \\+ \"\\?v=\" \\+ buildVersion)?,", "codeUrl: buildUrl + \"/WGL.wasm.unityweb?v=\" + buildVersion,");
        html = Regex.Replace(html, "productVersion: \"[^\"]*\",", "productVersion: buildVersion,");

        File.WriteAllText(indexPath, html);
    }
}
