using UnityEditor;

public static class WebGLBuild
{
    public static void Build()
    {
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            "Build/WebGL",
            BuildTarget.WebGL,
            BuildOptions.None);
    }
}
