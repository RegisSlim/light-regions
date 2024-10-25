using Unity.VisualScripting;
using UnityEngine;

namespace LightRegions
{
    public class RegionUpdater : MonoBehaviour
    {
        [HideInInspector] public RegionManager regionManager;
        [HideInInspector] public Region region;
        [InspectorLabel("If this is set to false the Region Updater will no longer effect the loaded regions but will still fire region events.")]
        public bool isMainUpdater = true;
        public Transform trackingTarget;
        [InspectorLabel("Set this to the GameObject this RegionUpdater should return when it sets off an event. (If you need to)")]
        public GameObject eventReturnGameObject;
        private bool init = false;
        private void Update()
        {
            if (!init)
            {
                if (regionManager == null)
                {
                    regionManager = LightRegionsUtility.GetManagerAtPoint(transform.position);
                    if(regionManager != null)
                    {
                        Init();
                    }
                    else
                    {
                        return;
                    }

                }
            }


        }
        private void Init()
        {
            init = true;
            gameObject.layer = regionManager.regionLayer;
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.enabled = true;
            sphereCollider.excludeLayers = regionManager.regionMaskExclude;
            sphereCollider.includeLayers = regionManager.regionMaskInclude;
            if (!isMainUpdater)
            {
                return;
            }
            int id = regionManager.GetRegionIDFromPos(transform.position);
            if (id != -1)
            {
                regionManager.SetActiveRegion(id);
            }
        }
        private void FixedUpdate()
        {
            if(trackingTarget != null)
            {
                transform.position = trackingTarget.position;
                return;
            }
            transform.position = Camera.main.transform.position;
        }
    }
}
