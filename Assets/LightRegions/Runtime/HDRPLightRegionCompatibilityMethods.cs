using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LightRegions
{
    public static class LightRegionCompatibilityMethods
    {
        public static void RenderLightShadow(Light light)
        {
            light.GetComponent<HDAdditionalLightData>().RequestShadowMapRendering();
        }
        public static void SetupLight(Light light)
        {
            HDAdditionalLightData additionalLightData = light.GetComponent<HDAdditionalLightData>();
            additionalLightData.shadowUpdateMode = ShadowUpdateMode.OnDemand;
            additionalLightData.preserveCachedShadow = false;
        }
        public static void RefreshLights(RegionManager regionManager)
        {
            HDAdditionalLightData[] hdData = Object.FindObjectsOfType<HDAdditionalLightData>();
            foreach(HDAdditionalLightData hdDataItem in hdData)
            {
                hdDataItem.RequestShadowMapRendering();
            }
        }
    }
}
