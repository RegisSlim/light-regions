using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LightRegions
{
    public static class LightRegionsUtility
    {
        public static RegionManager activeRegionManager;
        public static Region activeRegion;
        public static RegionEvent OnRegionEntered;
        public static RegionEvent OnRegionExit;
        public static RegionEvent OnManagerEnter;
        public static RegionEvent OnManagerExit;
        public static List<RegionManager> allRegionManagers;
        public static bool _initialized = false;

        public static void Init()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            allRegionManagers = new List<RegionManager>();
            if(OnRegionEntered == null)
            {
                OnRegionEntered = new RegionEvent();
            }
            if(OnRegionExit == null)
            {
                OnRegionExit = new RegionEvent();
            }
            if(OnManagerEnter == null)
            {
                OnManagerEnter = new RegionEvent();
            }
            if(OnManagerExit == null)
            {
                OnManagerExit = new RegionEvent();
            }
        }
        public static Region GetRegionAtPoint(Vector3 point)
        {
            RegionManager rm = GetManagerAtPoint(point);
            Region r = null;
            if(rm != null)
            {
                r = rm.GetRegionFromPos(point);
            }
            if(r != null)
            {
                return r;
            }
            return null;
        }
        public static RegionManager GetManagerAtPoint(Vector3 point)
        {
            if (!_initialized)
            {
                Init();
            }
            RegionManager[] managers = GameObject.FindObjectsByType<RegionManager>(FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].CheckBounds(point))
                {
                    return managers[i];
                }
            }
            return null;
        }
        public static void InitializeAtPoint(Vector3 point)
        {
            if (!_initialized)
            {
                Init();
            }
            RegionManager manager = GetManagerAtPoint(point);
            if (manager != null)
            {
                activeRegionManager = manager;
                manager.DeactivateAll();
                manager.SetActiveRegion(manager.GetRegionFromPos(point).regionID);
            }
            else
            {
                Debug.LogError("No region manager could be found at " + point);
            }
        }
        public static bool IsPointInsideRegion(Region region, Vector3 point)
        {
            if (region.GetComponent<BoxCollider>().bounds.Contains(point))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void SetActiveRegion(Region region)
        {
            region.regionManager.SetActiveRegion(region.regionID);
        }
        public static void DeactivateRegionManager(RegionManager regionManager)
        {
            regionManager.DeactivateAll();
        }
        public static void GetRegionManagers()
        {
            if(allRegionManagers == null)
            {
                allRegionManagers = new List<RegionManager>();
            }
            allRegionManagers.Clear();
            RegionManager[] regions = GameObject.FindObjectsOfType<RegionManager>();
            allRegionManagers.AddRange(regions);
        }
    }
    public class RegionEventData
    {
        public GameObject gameObject;
        public Region region;
        public RegionManager regionManager;

        public RegionEventData(GameObject gameObject, Region region, RegionManager regionManager)
        {
            this.gameObject = gameObject;
            this.region = region;
            this.regionManager = regionManager;
        }
    }
    public class RegionEvent : UnityEvent<RegionEventData> { }
}
