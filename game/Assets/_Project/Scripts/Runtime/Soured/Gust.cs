using UnityEngine;

namespace MyGame
{
    // A gust off Father Moth's wings: a front of wind and dead leaves rolling out across the floor from under him. No
    // harm: it shoves whoever it passes (less behind a raised guard; a dodge goes through it untouched).
    public sealed class Gust : MonoBehaviour
    {
        public float Front { get; private set; } // how far it's come
        public bool Struck { get; private set; }

        Apprentice target;
        Vector3 direction;
        float speed, width, range, push;
        float leafDebt;

        public static Gust Blow(Vector3 from, Vector3 direction, Apprentice target, float speed, float width, float range, float push)
        {
            var gust = new GameObject("Gust").AddComponent<Gust>();
            gust.transform.position = from;
            direction.y = 0f;
            gust.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            gust.target = target;
            gust.speed = speed;
            gust.width = width;
            gust.range = range;
            gust.push = push;
            return gust;
        }

        void Update()
        {
            Front += speed * Time.deltaTime;
            if (Front >= range)
            {
                Destroy(gameObject);
                return;
            }
            // Leaves along the front, thinning as it spends itself.
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            leafDebt += Time.deltaTime * 70f * (1f - Front / range);
            for (; leafDebt >= 1f; leafDebt -= 1f)
                Effects.Instance?.Leaves(transform.position + direction * Front + side * Random.Range(-width, width) * 0.5f + Vector3.up * Random.Range(0.2f, 1.2f),
                    direction * speed * 0.8f, 1);
            if (Struck || target == null || !target.IsAlive)
                return;
            Vector3 to = target.transform.position - transform.position;
            float along = Vector3.Dot(to, direction), across = Mathf.Abs(Vector3.Dot(to, side));
            if (Mathf.Abs(along - Front) <= 0.7f && across <= width * 0.5f && to.y < 1.5f)
            {
                Struck = true;
                target.Buffet(direction * push);
            }
        }
    }
}
