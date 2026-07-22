using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// Builds the AR reward: a spinning 3D Silver Compass (Срібний Компас) filmed by its own
    /// camera into a transparent RenderTexture, plus access to the device camera feed used as the
    /// AR background. Lives far from the main scene so nothing else renders it.
    /// </summary>
    public class ARShowcase : MonoBehaviour
    {
        public RenderTexture CompassTexture { get; private set; }

        private Transform _pivot;
        private Transform _needle;
        private bool _spinning;
        private WebCamTexture _webcam;

        public static ARShowcase Create(Transform parent)
        {
            var go = new GameObject("ARShowcase");
            var showcase = go.AddComponent<ARShowcase>();
            showcase.Build();
            if (parent != null) go.transform.SetParent(parent, true);
            return showcase;
        }

        public void SetSpinning(bool on) => _spinning = on;

        private void Update()
        {
            if (_spinning && _needle != null)
                _needle.Rotate(Vector3.up, 60f * Time.deltaTime, Space.Self);
        }

        /// <summary>Starts the device camera for the AR background. Returns null if unavailable.</summary>
        public WebCamTexture StartCamera()
        {
            try
            {
                if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0) return null;
                if (_webcam == null) _webcam = new WebCamTexture();
                if (!_webcam.isPlaying) _webcam.Play();
                return _webcam;
            }
            catch
            {
                return null;
            }
        }

        public void StopCamera()
        {
            if (_webcam != null && _webcam.isPlaying) _webcam.Stop();
        }

        private void Build()
        {
            transform.position = new Vector3(1000f, 1000f, 0f);

            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(transform, false);
            _pivot.localRotation = Quaternion.Euler(68f, 0f, 0f);

            var silver = PuzzleArt.LitMaterial(new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f, Color.black);
            var silverDark = PuzzleArt.LitMaterial(new Color(0.55f, 0.57f, 0.62f), 0.9f, 0.7f, Color.black);
            var face = PuzzleArt.LitMaterial(new Color(0.93f, 0.92f, 0.86f), 0.1f, 0.4f, Color.black);
            var red = PuzzleArt.LitMaterial(new Color(0.85f, 0.15f, 0.12f), 0.2f, 0.6f, new Color(0.2f, 0f, 0f));
            var white = PuzzleArt.LitMaterial(Color.white, 0.2f, 0.6f, Color.black);
            var gold = PuzzleArt.LitMaterial(new Color(0.95f, 0.78f, 0.28f), 0.95f, 0.85f, new Color(0.2f, 0.15f, 0.02f));

            Disc("Rim", _pivot, silverDark, 1.65f, 0.10f, -0.03f);
            Disc("Body", _pivot, silver, 1.45f, 0.14f, 0f);
            Disc("Face", _pivot, face, 1.20f, 0.16f, 0.02f);

            // Needle assembly spins on its own transform.
            _needle = new GameObject("Needle").transform;
            _needle.SetParent(_pivot, false);
            _needle.localPosition = new Vector3(0f, 0.12f, 0f);
            Bar("North", _needle, red, new Vector3(0f, 0f, 0.42f), new Vector3(0.10f, 0.04f, 0.85f));
            Bar("South", _needle, white, new Vector3(0f, 0f, -0.42f), new Vector3(0.10f, 0.04f, 0.85f));

            var pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SafeDestroy(pin.GetComponent<Collider>());
            pin.transform.SetParent(_needle, false);
            pin.transform.localScale = Vector3.one * 0.2f;
            pin.GetComponent<MeshRenderer>().sharedMaterial = gold;

            CompassTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "CompassRT" };
            CompassTexture.Create();

            var camGo = new GameObject("CompassCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -4.6f);
            camGo.transform.localRotation = Quaternion.identity;
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent for compositing over AR feed
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.targetTexture = CompassTexture;
            cam.depth = -5;

            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(40f, -25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
        }

        private static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private static void Disc(string name, Transform parent, Material mat, float diameter,
            float height, float y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            SafeDestroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(diameter, height, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void Bar(string name, Transform parent, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SafeDestroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
