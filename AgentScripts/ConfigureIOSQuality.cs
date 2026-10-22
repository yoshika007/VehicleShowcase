using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class ConfigureIOSQuality
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        int mobile = Array.IndexOf(QualitySettings.names, "Mobile");
        if (mobile < 0) throw new Exception("Existing Mobile preset is missing");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (pipeline == null) throw new Exception("Mobile pipeline asset is missing");
        Undo.RecordObject(pipeline, "Configure mobile rendering baseline");
        pipeline.renderScale = .8f;
        pipeline.msaaSampleCount = 2;
        pipeline.shadowDistance = 35f;
        pipeline.shadowCascadeCount = 1;
        // Keep per-pixel headlights and roadside lamps, without their shadow-map cost.
        var serializedPipeline = new SerializedObject(pipeline);
        serializedPipeline.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
        serializedPipeline.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
        serializedPipeline.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 4;
        serializedPipeline.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
        serializedPipeline.FindProperty("m_SoftShadowsSupported").boolValue = false;
        serializedPipeline.FindProperty("m_UseSRPBatcher").boolValue = true;
        serializedPipeline.ApplyModifiedProperties();
        QualitySettings.SetQualityLevel(mobile, true);
        QualitySettings.renderPipeline = pipeline;
        QualitySettings.shadowDistance = 35f;
        QualitySettings.shadowCascades = 0; // Legacy API: zero means one cascade.
        QualitySettings.vSyncCount = 0;
        EditorUtility.SetDirty(pipeline);
        // Preserve the existing iPhone default mapping, and validate that Mobile is selected.
        var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
        var settings = new SerializedObject(qualityAsset);
        var defaults = settings.FindProperty("m_PerPlatformDefaultQuality");
        bool found = false;
        for (int i = 0; i < defaults.arraySize; i++)
        {
            var entry = defaults.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue != "iPhone") continue;
            entry.FindPropertyRelative("second").intValue = mobile;
            found = true;
        }
        if (!found) throw new Exception("Expected existing iPhone quality default");
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        return "Mobile active for preview and iOS default; render scale 0.8, 2x MSAA, 35m single-cascade 1024 shadows, four per-pixel additional lights without shadows.";
    }
}
