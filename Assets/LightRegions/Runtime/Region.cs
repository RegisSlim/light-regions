using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LightRegions
{
    public class Region : MonoBehaviour
    {
        public LightRegionsSettings settings;
        [HideInInspector] public RegionManager regionManager;
        public List<Region> connectedRegions;
        public List<Region> positiveOverrides;
        public List<Region> negativeOverrides;
        public List<RegionManagedObject> regionGameObjects;
        public List<RegionManagedObject> regionLights;
        [HideInInspector] public BoxCollider[] colliders;
        [HideInInspector] public int regionID;
        [HideInInspector] public bool regionActive = true;
        public bool isBlocked = false;

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        //Connect the region by it's ID and ensure it isn't already connected. If added, ensure that this region is added to the target.
        public void Connect(int targetRegionID)
        {
            if (!IsConnectedTo(targetRegionID))
            {
                connectedRegions.Add(regionManager.regions[targetRegionID]);
                regionManager.regions[targetRegionID].Connect(regionID);
            }
        }
        public void Disconnect(int targetRegionID)
        {
            for(int i = 0; i < connectedRegions.Count; i++)
            {
                if (connectedRegions[i].regionID == targetRegionID)
                {
                    connectedRegions.RemoveAt(i);
                    regionManager.regions[targetRegionID].Disconnect(regionID);
                }
            }
        }
        //Check if the region is connected to the target region ID.
        public bool IsConnectedTo(int regionID)
        {
            if (regionID == this.regionID)
            {
                return true;
            }
            for (int i = 0; i < connectedRegions.Count; i++)
            {
                if (connectedRegions[i].regionID == regionID)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsConnectedTo(Region targetRegion)
        {
            if (regionID == targetRegion.regionID)
            {
                return true;
            }
            for (int i = 0; i < connectedRegions.Count; i++)
            {
                if (connectedRegions[i].regionID == targetRegion.regionID)
                {
                    return true;
                }
            }
            return false;
        }
        public void AddPositiveOverride(Region region)
        {
            for(int i = 0; i < positiveOverrides.Count; i++)
            {
                if (positiveOverrides[i].regionID == region.regionID)
                {
                    return;
                }
            }
            positiveOverrides.Add(region);
            region.AddPositiveOverride(this);
            if (!IsConnectedTo(region.regionID))
            {
                region.Connect(regionID);
            }
        }
        public void RemovePositiveOverride(Region region)
        {
            for (int i = 0; i < positiveOverrides.Count; i++)
            {
                if (positiveOverrides[i].regionID == region.regionID)
                {
                    positiveOverrides.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < region.positiveOverrides.Count; i++)
            {
                if (region.positiveOverrides[i].regionID == regionID)
                {
                    region.positiveOverrides.RemoveAt(i);
                    break;
                }
            }
        }
        public void AddNegativeOverride(Region region)
        {
            for (int i = 0; i < negativeOverrides.Count; i++)
            {
                if (negativeOverrides[i].regionID == region.regionID)
                {
                    return;
                }
            }
            Disconnect(region.regionID);
            negativeOverrides.Add(region);
            region.AddNegativeOverride(this);
        }
        public void RemoveNegativeOverride(Region region)
        {
            for (int i = 0; i < negativeOverrides.Count; i++)
            {
                if (negativeOverrides[i].regionID == region.regionID)
                {
                    negativeOverrides.RemoveAt(i);
                    return;
                }
            }
            for (int i = 0; i < region.negativeOverrides.Count; i++)
            {
                if (region.negativeOverrides[i].regionID == regionID)
                {
                    region.negativeOverrides.RemoveAt(i);
                    break;
                }
            }
        }
        //Set the state of the region and all connected regions.
        public void SetRegionState(bool state)
        {
            if (state)
            {
                regionActive = true;
                for (int i = 0; i < regionGameObjects.Count; i++)
                {
                    regionGameObjects[i].gameObject.SetActive(true);
                    regionGameObjects[i].isActive = true;
                }
                for (int i = 0; i < regionLights.Count; i++)
                {
                    regionLights[i].gameObject.SetActive(true);
                    regionLights[i].isActive = true;
                    //regionLights[i].lightComponent.GetComponent<HDAdditionalLightData>().RequestShadowMapRendering();
                }
            }
            else
            {
                regionActive = false;
                for (int i = 0; i < regionGameObjects.Count; i++)
                {
                    regionGameObjects[i].isActive = false;
                    regionGameObjects[i].gameObject.SetActive(false);
                }
                for (int i = 0; i < regionLights.Count; i++)
                {
                    regionLights[i].gameObject.SetActive(false);
                    regionLights[i].isActive = false;
                }
            }
        }
        public Vector3[] GetCorners()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;
            Vector3[] localCorners = new Vector3[8];
            localCorners[0] = center + new Vector3(-size.x / 2, -size.y / 2, -size.z / 2);
            localCorners[1] = center + new Vector3(size.x / 2, -size.y / 2, -size.z / 2);
            localCorners[2] = center + new Vector3(size.x / 2, -size.y / 2, size.z / 2);
            localCorners[3] = center + new Vector3(-size.x / 2, -size.y / 2, size.z / 2);
            localCorners[4] = center + new Vector3(-size.x / 2, size.y / 2, -size.z / 2);
            localCorners[5] = center + new Vector3(size.x / 2, size.y / 2, -size.z / 2);
            localCorners[6] = center + new Vector3(size.x / 2, size.y / 2, size.z / 2);
            localCorners[7] = center + new Vector3(-size.x / 2, size.y / 2, size.z / 2);
            Vector3[] worldCorners = new Vector3[8];
            for (int i = 0; i < localCorners.Length; i++)
            {
                worldCorners[i] = boxCollider.transform.TransformPoint(localCorners[i]);
            }

            return worldCorners;
        }
        public void GatherColliders()
        {
            colliders = gameObject.GetComponents<BoxCollider>();
        }
        //Detect the LightRegionUpdater on the player gameobject.
        private RegionUpdater regionUpdater;
        private void OnTriggerEnter(Collider other)
        {

            if (regionManager.occlusionActive)
            {
                regionUpdater = other.GetComponent<RegionUpdater>();
                if (regionUpdater != null)
                {
                    regionUpdater.region = this;
                    if (regionUpdater.isMainUpdater)
                    {
                        regionManager.SetActiveRegion(regionID);
                    }
                    LightRegionsUtility.OnRegionEntered.Invoke(new RegionEventData(regionUpdater.eventReturnGameObject, this, regionManager));
                }
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (regionManager.occlusionActive)
            {
                regionUpdater = other.GetComponent<RegionUpdater>();
                if (regionUpdater != null)
                {
                    if (regionManager.activeRegion.regionID == regionID && regionUpdater.isMainUpdater)
                    {
                        Collider[] col = Physics.OverlapSphere(regionUpdater.transform.position, regionUpdater.GetComponent<SphereCollider>().radius, regionManager.regionMaskInclude);
                        if(col != null && col.Length > 0)
                        {
                            for(int i = 0; i < col.Length; i++)
                            {
                                Region r = col[i].gameObject.GetComponent<Region>();
                                if (r != null)
                                {
                                    regionManager.SetActiveRegion(col[0].GetComponent<Region>().regionID);
                                    break;
                                }
                            }
                        }
                    }
                    LightRegionsUtility.OnRegionExit.Invoke(new RegionEventData(regionUpdater.eventReturnGameObject, this, regionManager));
                }
            }
        }
        //Inspector tool options
        public bool showSliceTool;
        public int xSlices = 1;
        public int ySlices = 1;
        public int zSlices = 1;
        public void Slice()
        {
            Undo.RecordObject(this, "Undo slice operation");
            int undoID = Undo.GetCurrentGroup();
            BoxCollider originalCollider = GetComponent<BoxCollider>();
            Vector3 originalSize = originalCollider.size;
            Vector3 originalCenter = originalCollider.center;

            Vector3 sliceSize = new Vector3(
                originalSize.x / xSlices,
                originalSize.y / ySlices,
                originalSize.z / zSlices
            );
            for (int x = 0; x < xSlices; x++)
            {
                for (int y = 0; y < ySlices; y++)
                {
                    for (int z = 0; z < zSlices; z++)
                    {
                        GameObject slice = new GameObject(name + " " + x + ", " + y + ", " + z);
                        Undo.RegisterCreatedObjectUndo(slice, "Undo slice");
                        Undo.CollapseUndoOperations(undoID);
                        BoxCollider bc = Undo.AddComponent<BoxCollider>(slice);
                        Region r = Undo.AddComponent<Region>(slice);
                        r.connectedRegions = new List<Region>();
                        r.regionManager = regionManager;
                        r.colliders = new BoxCollider[] { bc };
                        r.regionGameObjects = new List<RegionManagedObject>();
                        r.regionLights = new List<RegionManagedObject>();
                        r.positiveOverrides = new List<Region>();
                        r.negativeOverrides = new List<Region>();
                        slice.layer = gameObject.layer;
                        bc.isTrigger = true;
                        bc.includeLayers = regionManager.regionMaskInclude;
                        bc.excludeLayers = regionManager.regionMaskExclude;
                        slice.transform.parent = transform.parent;
                        slice.transform.position = transform.position + new Vector3(
                            (x - xSlices / 2.0f + 0.5f) * sliceSize.x,
                            (y - ySlices / 2.0f + 0.5f) * sliceSize.y,
                            (z - zSlices / 2.0f + 0.5f) * sliceSize.z
                        );
                        bc.size = sliceSize;

                        BoxCollider sliceCollider = slice.GetComponent<BoxCollider>();
                        sliceCollider.size = sliceSize;
                        sliceCollider.center = Vector3.zero;
                        Selection.activeTransform = slice.transform;
                    }
                }
            }
            Undo.DestroyObjectImmediate(gameObject);
            Undo.CollapseUndoOperations(undoID);
            regionManager.GatherRegionObjects();
        }
    }
    public enum ShowRegionMode { All = 0, Connected = 1, Near = 2, None = 3 }
}
