using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle
{
    /// <summary>
    /// Lightweight UI "particle system" placeholder: spawns a burst of sparkle images that fly
    /// outward, fall under gravity and fade out. Lives on a full-screen UI layer so the sparks
    /// render above the chest.
    /// </summary>
    public class SparkleBurst : MonoBehaviour
    {
        private RectTransform _rt;

        private static readonly Color[] Palette =
        {
            new Color(1f, 0.9f, 0.4f),
            new Color(1f, 0.75f, 0.2f),
            new Color(1f, 1f, 0.85f),
            new Color(0.7f, 0.9f, 1f),
        };

        private void Awake() => _rt = (RectTransform)transform;

        /// <summary>Fire a burst centered at an anchored position (in this layer's space).</summary>
        public void Play(Vector2 center, int count = 40)
        {
            for (int i = 0; i < count; i++)
                StartCoroutine(Spark(center));
        }

        private IEnumerator Spark(Vector2 center)
        {
            var go = new GameObject("Spark", typeof(Image));
            var srt = (RectTransform)go.transform;
            srt.SetParent(_rt, false);
            srt.anchoredPosition = center;
            float size = Random.Range(18f, 42f);
            srt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.sprite = PuzzleArt.Spark();
            img.raycastTarget = false;
            var color = Palette[Random.Range(0, Palette.Length)];
            img.color = color;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(500f, 1300f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            Vector2 gravity = new Vector2(0f, -1600f);
            float life = Random.Range(0.6f, 1.1f);
            float spin = Random.Range(-360f, 360f);

            float t = 0f;
            while (t < life)
            {
                float dt = Time.deltaTime;
                t += dt;
                vel += gravity * dt;
                srt.anchoredPosition += vel * dt;
                srt.Rotate(0f, 0f, spin * dt);
                float k = 1f - (t / life);
                img.color = new Color(color.r, color.g, color.b, k);
                srt.localScale = Vector3.one * (0.6f + k * 0.8f);
                yield return null;
            }
            Destroy(go);
        }
    }
}
