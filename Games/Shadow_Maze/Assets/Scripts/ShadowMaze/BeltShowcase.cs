using UnityEngine;

namespace ShadowMaze
{
    /// <summary>
    /// Builds the 3D reward — a Cossack belt (Козацький Пояс) — from a procedural torus mesh
    /// plus a golden buckle and studs. It lives far from the 2D gameplay camera and is filmed
    /// by its own perspective camera into a RenderTexture, shown on the victory screen.
    /// </summary>
    public class BeltShowcase : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }

        private Transform _pivot;
        private float _spin = 35f;
        private bool _spinning;

        public static BeltShowcase Create(Transform parent = null)
        {
            var root = new GameObject("BeltShowcase");
            var showcase = root.AddComponent<BeltShowcase>();
            showcase.Build();
            // Parent afterwards keeping the far-away world position so the 2D camera never sees it.
            if (parent != null) root.transform.SetParent(parent, true);
            return showcase;
        }

        public void SetSpinning(bool on) => _spinning = on;

        private void Update()
        {
            if (_spinning && _pivot != null)
                _pivot.Rotate(Vector3.up, _spin * Time.deltaTime, Space.World);
        }

        private void Build()
        {
            // Keep the showcase far away so the orthographic 2D camera never sees it.
            transform.position = new Vector3(1000f, 1000f, 0f);

            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(transform, false);
            _pivot.localRotation = Quaternion.Euler(18f, 0f, 0f);

            var beltMat = PrimitiveFactory.LitMaterial(
                new Color(0.55f, 0.12f, 0.12f), 0.1f, 0.5f, Color.black); // deep crimson sash
            var goldMat = PrimitiveFactory.LitMaterial(
                new Color(0.95f, 0.75f, 0.22f), 0.95f, 0.85f, new Color(0.25f, 0.18f, 0.03f));

            // The sash ring (torus).
            var ring = new GameObject("Sash");
            ring.transform.SetParent(_pivot, false);
            var mf = ring.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildTorus(1.05f, 0.34f, 48, 20);
            ring.AddComponent<MeshRenderer>().sharedMaterial = beltMat;

            // Golden buckle plate at the front of the belt.
            var buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckle.name = "Buckle";
            Destroy(buckle.GetComponent<Collider>());
            buckle.transform.SetParent(_pivot, false);
            buckle.transform.localPosition = new Vector3(0f, -1.05f, 0.34f);
            buckle.transform.localScale = new Vector3(0.7f, 0.5f, 0.18f);
            buckle.GetComponent<MeshRenderer>().sharedMaterial = goldMat;

            // Decorative studs around the ring.
            int studs = 10;
            for (int i = 0; i < studs; i++)
            {
                float a = (i / (float)studs) * Mathf.PI * 2f;
                var stud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(stud.GetComponent<Collider>());
                stud.transform.SetParent(_pivot, false);
                stud.transform.localPosition = new Vector3(Mathf.Sin(a) * 1.05f, Mathf.Cos(a) * 1.05f, 0.32f);
                stud.transform.localScale = Vector3.one * 0.16f;
                stud.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
            }

            // Dedicated render target + camera.
            Texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "BeltRT" };
            Texture.Create();

            var camGo = new GameObject("ShowcaseCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -4.2f);
            camGo.transform.localRotation = Quaternion.identity;
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.10f, 1f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.targetTexture = Texture;
            cam.depth = -5;

            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(35f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.97f, 0.9f);
        }

        /// <summary>Generates a torus (donut) mesh centered at origin, ring in the XY plane.</summary>
        private static Mesh BuildTorus(float radius, float tube, int radialSegments, int tubularSegments)
        {
            int vertCount = (radialSegments + 1) * (tubularSegments + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var tris = new int[radialSegments * tubularSegments * 6];

            int v = 0;
            for (int i = 0; i <= radialSegments; i++)
            {
                float u = (i / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(u) * radius, Mathf.Sin(u) * radius, 0f);
                for (int j = 0; j <= tubularSegments; j++)
                {
                    float vAng = (j / (float)tubularSegments) * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(u) * Mathf.Cos(vAng),
                                              Mathf.Sin(u) * Mathf.Cos(vAng),
                                              Mathf.Sin(vAng));
                    verts[v] = center + dir * tube;
                    normals[v] = dir;
                    uvs[v] = new Vector2(i / (float)radialSegments, j / (float)tubularSegments);
                    v++;
                }
            }

            int t = 0;
            int stride = tubularSegments + 1;
            for (int i = 0; i < radialSegments; i++)
            for (int j = 0; j < tubularSegments; j++)
            {
                int a = i * stride + j;
                int b = (i + 1) * stride + j;
                int c = (i + 1) * stride + (j + 1);
                int d = i * stride + (j + 1);
                tris[t++] = a; tris[t++] = b; tris[t++] = d;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }

            var mesh = new Mesh { name = "Torus" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
