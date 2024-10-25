using System.Collections.Generic;
using UnityEngine;

namespace LightRegions
{
    public class LightInfluenceMesher : MonoBehaviour
    {
        public LightRegionsSettings settings;
        public int subdivisions = 2;  // Number of subdivisions
        public RegionManagedObject regionManagedObject;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public float planarThreshold = 1f;
        public Mesh mesh;
        private Mesh meshPreDecimate;
        public bool drawMeshGizmo;
        public bool renderedThisFrame = false;

        public void Generate(RegionManagedObject regionObject)
        {
            regionManagedObject = regionObject;
            int sD = regionObject.regionManager.settings.influneceMeshProjectionSubdivisions;
            GenerateMesh(sD);
            for(int i = 0; i < 4; i++)
            {
                if(i >= 2 && sD > 0)
                {
                    sD--;
                }
                GenerateMesh(sD);
                //Planar decimate the mesh.
                mesh = SimplifyMesh(meshPreDecimate, 1 + (i * 0.8f));
                mesh.name = "Light Influence Mesh (" + regionManagedObject.name + ")";
                if (mesh.vertices.Length < 128)
                {
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    meshCollider.sharedMesh = mesh;
                    return;
                }
            }
            Debug.LogWarning("Couldn't simplify the mesh enough. Expected a vertex count under 128 but only achieved " + mesh.vertices.Length);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = mesh;
        }
        public void GenerateMesh(int subdivisions)
        {

            //Center the object on the region manged objects offset point
            transform.position = regionManagedObject.GetOffsetPoint();
            transform.rotation = Quaternion.identity;
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            //Apply layermasks
            meshCollider.excludeLayers = regionManagedObject.regionManager.lightInfluenceMaskExclude;
            meshCollider.includeLayers = regionManagedObject.regionManager.lightInfluenceMaskInclude;
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("LightInfluenceMesh");
            meshPreDecimate = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            // Create initial isosphere for the shadow projection.
            CreateIsosphere(vertices, triangles);

            // Subdivide the isosphere for a higher resolution projection.
            for (int i = 0; i < subdivisions; i++)
            {
                Subdivide(vertices, triangles);
            }

            // Normalize vertices to make the shape spherical
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized * 1;
            }
            for (int i = 0; i < vertices.Count; i++)
            {
                RaycastHit hit;
                if (Physics.Raycast(regionManagedObject.GetOffsetPoint(), vertices[i], out hit, regionManagedObject.lightComponent.range, regionManagedObject.regionManager.lightOcclusionLayerMask))
                {
                    vertices[i] = hit.point - transform.position;
                }
                else
                {
                    vertices[i] = vertices[i] * regionManagedObject.lightComponent.range;
                }
            }
            meshPreDecimate.vertices = vertices.ToArray();
            meshPreDecimate.triangles = triangles.ToArray();
        }

        //Planar decimator
        private Mesh SimplifyMesh(Mesh mesh, float multiplier)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            List<Vector3> newVertices = new List<Vector3>();

