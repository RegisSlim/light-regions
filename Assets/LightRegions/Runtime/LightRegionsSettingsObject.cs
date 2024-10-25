using UnityEditor;
using UnityEngine;

namespace LightRegions
{
    [CreateAssetMenu(fileName = "LightRegionsSettings", menuName = "Light Regions/Light Regions Settings", order = 1)]
    public class LightRegionsSettings : ScriptableObject
    {
        public int regionLayer;
        public int lightInfluenceLayer;

        //Default bake settings
        public LayerMask occlusionBakeMask;
        public int raysPerLight = 1024;
        public int bouncesPerRay = 3;
        public float maxRayLength = 400;
        public float rayDiffusion = 2f;
        public bool testAllRegions = true;
        //Light mesher settings
        public float influenceMeshDecimationFactor = 0.8f;
        public int influneceMeshProjectionSubdivisions = 3;

        public bool reparentRegions = true;
        public bool reparentObjects = false;
        public LayerMask regionFitLayerMask;
        public float regionFitToolRange = 40;
        public float regionFitToolHorizontalThickness = 0.01f;
        public float regionFitToolVerticalThickness = 0.01f;

        public bool showRegionLabels = true;

        public ShowRegionMode currentShowRegionMode = ShowRegionMode.All;
    }

    [CustomEditor(typeof(LightRegionsSettings))]
    public class LightRegionsSettingsEditor : Editor
    {
        SerializedProperty regionLayer;
        SerializedProperty lightInfluenceLayer;

        SerializedProperty occlusionBakeMask;
        SerializedProperty raysPerLight;
        SerializedProperty bouncesPerRay;
        SerializedProperty maxRayLength;
        SerializedProperty rayDiffusion;
        SerializedProperty testAllRegions;

        SerializedProperty influenceMeshDecimationFactor;
        SerializedProperty influenceMeshProjectionSubdivisions;
        
        SerializedProperty reparentRegions;
        SerializedProperty reparentObjects;

        private void OnEnable()
        {
            regionLayer = serializedObject.FindProperty("regionLayer");
            lightInfluenceLayer = serializedObject.FindProperty("lightInfluenceLayer");
            occlusionBakeMask = serializedObject.FindProperty("occlusionBakeMask");
            raysPerLight = serializedObject.FindProperty("raysPerLight");
            bouncesPerRay = serializedObject.FindProperty("bouncesPerRay");
            maxRayLength = serializedObject.FindProperty("maxRayLength");
            influenceMeshDecimationFactor = serializedObject.FindProperty("influenceMeshDecimationFactor");
            influenceMeshProjectionSubdivisions = serializedObject.FindProperty("influneceMeshProjectionSubdivisions");
            reparentRegions = serializedObject.FindProperty("reparentRegions");
            reparentObjects = serializedObject.FindProperty("reparentObjects");
            rayDiffusion = serializedObject.FindProperty("rayDiffusion");
            testAllRegions = serializedObject.FindProperty("testAllRegions");
        }

        public override void OnInspectorGUI()
        {
            LightRegionsSettings settings = (LightRegionsSettings)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer Settings", EditorStyles.boldLabel);
            regionLayer.intValue = EditorGUILayout.LayerField("Region Layer", regionLayer.intValue);
            EditorGUILayout.LabelField("The layer used for the Region objects. This should be dedicated to the region system and nothing else. If you haven't already, consider making a new layer named 'Region' and set it here.", EditorStyles.helpBox);
            lightInfluenceLayer.intValue = EditorGUILayout.LayerField("Influence Layer", lightInfluenceLayer.intValue);
            EditorGUILayout.LabelField("The layer used for the light influence mesh check. If you haven't already, create a new Layer named 'LightInfluence' and set it here.", EditorStyles.helpBox);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Occlusion Settings", EditorStyles.boldLabel);

            raysPerLight.intValue = EditorGUILayout.IntField("Rays Per Light: ", raysPerLight.intValue);
            bouncesPerRay.intValue = EditorGUILayout.IntField("Bounces Per Ray: ", bouncesPerRay.intValue);
            maxRayLength.floatValue = EditorGUILayout.FloatField("Max Ray Length: ", maxRayLength.floatValue);
            rayDiffusion.floatValue = EditorGUILayout.FloatField("Ray Diffusion: ", rayDiffusion.floatValue);
            EditorGUILayout.LabelField("The diffusion value changes the degree of random scattering applied to each bounce.", EditorStyles.helpBox);
            testAllRegions.boolValue = EditorGUILayout.Toggle("Test All Regions: ", testAllRegions.boolValue);
            EditorGUILayout.LabelField("If this is enabled all regions will recieve an additional ray test from their center at half resolution.", EditorStyles.helpBox);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Light Influence Settings", EditorStyles.boldLabel);
            influenceMeshDecimationFactor.floatValue = EditorGUILayout.FloatField("Influence Decimation Factor: ", influenceMeshDecimationFactor.floatValue);
            influenceMeshProjectionSubdivisions.intValue = EditorGUILayout.IntField("Influence Projection Subdivisions: ", influenceMeshProjectionSubdivisions.intValue);
            EditorDataHelper.RegionLayer = regionLayer.intValue;

            EditorDataHelper.InfluenceLayer = lightInfluenceLayer.intValue;
            EditorGUILayout.Space();
            reparentRegions.boolValue = EditorGUILayout.Toggle("Re-parent Regions: ", reparentRegions.boolValue);
            EditorGUILayout.LabelField("If this is enabled Regions will be re-parented to their RegionManager", EditorStyles.helpBox);
            reparentObjects.boolValue = EditorGUILayout.Toggle("Re-parent Objects: ", reparentObjects.boolValue);
            EditorGUILayout.LabelField("If this is enabled RegionObjects will be re-parented to their Region", EditorStyles.helpBox);

            serializedObject.ApplyModifiedProperties();
            EditorDataHelper.BuildMasks();
            //base.OnInspectorGUI();
        }
    }
}
