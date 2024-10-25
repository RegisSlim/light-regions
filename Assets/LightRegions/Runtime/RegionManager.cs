using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace LightRegions
{
    public enum RegionManagerBoundsMode { Box, Sphere, Custom }
    public class RegionManager : MonoBehaviour
    {

        public static RegionManager ActiveRegionManager;

        public bool occlusionActive = true;
        public bool influenceActive = true;
        public bool bakeOcclusion = true;
        public bool bakeInfluence = true;
        public Region activeRegion;
        public List<Region> regions = new List<Region>();
        public LayerMask lightOcclusionLayerMask;
        public List<BoxCollider> colliders;

        //Layers
        public int lightInfluenceLayer;
        public int regionLayer;
        public LayerMask lightInfluenceMaskInclude;
        public LayerMask lightInfluenceMaskExclude;
        public LayerMask regionMaskInclude;
        public LayerMask regionMaskExclude;

        public bool inspector_regatherObjects = true;
        public bool inspector_bakeOcclusion = true;
        public bool inspector_bakeInfluence = true;

        public RegionManagerBoundsMode boundsMode;
        public MeshCollider customBounds;
        public BoxCollider boxCollider;
        public SphereCollider sphereCollider;
        public LightRegionsSettings settings;

        public bool CheckBounds(Vector3 point)
        {
            switch (boundsMode)
            {
                case RegionManagerBoundsMode.Box:
                    if (boxCollider.bounds.Contains(point))
                    {
                        return true;
                    }
                    return false;
                case RegionManagerBoundsMode.Sphere:
                    if (Vector3.Distance(point, transform.position) < sphereCollider.radius)
                    {
                        return true;
                    }
                    return false;
                case RegionManagerBoundsMode.Custom:
                    if(customBounds.ClosestPoint(point) == point)
                    {
                        return true;
                    }
                    return false;
            }
            return false;
        }
        public void BuildMasks()
        {
            regionLayer = settings.regionLayer;
            lightInfluenceLayer = settings.lightInfluenceLayer;
            regionMaskInclude = 1 << settings.regionLayer;
            regionMaskExclude = ~(1 << settings.regionLayer);
            lightInfluenceMaskInclude = 1 << settings.lightInfluenceLayer;
            lightInfluenceMaskExclude = ~(1 << settings.lightInfluenceLayer);
            gameObject.layer = regionLayer;
        }
        private void Awake()
        {
            if (!LightRegionsUtility._initialized)
            {
                LightRegionsUtility.Init();
            }
            LightRegionsUtility.GetRegionManagers();
            for (int i = 0; i < regions.Count; i++)
            {
                regions[i].regionID = i;
            }
            DeactivateAll();
        }
        private void Start()
        {
            DeactivateAll();
        }
        private void OnTriggerEnter(Collider other)
        {
            RegionUpdater rU = other.GetComponent<RegionUpdater>();
            if(rU != null)
            {
                rU.regionManager = this;
                if (!rU.isMainUpdater)
                {
                    LightRegionsUtility.OnManagerEnter.Invoke(new RegionEventData(rU.eventReturnGameObject, rU.region, this));
                    return;
                }
                ActiveRegionManager = this;
                LightRegionsUtility.activeRegionManager = this;
                LightRegionsUtility.OnManagerEnter.Invoke(new RegionEventData(rU.eventReturnGameObject, activeRegion, this));
            }
        }
        private void OnTriggerExit(Collider other)
        {
            RegionUpdater rU = other.GetComponent<RegionUpdater>();
            if (rU != null)
            {
                LightRegionsUtility.OnManagerExit.Invoke(new RegionEventData(rU.eventReturnGameObject, activeRegion, this));
                if (!rU.isMainUpdater)
                {
                    return;
                }
                if(ActiveRegionManager == this)
                {
                    ActiveRegionManager = null;
                    LightRegionsUtility.activeRegionManager = null;
                }
                DeactivateAll();
            }
        }
        public int GetRegionIDFromPos(Vector3 point)
        {
            for (int c = 0; c < colliders.Count; c++)
            {
                if (colliders[c].bounds.Contains(point))
                {
                    return colliders[c].gameObject.GetComponent<Region>().regionID;
                }
            }
            return -1;
        }
        public Region GetRegionFromPos(Vector3 point)
        {
            for (int c = 0; c < colliders.Count; c++)
            {
                if (colliders[c].bounds.Contains(point))
                {
                    return colliders[c].gameObject.GetComponent<Region>();
                }
            }
            return null;
        }
        public Region GetRegion(int regionID)
        {
            if (regionID >= 0 && regionID < regions.Count)
            {
                return regions[regionID];
            }
            else
            {
                Debug.LogError("RegionID: " + regionID + " | Trying to retrieve a region that either doesn't exist or isn't attached to a region manager.");
                return null;
            }
        }
        public void SetupBoundingMesh()
        {
            switch (boundsMode)
            {
                case RegionManagerBoundsMode.Box:
                    boxCollider = GetComponent<BoxCollider>();
                    if(boxCollider == null)
                    {
                        boxCollider = gameObject.AddComponent<BoxCollider>();
                    }
                    boxCollider.includeLayers = regionMaskInclude;
                    boxCollider.excludeLayers = regionMaskExclude;
                    boxCollider.isTrigger = true;
                    break;
                case RegionManagerBoundsMode.Sphere:
                    sphereCollider = GetComponent<SphereCollider>();
                    if(sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                    }
                    sphereCollider.includeLayers = regionMaskInclude;
                    sphereCollider.excludeLayers = regionMaskExclude;
                    sphereCollider.isTrigger = true;
                    break;
                case RegionManagerBoundsMode.Custom:
                    customBounds = GetComponent<MeshCollider>();
                    if(customBounds == null)
                    {
                        customBounds = gameObject.AddComponent<MeshCollider>();
                    }
                    customBounds.includeLayers = regionMaskInclude;
                    customBounds.excludeLayers = regionMaskExclude;
                    customBounds.convex = true;
                    customBounds.isTrigger = true;
                    break;
            }
        }
        public int inspector_lightCount;
        public void GatherRegionObjects()
        {
            inspector_lightCount = 0;
            foreach(Region r in regions)
            {
                r.regionGameObjects = new List<RegionManagedObject>();
                r.regionLights = new List<RegionManagedObject>();
            }
            RegionManagedObject[] objects = FindObjectsByType<RegionManagedObject>(FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].regionManager = this;
                int r = GetRegionIDFromPos(objects[i].GetOffsetPoint());
                if (r != -1)
                {
                    objects[i].region = regions[r];
                    if (objects[i].objectType == RegionManagedObjectType.GameObject)
                    {
                        regions[r].regionGameObjects.Add(objects[i]);

                    }
                    if (objects[i].objectType == RegionManagedObjectType.Light)
                    {
                        inspector_lightCount++;
                        if (objects[i].lightComponent == null)
                        {
                            objects[i].lightComponent = objects[i].GetComponent<Light>();
                            if (objects[i] == null)
                            {
                                Debug.LogWarning("There is no specified light component on " + objects[i].name + ". Either specify a light or add a light to the same gameobject as the component.");
                            }
                        }
                        if (bakeInfluence)
                        {
                            LightRegionCompatibilityMethods.SetupLight(objects[i].lightComponent);
                            if (objects[i].influenceMesh != null)
                            {
                                DestroyImmediate(objects[i].influenceMesh.gameObject);
                            }
                            GameObject lightInfluence = new GameObject("Light Influence Mesh");
                            lightInfluence.layer = lightInfluenceLayer;
                            lightInfluence.transform.parent = objects[i].transform;
                            lightInfluence.transform.localPosition = Vector3.zero;
                            lightInfluence.transform.localRotation = Quaternion.identity;
                            objects[i].influenceMesh = lightInfluence.AddComponent<LightInfluenceMesher>();

                            MeshCollider collider = lightInfluence.AddComponent<MeshCollider>();
                            objects[i].influenceMesh.GetComponent<LightInfluenceMesher>().meshCollider = collider;
                            collider.includeLayers = lightInfluenceMaskInclude;
                            collider.excludeLayers = lightInfluenceMaskExclude;
                        }
                        regions[r].regionLights.Add(objects[i]);
                    }
                }
            }
        }
        public bool ValidateRegionBounds()
        {
            foreach (Region r in regions)
            {
                Vector3[] corners = r.GetCorners();
                bool outOfBounds = false;
                foreach (Vector3 c in corners)
                {
                    if (!CheckBounds(c))
                    {
                        outOfBounds = true;
                        break;
                    }
                }
                if (outOfBounds)
                {
                    Debug.LogError("Region (" + r.name + ") is out of bounds. Either expand the manager's bounds or shrink the region.");
                    return false;
                }
            }
            return true;
        }
        public void GatherRegions()
        {
            Region[] rArry = FindObjectsByType<Region>(FindObjectsSortMode.None);
            regions.Clear();
            foreach (Region r in rArry)
            {
                if (CheckBounds(r.transform.position))
                {
                    regions.Add(r);
                }
            }
            colliders = new List<BoxCollider>();

            for (int i = 0; i < regions.Count; i++)
            {
                Region r = regions[i];
                r.gameObject.layer = regionLayer;
                r.regionID = i;
                r.regionManager = this;
                if (settings.reparentRegions)
                {
                    r.transform.parent = transform;
                }
                BoxCollider[] bc = r.GetComponents<BoxCollider>();
                r.colliders = bc;
                foreach (BoxCollider bc2 in bc)
                {
                    colliders.Add(bc2);
                    bc2.includeLayers = regionMaskInclude;
                    bc2.excludeLayers = regionMaskExclude;
                    bc2.isTrigger = true;
                }
            }
        }
        public void DeactivateAll()
        {
            foreach (Region z in regions)
            {
                z.SetRegionState(false);
            }
        }
        private List<int> currentRegions = new List<int>();
        public void SetActiveRegion(int regionID)
        {
            if(regionID >= regions.Count)
            {
                Debug.LogError("Region ID " + regionID + " is out of bounds.");
            }
            LightRegionsUtility.activeRegion = regions[regionID];
            activeRegion = regions[regionID];
            for (int i = 0; i < activeRegion.connectedRegions.Count; i++)
            {
                if (!currentRegions.Contains(activeRegion.connectedRegions[i].regionID))
                {
                    activeRegion.connectedRegions[i].SetRegionState(true);
                    currentRegions.Add(activeRegion.connectedRegions[i].regionID);
                }

            }
            if (!currentRegions.Contains(activeRegion.regionID))
            {
                activeRegion.SetRegionState(true);
                currentRegions.Add(activeRegion.regionID);
            }
            for (int i = 0; i < currentRegions.Count; i++)
            {
                if (!activeRegion.IsConnectedTo(currentRegions[i]))
                {
                    regions[currentRegions[i]].SetRegionState(false);
                    currentRegions[i] = -1;
                }
            }
            currentRegions.RemoveAll(x => x == -1);
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0.2f, 0.2f, 0.1f);
            switch (boundsMode)
            {
                case RegionManagerBoundsMode.Box:
                    if (boxCollider == null)
                    {
                        boxCollider = GetComponent<BoxCollider>();
                        SetupBoundingMesh();
                    }
                    Gizmos.DrawWireCube(boxCollider.center + transform.position, boxCollider.size);
                    break;
                case RegionManagerBoundsMode.Sphere:
                    if (sphereCollider == null)
                    {
                        sphereCollider = GetComponent<SphereCollider>();
                        SetupBoundingMesh();
                    }
                    Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
                    break;
                case RegionManagerBoundsMode.Custom:
                    if (customBounds == null)
                    {
                        customBounds = GetComponent<MeshCollider>();
                        SetupBoundingMesh();
                    }
                    Gizmos.DrawWireMesh(customBounds.sharedMesh, 0, transform.position, transform.rotation);
                    break;
            }
        }
        [MenuItem("GameObject/Light Regions/Region Manager", false, 10)]
        static void CreateRegionManager(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Region Manager");
            RegionManager r = go.AddComponent<RegionManager>();
            go.layer = r.settings.regionLayer;
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        [MenuItem("GameObject/Light Regions/Region", false, 10)]
        static void CreateRegion(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Region");
            Region  r = go.AddComponent<Region>();
            r.connectedRegions = new List<Region>();
            r.colliders = new BoxCollider[0];
            r.negativeOverrides = new List<Region>();
            r.positiveOverrides = new List<Region>();
            r.regionGameObjects = new List<RegionManagedObject>();
            r.regionLights = new List<RegionManagedObject>();
            go.AddComponent<BoxCollider>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        [MenuItem("GameObject/Light Regions/Region Updater", false, 10)]
        static void CreateRegionUpdater(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Region Updater");
            go.AddComponent<RegionUpdater>();
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.radius = 0.25f;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
    // Events
    [System.Serializable]
    public class ReadOnlyAttribute : PropertyAttribute
    {

    }
}