            int GetOrAddVertex(Vector3 vertex)
            {
                for (int i = 0; i < newVertices.Count; i++)
                {
                    if (Vector3.Distance(vertex, newVertices[i]) < regionManagedObject.regionManager.settings.influenceMeshDecimationFactor * multiplier)
                    {
                        return i;
                    }
                }
                newVertices.Add(vertex);
                return newVertices.Count - 1;
            }
            int[] newVertexIndices = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                newVertexIndices[i] = GetOrAddVertex(vertex);
            }
            List<int> newTriangles = new List<int>();
            for (int i = 0; i < triangles.Length; i++)
            {
                newTriangles.Add(newVertexIndices[triangles[i]]);
            }
            Mesh simplifiedMesh = new Mesh
            {
                vertices = newVertices.ToArray(),
                triangles = newTriangles.ToArray()
            };
            simplifiedMesh.RecalculateBounds();
            simplifiedMesh.RecalculateNormals();
            return simplifiedMesh;
        }

        //Render the shadows of the attached light
        public void RenderShadows()
        {
            if (renderedThisFrame)
            {
                return;
            }
            if (regionManagedObject.region.regionActive)
            {
                LightRegionCompatibilityMethods.RenderLightShadow(regionManagedObject.lightComponent);
                renderedThisFrame = true;
            }

        }
        private void LateUpdate()
        {
            renderedThisFrame = false;
        }

        //Generates a basic Isosphere mesh
        private void CreateIsosphere(List<Vector3> vertices, List<int> triangles)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            // Verts
            vertices.Add(new Vector3(-1, t, 0).normalized);
            vertices.Add(new Vector3(1, t, 0).normalized);
            vertices.Add(new Vector3(-1, -t, 0).normalized);
            vertices.Add(new Vector3(1, -t, 0).normalized);

            vertices.Add(new Vector3(0, -1, t).normalized);
            vertices.Add(new Vector3(0, 1, t).normalized);
            vertices.Add(new Vector3(0, -1, -t).normalized);
            vertices.Add(new Vector3(0, 1, -t).normalized);

            vertices.Add(new Vector3(t, 0, -1).normalized);
            vertices.Add(new Vector3(t, 0, 1).normalized);
            vertices.Add(new Vector3(-t, 0, -1).normalized);
            vertices.Add(new Vector3(-t, 0, 1).normalized);

            // Tris
            triangles.AddRange(new int[] { 0, 11, 5 });
            triangles.AddRange(new int[] { 0, 5, 1 });
            triangles.AddRange(new int[] { 0, 1, 7 });
            triangles.AddRange(new int[] { 0, 7, 10 });
            triangles.AddRange(new int[] { 0, 10, 11 });

            triangles.AddRange(new int[] { 1, 5, 9 });
            triangles.AddRange(new int[] { 5, 11, 4 });
            triangles.AddRange(new int[] { 11, 10, 2 });
            triangles.AddRange(new int[] { 10, 7, 6 });
            triangles.AddRange(new int[] { 7, 1, 8 });

            triangles.AddRange(new int[] { 3, 9, 4 });
            triangles.AddRange(new int[] { 3, 4, 2 });
            triangles.AddRange(new int[] { 3, 2, 6 });
            triangles.AddRange(new int[] { 3, 6, 8 });
            triangles.AddRange(new int[] { 3, 8, 9 });

            triangles.AddRange(new int[] { 4, 9, 5 });
            triangles.AddRange(new int[] { 2, 4, 11 });
            triangles.AddRange(new int[] { 6, 2, 10 });
            triangles.AddRange(new int[] { 8, 6, 7 });
            triangles.AddRange(new int[] { 9, 8, 1 });
        }

        //Subdivide the mesh
        private void Subdivide(List<Vector3> vertices, List<int> triangles)
        {
            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
            List<int> newTriangles = new List<int>();

            int[] oldTriangles = triangles.ToArray();
            for (int i = 0; i < oldTriangles.Length; i += 3)
            {
                int v1 = oldTriangles[i];
                int v2 = oldTriangles[i + 1];
                int v3 = oldTriangles[i + 2];

                int a = GetMiddlePoint(v1, v2, vertices, middlePointIndexCache);
                int b = GetMiddlePoint(v2, v3, vertices, middlePointIndexCache);
                int c = GetMiddlePoint(v3, v1, vertices, middlePointIndexCache);

                newTriangles.Add(v1);
                newTriangles.Add(a);
                newTriangles.Add(c);

                newTriangles.Add(v2);
                newTriangles.Add(b);
                newTriangles.Add(a);

                newTriangles.Add(v3);
                newTriangles.Add(c);
                newTriangles.Add(b);

                newTriangles.Add(a);
                newTriangles.Add(b);
                newTriangles.Add(c);
            }

            triangles.Clear();
            triangles.AddRange(newTriangles);
        }

        //Get the middle point between two points
        private int GetMiddlePoint(int p1, int p2, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            long smallerIndex = Mathf.Min(p1, p2);
            long greaterIndex = Mathf.Max(p1, p2);
            long key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (cache.TryGetValue(key, out ret))
            {
                return ret;
            }

            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = (point1 + point2).normalized;

            ret = vertices.Count;
            vertices.Add(middle);

            cache[key] = ret;

            return ret;
        }
        private void OnDrawGizmosSelected()
        {
            if (drawMeshGizmo)
            {
                //Gizmos.color = Color.cyan;
                //Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation);
            }
        }
    }
}
