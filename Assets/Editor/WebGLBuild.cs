using UnityEditor;

public static class WebGLBuild
{
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
}
