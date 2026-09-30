using UnityEngine;

namespace MyGame
{
    // Slow motes of light rising around a place, now and then (the Far shore's light: where the dead go on).
    public sealed class Motes : MonoBehaviour
    {
        public float perSecond = 3f;
        public float radius = 1.2f;
        public Color colour = new Color(1f, 0.95f, 0.82f, 0.9f);

        float debt;

        void Update()
        {
            if (Effects.Instance == null)
                return;
            debt += perSecond * Time.deltaTime;
            for (; debt >= 1f; debt -= 1f)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 at = transform.position + new Vector3(r.x, Random.Range(-0.3f, 0.6f), r.y);
                Effects.Instance.Wisp(at, colour, new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.25f, 0.55f), Random.Range(-0.1f, 0.1f)), Random.Range(2.5f, 4f));
            }
        }
    }
}
