using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace LightRegions.EditorExtensions
{
    [CustomEditor(typeof(RegionManager))]
    public class RegionManagerEditor : Editor
    {
        SerializedProperty bakeOcclusion;
        SerializedProperty bakeInfluence;
        SerializedProperty boundsMode;


        private void OnEnable()
        {
            RegionManager regionManager = (RegionManager)target;
            bakeOcclusion = serializedObject.FindProperty("bakeOcclusion");
            bakeInfluence = serializedObject.FindProperty("bakeInfluence");
            boundsMode = serializedObject.FindProperty("boundsMode");
            regionManager.BuildMasks();
            LightRegionCompatibilityMethods.RefreshLights(regionManager);
            lastBoundsMode = boundsMode.enumValueIndex;
        }
        private int lastBoundsMode;
        public override void OnInspectorGUI()
        {

            RegionManager regionManager = (RegionManager)target;

            LayerMask tempMask = EditorGUILayout.MaskField("Bake LayerMask: ", regionManager.lightOcclusionLayerMask, InternalEditorUtility.layers);
            if (tempMask != regionManager.lightOcclusionLayerMask)
            {
                EditorUtility.SetDirty(regionManager);
            }
            regionManager.lightOcclusionLayerMask = tempMask;
            bakeOcclusion.boolValue = EditorGUILayout.Toggle("Bake occlusion:", bakeOcclusion.boolValue);

            EditorGUILayout.Space();

            bakeInfluence.boolValue = EditorGUILayout.Toggle("Bake influence:", bakeInfluence.boolValue);

            EditorGUILayout.PropertyField(boundsMode, new GUIContent("Bounds Mode: "));
            if (boundsMode.enumValueIndex != lastBoundsMode)
            {
                if (lastBoundsMode == 0)
                {
                    DestroyImmediate(regionManager.GetComponent<BoxCollider>());
                }
                else if (lastBoundsMode == 1)
                {
                    DestroyImmediate(regionManager.GetComponent<SphereCollider>());
                }
                else if (lastBoundsMode == 2)
                {
                    DestroyImmediate(regionManager.GetComponent<MeshCollider>());
                }
                if (boundsMode.enumValueIndex == 0)
                {
                    regionManager.boundsMode = RegionManagerBoundsMode.Box;
                    regionManager.AddComponent<BoxCollider>();
                    regionManager.SetupBoundingMesh();
                }
                else if(boundsMode.enumValueIndex == 1)
                {
                    regionManager.boundsMode = RegionManagerBoundsMode.Sphere;
                    regionManager.AddComponent<SphereCollider>();
                    regionManager.SetupBoundingMesh();
                }
                else if(boundsMode.enumValueIndex == 2)
                {
                    regionManager.boundsMode = RegionManagerBoundsMode.Custom;
                    regionManager.AddComponent<MeshCollider>();
                    regionManager.SetupBoundingMesh();
                }
                lastBoundsMode = boundsMode.enumValueIndex;
                EditorUtility.SetDirty(regionManager);
                EditorGUIUtility.ExitGUI();
            }
            if (boundsMode.enumValueIndex == 2)
            {
                EditorGUILayout.LabelField("Note: Custom bounds mesh must be convex.");
            }
            ApplyOverrides(regionManager);
            EditorGUILayout.LabelField(new GUIContent(regionManager.regions.Count + " regions. " + regionManager.inspector_lightCount + " lights."));

            if (GUILayout.Button("Bake"))
            {
                GatherRegions(regionManager);
                GatherRegionObjects(regionManager);
                if (!regionManager.ValidateRegionBounds())
                {
                    return;
                }
                if (bakeOcclusion.boolValue)
                {
                    EditorCoroutineUtility.StartCoroutine(BakeLightOcclusion(regionManager), this);
                }
                if (bakeInfluence.boolValue)
                {
                    BakeLightInfluence(regionManager);
                }
            }
            serializedObject.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(regionManager);
            if (EditorGUILayout.LinkButton("Documentation"))
            {
                Application.OpenURL("https://github.com/RegisSlim/LightRegionsRider/wiki/2:-Region-Manager");
            }
        }
        public void OnSceneGUI()
        {
            RegionManager regionManager = (RegionManager)target;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = Color.red;
            Matrix4x4 ogMatrix = Handles.matrix;
            if(regionManager.regions == null)
            {
                return;
            }
            foreach(Region region in regionManager.regions)
            {
                Handles.matrix = region.transform.localToWorldMatrix;
                Handles.zTest = CompareFunction.Always;
                Handles.DrawWireCube(region.colliders[0].center, region.colliders[0].size);
                Handles.matrix = ogMatrix;
            }
        }
        public void GatherRegions(RegionManager regionManager)
        {
            regionManager.GatherRegions();
            LightRegionCompatibilityMethods.RefreshLights(regionManager);
            EditorUtility.SetDirty(regionManager);
            foreach (Region r in regionManager.regions)
            {
                EditorUtility.SetDirty(r);
            }

        }
        public void GatherRegionObjects(RegionManager regionManager)
        {
            regionManager.GatherRegionObjects();
            LightRegionCompatibilityMethods.RefreshLights(regionManager);
            foreach (Region r in regionManager.regions)
            {
                EditorUtility.SetDirty(r);
            }
            EditorUtility.SetDirty(regionManager);
        }
        public void BakeLightInfluence(RegionManager regionManager)
        {
            foreach (Region r in regionManager.regions)
            {
                foreach (RegionManagedObject o in r.regionLights)
                {
                    o.influenceMesh.GetComponent<LightInfluenceMesher>().Generate(o);
                    EditorUtility.SetDirty(o);
                    EditorUtility.SetDirty(o.influenceMesh.gameObject);
                }
            }
        }
        public bool isBaking = false;
        public IEnumerator BakeLightOcclusion(RegionManager regionManager)
        {
            isBaking = true;
            int lightTotal = 0;
            int lightCurrent = 0;
            foreach (Region r in regionManager.regions)
            {
                r.connectedRegions.Clear();
                foreach (RegionManagedObject obj in r.regionLights)
                {
                    lightTotal++;
                }
            }
            for (int i = 0; i < regionManager.regions.Count; i++)
            {
                for (int l = 0; l < regionManager.regions[i].regionLights.Count; l++)
                {
                    Vector3[] rayVectors = DistributePointsOnSphere(regionManager.settings.raysPerLight, 1f);
                    Region originRegion = regionManager.regions[i];
                    RegionManagedObject obj = originRegion.regionLights[l];
                    lightCurrent++;
                    EditorUtility.DisplayProgressBar("Baking Light Occlusion", "Baking light " + lightCurrent + "/" + lightTotal, (float)lightTotal / (float)lightCurrent);
                    if (obj.debugTrace)
                    {
                        debug = true;
                    }
                    else
                    {
                        debug = false;
                    }
                    foreach (Vector3 v in rayVectors)
                    {
                        int[] regions = Trace(new Ray(obj.GetOffsetPoint(), v), regionManager, regionManager.settings.bouncesPerRay);
                        for (int r = 0; r < regions.Length; r++)
                        {
                            if (r > 0)
                            {
                                regionManager.regions[regions[r]].Connect(regions[r - 1]);
                            }
                            originRegion.Connect(regions[r]);
                        }
                    }
                    yield return null;
                }
            }
            if (regionManager.settings.testAllRegions)
            {
                int regionCount = 0;
                for(int i = 0; i < regionManager.regions.Count; i++)
                {
                    regionCount++;
                    Vector3[] rayVectors = DistributePointsOnSphere(regionManager.settings.raysPerLight / 2, 1f);
                    Region originRegion = regionManager.regions[i];
                    EditorUtility.DisplayProgressBar("Baking Light Occlusion", "Baking region " + regionCount + "/" + regionManager.regions.Count, (float)regionCount / (float)regionManager.regions.Count);
                    foreach (Vector3 v in rayVectors)
                    {
                        int[] regions = Trace(new Ray(originRegion.transform.position,v), regionManager, regionManager.settings.bouncesPerRay);
                        for (int r = 0; r < regions.Length; r++)
                        {
                            if (r > 0)
                            {
                                regionManager.regions[regions[r]].Connect(regions[r - 1]);
                            }
                            originRegion.Connect(regions[r]);
                        }
                    }
                }
            }
            foreach (Region r in regionManager.regions)
            {
                EditorUtility.SetDirty(r);
            }
            EditorUtility.SetDirty(this);
            EditorUtility.ClearProgressBar();
            isBaking = false;
        }
        public void ApplyOverrides(RegionManager regionManager)
        {
            if(regionManager.regions != null)
            {
                regionManager.regions.RemoveAll(item => item == null);
                foreach (Region r in regionManager.regions)
                {
                    r.positiveOverrides.RemoveAll(item => item == null);
                    r.negativeOverrides.RemoveAll(item => item == null);
                    r.connectedRegions.RemoveAll(item => item == null);
                    foreach (Region p in r.positiveOverrides)
                    {
                        if (!r.IsConnectedTo(p.regionID))
                        {
                            r.Connect(p.regionID);
                        }
                    }
                    foreach (Region n in r.negativeOverrides)
                    {
                        if (r.IsConnectedTo(n.regionID))
                        {
                            r.Disconnect(n.regionID);
                        }
                    }
                }
            }
        }

        public Vector3 lastPoint;
        public Vector3 reflectV;
        private bool debug = false;

        //Trace function that returns an array of region ids that the ray passed through
        public int[] Trace(Ray ray, RegionManager regionManager, int bounces)
        {
            RaycastHit hit;
            List<int> regionIDs = new List<int>();
            Ray newRay = ray;
            for (int bounce = 0; bounce < bounces; bounce++)
            {
                Vector3[] points;
                if (Physics.Raycast(newRay, out hit, 100, regionManager.lightOcclusionLayerMask))
                {
                    points = GetTracePoints(newRay.origin, hit.point);
                    if (debug)
                    {
                        Debug.DrawLine(newRay.origin, hit.point, new Color(0, 1, 0, 0.1f), 15f);
                    }
                    newRay = new Ray(hit.point + (hit.normal * 0.01f), Vector3.Reflect(newRay.direction, SimulateRoughness(hit.normal, regionManager.settings.rayDiffusion)));
                }
                else 
                {
                    if (debug)
                    {
                        Debug.DrawLine(newRay.origin, newRay.direction * regionManager.settings.maxRayLength, new Color(0, 1, 0, 0.1f), 15f);
                    }
                    points = GetTracePoints(newRay.origin, newRay.direction * regionManager.settings.maxRayLength);
                    regionIDs.AddRange(PointsToRegionIDs(points, regionManager));
                    break;
                }
                regionIDs.AddRange(PointsToRegionIDs(points, regionManager));
            }
            return SimplifyRegionList(regionIDs);
        }
        public int[] PointsToRegionIDs(Vector3[] points, RegionManager regionManager)
        {
            int hitZone;
            List<int> regionIDs = new List<int>();
            for (int i = 0; i < points.Length; i++)
            {
                hitZone = regionManager.GetRegionIDFromPos(points[i]);
                if (hitZone != -1)
                {
                    if (!regionIDs.Contains(hitZone))
                    {
                        regionIDs.Add(hitZone);
                    }
                }
            }
            return regionIDs.ToArray();
        }
        public int[] SimplifyRegionList(List<int> regionIDs)
        {
            List<int> newList = new List<int>();
            for(int i = 0; i < regionIDs.Count; i++)
            {
                if (!newList.Contains(regionIDs[i]))
                {
                    newList.Add(regionIDs[i]);
                }
            }
            return newList.ToArray();
        }
        public Vector3[] GetTracePoints(Vector3 A, Vector3 B)
        {
            float dist = Vector3.Distance(A, B);
            float checkIncrement = 1f;
            int pointCount = (int)Mathf.Ceil(dist / checkIncrement);
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Vector3.Lerp(A, B, (float)i / (float)pointCount);
            }
            return points;
        }
        public Vector3 SimulateRoughness(Vector3 originalDirection, float roughnessDiffusionAngle)
        {
            originalDirection.Normalize();
            Vector3 randomPoint = Random.onUnitSphere;
            Vector3 rotationAxis = Vector3.Cross(originalDirection, randomPoint);
            rotationAxis.Normalize();
            float rotationAngle = Random.Range(0f, roughnessDiffusionAngle);
            Quaternion rotation = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            Vector3 newDirection = rotation * originalDirection;

            return newDirection.normalized;
        }
        //Get an array of points along the surface of a sphere
        public Vector3[] DistributePointsOnSphere(int numPoints, float radius)
        {
            Vector3[] points = new Vector3[numPoints];

            // Golden ratio
            float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;

            for (int i = 0; i < numPoints; i++)
            {
                float t = i / (float)numPoints;
                float inclination = Mathf.Acos(1 - 2 * t);
                float azimuth = 2 * Mathf.PI * goldenRatio * i;

                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);

                points[i] = new Vector3(x, y, z) * radius;
            }

            return points;
        }
    }

    [CustomEditor(typeof(LightInfluenceMesher))]
    public class LightInfluenceMesherEditor : Editor
    {
        private void OnEnable()
        {
            LightInfluenceMesher mesher = (LightInfluenceMesher)target;
            mesher.drawMeshGizmo = true;
        }
        private void OnDisable()
        {
            LightInfluenceMesher mesher = (LightInfluenceMesher)target;
            mesher.drawMeshGizmo = false;
        }
        public override void OnInspectorGUI()
        {
          
        }
    }

    [CustomEditor(typeof(Region))]
    public class RegionEditor : Editor
    {
        List<int> connectedIDs = new List<int>();
        float[] dist = new float[6];
        SerializedProperty xSlices;
        SerializedProperty ySlices;
        SerializedProperty zSlices;
        SerializedProperty showSliceTool;
        Vector3[] directions = new Vector3[]
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };
        Region thisRegion;
        private void OnEnable()
        {
            GlobalKeyEventHandler.OnKeyEvent += OnKeyPressed;
            Region region = (Region)target;
            showRegionMode = region.settings.currentShowRegionMode;
            thisRegion = region;
            if (region.regionManager == null)
            {
                RegionManager rm = FindFirstObjectByType<RegionManager>();
                if(rm != null)
                {
                    region.regionManager = rm;
                    rm.GatherRegions();
                    EditorUtility.SetDirty(rm);
                    foreach(Region r in rm.regions)
                    {
                        EditorUtility.SetDirty(r);
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                region.regionManager.GatherRegions();
            }
            showSliceTool = serializedObject.FindProperty("showSliceTool");
            xSlices = serializedObject.FindProperty("xSlices");
            ySlices = serializedObject.FindProperty("ySlices");
            zSlices = serializedObject.FindProperty("zSlices");
            region.gameObject.layer = region.settings.regionLayer;
            region.regionManager.BuildMasks();
            BoxCollider[] colliders = region.GetComponents<BoxCollider>();
            foreach(BoxCollider c in colliders)
            {
                c.includeLayers = region.regionManager.regionMaskInclude;
                c.excludeLayers = region.regionManager.regionMaskExclude;
            }
        }
        private void OnDisable()
        {
            Region region = (Region)target;
            region.settings.currentShowRegionMode = showRegionMode;
            GlobalKeyEventHandler.OnKeyEvent -= OnKeyPressed;
        }
        private bool altDown = false;
        private bool showOverrideView = false;
        private ShowRegionMode showRegionMode = ShowRegionMode.All;

        private void OnKeyPressed(Event e) 
        {
            if(e.type == EventType.KeyDown)
            {
                if(e.keyCode == KeyCode.LeftAlt)
                {
                    altDown = true;
                }
                if(altDown && e.keyCode == KeyCode.LeftShift)
                {
                    showOverrideView = !showOverrideView;
                }
                if(e.keyCode == KeyCode.V)
                {
                    int i = (int)showRegionMode;
                    i++;
                    if(i > 3)
                    {
                        i = 0;
                    }
                    showRegionMode = (ShowRegionMode)i;
                    Repaint();
                }
            }
            if(e.type == EventType.KeyUp)
            {
                if(e.keyCode == KeyCode.LeftAlt)
                {
                    altDown = false;
                }
            }
            if(e.type == EventType.MouseDown)
            {
                if(altDown)
                {
                    FitByNormal(thisRegion);
                }
            }
        }
        public bool showInfo = false;
        BoxCollider bc;
        LayerMask mask;
        public override void OnInspectorGUI()
        {
            Region region = (Region)target;
            bc = region.GetComponent<BoxCollider>();
            if (GUILayout.Button("Fit"))
            {
                RaycastHit hit;
                for (int i = 0; i < 6; i++)
                {
                    if (Physics.Raycast(region.transform.position, region.transform.TransformDirection(directions[i]), out hit, region.settings.regionFitToolRange, region.settings.regionFitLayerMask))
                    {
                        dist[i] = hit.distance;
                        if (i == 2 || i == 3)
                        {
                            dist[i] += region.settings.regionFitToolVerticalThickness;
                        }
                        else
                        {
                            dist[i] += region.settings.regionFitToolHorizontalThickness;
                        }

                    }
                    else
                    {
                        dist[i] = region.settings.regionFitToolRange;
                    }
                }

                Vector3 size;
                Vector3 offset;
                Handles.color = Color.green;
                size.x = dist[0] + dist[1];
                size.y = dist[2] + dist[3];
                size.z = dist[4] + dist[5];
                offset.x = (dist[0] - dist[1]) / 2;
                offset.y = (dist[2] - dist[3]) / 2;
                offset.z = (dist[4] - dist[5]) / 2;
                Undo.RecordObject(bc, "Box collider size change");
                bc.size = size;
                bc.center = offset;
            }
            GUILayout.Label("Fit to point: Alt + Left Click", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("Toggle override view: Alt + Left Shift", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("Cycle region view: V | Current view mode: " + showRegionMode, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            mask = EditorGUILayout.MaskField("Fit LayerMask: ", region.settings.regionFitLayerMask, InternalEditorUtility.layers);
            if(region.settings.regionFitLayerMask != mask)
            {
                EditorUtility.SetDirty(region.settings);
            }
            region.settings.regionFitLayerMask = mask;
            region.settings.regionFitToolRange = EditorGUILayout.FloatField("Fit Range", region.settings.regionFitToolRange);
            region.settings.regionFitToolHorizontalThickness = EditorGUILayout.FloatField("Fit Horizontal Thickness", region.settings.regionFitToolHorizontalThickness);
            region.settings.regionFitToolVerticalThickness = EditorGUILayout.FloatField("Fit Vertical Thickness", region.settings.regionFitToolVerticalThickness);
            showSliceTool.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(showSliceTool.boolValue, "Show slice tool?");
            if (showSliceTool.boolValue)
            {
                xSlices.intValue = EditorGUILayout.IntSlider("X Slices", xSlices.intValue, 1, 16);
                ySlices.intValue = EditorGUILayout.IntSlider("Y Slices", ySlices.intValue, 1, 16);
                zSlices.intValue = EditorGUILayout.IntSlider("Z Slices", zSlices.intValue, 1, 16);
                if (GUILayout.Button("Slice"))
                {
                    
                    region.Slice();
                    return;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            bool showRegionLabels = EditorGUILayout.Toggle("Show Region Labels?", region.settings.showRegionLabels);
            if(showRegionLabels != region.settings.showRegionLabels)
            {
                region.settings.showRegionLabels = showRegionLabels;
                EditorUtility.SetDirty(region.settings);
            }
            EditorGUILayout.Space();
            showInfo = EditorGUILayout.Toggle("Show debug info:", showInfo);
            if (showInfo)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("DO NOT EDIT THE VALUES BELOW THIS POINT");
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                base.OnInspectorGUI();
            }
            if(serializedObject != null)
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (EditorGUILayout.LinkButton("Documentation"))
            {
                Application.OpenURL("https://github.com/RegisSlim/LightRegionsRider/wiki/3:-Region");
            }
        }
        public enum RelationType { PosOverride, NegOverride, Connected, Disconnected }
        public RelationType GetRelationType(Region source, Region target)
        {
            for (int i = 0; i < source.positiveOverrides.Count; i++)
            {
                if (source.positiveOverrides[i].regionID == target.regionID)
                {
                    return RelationType.PosOverride;
                }
            }
            for (int i = 0; i < source.negativeOverrides.Count; i++)
            {
                if (source.negativeOverrides[i].regionID == target.regionID)
                {
                    return RelationType.NegOverride;
                }
            }
            if (connectedIDs.Contains(target.regionID))
            {
                return RelationType.Connected;

            }
            else
            {
                return RelationType.Disconnected;
            }
        }
        private Vector3 currentHitPos;
        private Vector3 currentHitNormal;
        private void FitByNormal(Region region)
        {
            BoxCollider boxCollider = region.GetComponent<BoxCollider>();
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;
            Vector3 localTarget = region.transform.InverseTransformPoint(currentHitPos);
            Vector3 localNormal = region.transform.InverseTransformDirection(currentHitNormal);
            float closestProd = 1;
            int closestIndex = 0;
            float product;
            float[] distances = new float[6];
            for (int i = 0; i < directions.Length; i++)
            {
                product = Vector3.Dot(localNormal, directions[i]);
                if (product < closestProd)
                {
                    closestIndex = i;
                    closestProd = product;
                }
            }

            // Modify and reconstruct bounds
            Vector3 half = size / 2;
            distances[1] = Mathf.Abs((half.x * -1) + center.x);
            distances[0] = half.x + center.x;
            distances[3] = Mathf.Abs((half.y * -1) + center.y);
            distances[2] = half.y + center.y;
            distances[5] = Mathf.Abs((half.z * -1) + center.z);
            distances[4] = half.z + center.z;
            if (directions[closestIndex] == Vector3.right){distances[0] = Mathf.Abs(localTarget.x);}
            else if (directions[closestIndex] == Vector3.left) { distances[1] = Mathf.Abs(localTarget.x); }
            else if (directions[closestIndex] == Vector3.up) { distances[2] = Mathf.Abs(localTarget.y); }
            else if (directions[closestIndex] == Vector3.down) { distances[3] = Mathf.Abs(localTarget.y); }
            else if (directions[closestIndex] == Vector3.forward) { distances[4] = Mathf.Abs(localTarget.z); }
            else if (directions[closestIndex] == Vector3.back) { distances[5] = Mathf.Abs(localTarget.z); }
            size.x = distances[0] + distances[1];
            size.y = distances[2] + distances[3];
            size.z = distances[4] + distances[5];
            center.x = (distances[0] - distances[1]) / 2;
            center.y = (distances[2] - distances[3]) / 2;
            center.z = (distances[4] - distances[5]) / 2;
            Undo.RegisterCompleteObjectUndo(boxCollider, "bc change");
            boxCollider.size = size;
            boxCollider.center = center;
            EditorUtility.SetDirty(region);
        }
        private void DrawSceneObjects(Region r)
        {
            Handles.matrix = r.transform.localToWorldMatrix;
            foreach (BoxCollider b in r.colliders)
            {

                if (b != null)
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    Handles.color = new Color(1f, 0, 1f, 1);
                    Handles.DrawWireCube(b.center, b.size);


                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    Handles.color = new Color(0.2f, 0, 0.2f, 1);
                    Handles.DrawWireCube(b.center, b.size);
                }
            }
        }
        public void OnSceneGUI()
        {
            Matrix4x4 matrix = Handles.matrix;
            Region region = (Region)target;
            BoxCollider collider = region.GetComponent<BoxCollider>();
            Camera sceneCam = SceneView.currentDrawingSceneView.camera;
            RaycastHit hit;
            if (altDown)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(ray, out hit, thisRegion.settings.regionFitToolRange, thisRegion.settings.regionFitLayerMask))
                {
                    currentHitPos = hit.point;
                    currentHitNormal = hit.normal;
                    Handles.zTest = CompareFunction.LessEqual;
                    Vector3 cVector = hit.normal;
                    cVector.x = Mathf.Abs(cVector.x);
                    cVector.y = Mathf.Abs(cVector.y);
                    cVector.z = Mathf.Abs(cVector.z);
                    Handles.color = new Color(cVector.x,cVector.y,cVector.z, 0.25f);
                    Handles.DrawLine(region.transform.position, hit.point);
                    Handles.DrawSolidDisc(hit.point + (hit.normal * 0.02f), hit.normal, 0.25f);
                }
            }

            if (bc != null)
            {
                Vector3 colliderCenterWorld = bc.transform.TransformPoint(bc.center);
                Vector3 offset = colliderCenterWorld - bc.transform.position;
                bc.transform.position += offset;
                bc.center = Vector3.zero;
            }
            if (region.regionManager != null)
            {
                Handles.color = Color.green;
                if(region.colliders == null || region.colliders.Length == 0)
                {
                    region.GatherColliders();
                }
                if (region.colliders.Length > 0)
                {
                    Handles.matrix = region.transform.localToWorldMatrix;
                    foreach (BoxCollider b in region.colliders)
                    {
                        if (b != null)
                        {
                            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                            Handles.color = Color.green;
                            Handles.DrawWireCube(b.center, b.size);

                            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                            Handles.color = new Color(0, 0.2f, 0);
                            Handles.DrawWireCube(b.center, b.size);
                        }
                    }
                    Handles.matrix = matrix;
                }
                Handles.color = Color.blue;
                connectedIDs.Clear();
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                if(region.connectedRegions == null)
                {
                    region.connectedRegions = new List<Region>();
                }
                foreach (Region r in region.connectedRegions)
                {
                    if(r == null)
                    {
                        return;
                    }
                    connectedIDs.Add(r.regionID);
                    Handles.color = new Color(0, 1, 1, 0.2f);
                    Handles.DrawLine(region.transform.position, r.transform.position, 0.005f);
                }
                foreach (Region r in region.regionManager.regions)
                {
                    if (r.regionID != region.regionID)
                    {
                        if(r == null)
                        {
                            return;
                        }


                        RelationType type = GetRelationType(region, r);
                        Handles.zTest = CompareFunction.LessEqual;
                        Vector3 labelOffset = new Vector3(0f, 0.4f, 0);
                        Vector3 spacingOffset = Vector3.zero;
                        float camDist = 1f;
                        if (SceneView.currentDrawingSceneView != null)
                        {
                            camDist = Vector3.Distance(r.transform.position, SceneView.currentDrawingSceneView.camera.transform.position);
                            spacingOffset.y = camDist * 0.015f;
                        }
                        switch (showRegionMode)
                        {
                            case ShowRegionMode.None:

                                break;
                            case ShowRegionMode.All:
                                DrawSceneObjects(r);
                                break;
                            case ShowRegionMode.Connected:
                                if (region.IsConnectedTo(r.regionID))
                                {
                                    DrawSceneObjects(r);
                                }
                                break;
                            case ShowRegionMode.Near:
                                if (camDist < 16)
                                {
                                    DrawSceneObjects(r);
                                }
                                break;
                        }
                        Handles.matrix = matrix;
                        if (camDist < 25)
                        {
                            if (r.settings.showRegionLabels)
                            {
                                Handles.Label(r.transform.position + (labelOffset * 1.2f) + spacingOffset, r.name, EditorStyles.centeredGreyMiniLabel);

                                switch (type)
                                {
                                    case RelationType.Disconnected:
                                        Handles.Label(r.transform.position + labelOffset, "Disconnected", EditorStyles.centeredGreyMiniLabel);
                                        break;
                                    case RelationType.Connected:
                                        Handles.Label(r.transform.position + labelOffset, "Connected", EditorStyles.centeredGreyMiniLabel);
                                        break;
                                    case RelationType.PosOverride:
                                        Handles.Label(r.transform.position + labelOffset, "Positive Override", EditorStyles.centeredGreyMiniLabel);
                                        break;
                                    case RelationType.NegOverride:
                                        Handles.Label(r.transform.position + labelOffset, "Negative Override", EditorStyles.centeredGreyMiniLabel);
                                        break;
                                }
                            }
                            
                            if (showOverrideView)
                            {
                                switch (type)
                                {
                                    case RelationType.Disconnected:
                                        Handles.color = Color.yellow;
                                        break;
                                    case RelationType.Connected:
                                        Handles.color = Color.cyan;
                                        break;
                                    case RelationType.PosOverride:
                                        Handles.color = Color.green;
                                        break;
                                    case RelationType.NegOverride:
                                        Handles.color = Color.red;
                                        break;
                                }
                                Handles.zTest = CompareFunction.Always;
                                if (Handles.Button(r.transform.position, Quaternion.identity, 0.5f, 0.5f, Handles.SphereHandleCap))
                                {

                                    switch (type)
                                    {
                                        case RelationType.Disconnected:
                                            region.AddPositiveOverride(r);
                                            break;
                                        case RelationType.Connected:

                                            region.AddNegativeOverride(r);
                                            break;
                                        case RelationType.PosOverride:

                                            region.RemovePositiveOverride(r);
                                            break;
                                        case RelationType.NegOverride:

                                            region.RemoveNegativeOverride(r);
                                            break;
                                    }
                                    EditorUtility.SetDirty(region);
                                    EditorUtility.SetDirty(r);
                                }
                            }
                        }
                    }
                }
                if (bc != null && showSliceTool.boolValue)
                {
                    Vector3 originalSize = bc.size;
                    Vector3 originalCenter = bc.center;
                    Vector3 sliceSize = new Vector3(
                        originalSize.x / xSlices.intValue,
                        originalSize.y / ySlices.intValue,
                        originalSize.z / zSlices.intValue
                    );
                    for (int x = 0; x < xSlices.intValue; x++)
                    {
                        for (int y = 0; y < ySlices.intValue; y++)
                        {
                            for (int z = 0; z < zSlices.intValue; z++)
                            {
                                Handles.zTest = CompareFunction.LessEqual;
                                Vector3 slicePosition = region.transform.position + new Vector3(
                                    (x - xSlices.intValue / 2.0f + 0.5f) * sliceSize.x,
                                    (y - ySlices.intValue / 2.0f + 0.5f) * sliceSize.y,
                                    (z - zSlices.intValue / 2.0f + 0.5f) * sliceSize.z
                                );
                                Handles.color = Color.yellow;
                                Handles.DrawWireCube(slicePosition, sliceSize);
                                Handles.zTest = CompareFunction.Greater;
                                Handles.DrawWireCube(slicePosition, sliceSize);
                            }
                        }
                    }
                }
            }
            Handles.matrix = matrix;
        }
    }

    [CustomEditor(typeof(RegionManagedObject))]
    public class RegionManagedObjectEditor : Editor
    {
        SerializedProperty offset;
        SerializedProperty type;
        SerializedProperty lightComponent;
        SerializedProperty debugTrace;
        private void OnEnable()
        {
            offset = serializedObject.FindProperty("centerOffset");
            type = serializedObject.FindProperty("objectType");
            lightComponent = serializedObject.FindProperty("lightComponent");
            debugTrace = serializedObject.FindProperty("debugTrace");
        }
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(offset, new GUIContent("Center Offset"));
            EditorGUILayout.PropertyField(type, new GUIContent("Object Type"));

            switch (type.enumValueIndex)
            {
                case 0:

                    break;
                case 1:
                    EditorGUILayout.PropertyField(lightComponent, new GUIContent("Light Component"));
                    EditorGUILayout.PropertyField(debugTrace, new GUIContent("Debug Raytracing"));
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

}
