using UnityEngine;

namespace MyGame
{
    // A Soured's belt lantern, dropped where it fell: it tumbles off the belt, lands on its side and keeps glowing
    // sickly green until the Soured re-forms, marking the spot. Falling into the water, it sinks and goes out.
    // The Soured's own lantern light moves onto it (and back on re-forming); the body's lantern is hidden meanwhile.
    public sealed class DroppedLantern : MonoBehaviour
    {
        public Light carried;          // the Soured's lantern light, on its belt until dropped
        public Transform body;         // the lantern model (tumbles; this root stays upright so the light stays above it)
        public float waterLevel = -0.35f;
        public float lightHeight = 0.6f; // about belt height: lights a pool around it

        public bool Landed { get; private set; }
        public bool Sunk { get; private set; }

        Transform home;
        Vector3 homeLocal;
        LightFlicker flicker;
        float intensity;
        Vector3 velocity, spin;
        float groundY;
        bool falling, sinking;
        Quaternion restRotation;

        void Awake()
        {
            if (carried != null)
            {
                home = carried.transform.parent;
                flicker = carried.GetComponent<LightFlicker>();
                homeLocal = flicker != null ? flicker.BasePosition : carried.transform.localPosition;
                intensity = flicker != null ? flicker.BaseIntensity : carried.intensity;
            }
            foreach (var c in GetComponentsInChildren<Collider>())
                Destroy(c); // a prop to look at, not to trip over
            gameObject.SetActive(false);
        }

        // Knocked off the belt at `from` (the lantern's centre), flung along `push`.
        public void Drop(Vector3 from, Vector3 push)
        {
            transform.SetPositionAndRotation(from, Quaternion.identity);
            body.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            body.localPosition = Vector3.down * (0.085f * body.localScale.y); // the model's origin is its base; `from` is the glass
            velocity = Vector3.ClampMagnitude(push, 2.5f) + Vector3.up * 1.6f;
            spin = Random.onUnitSphere * Random.Range(360f, 720f);
            groundY = FindGround(from);
            falling = true;
            sinking = Landed = Sunk = false;
            gameObject.SetActive(true);
            MoveLight(transform, Vector3.up * 0.085f);
        }

        // The Soured re-formed: its lantern is back on its belt.
        public void Return()
        {
            if (carried != null)
            {
                MoveLight(home, homeLocal);
                SetIntensity(intensity);
                carried.enabled = true;
            }
            falling = sinking = Landed = Sunk = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (falling)
            {
                velocity.y -= 20f * dt;
                transform.position += velocity * dt;
                body.rotation = Quaternion.Euler(spin * dt) * body.rotation;
                if (transform.position.y <= Mathf.Max(groundY, waterLevel)) // the ground, or the water's surface
                {
                    falling = false;
                    if (groundY < waterLevel + 0.05f)
                        sinking = true;
                    else
                        Land();
                }
            }
            else if (sinking)
            {
                transform.position += Vector3.down * (0.35f * dt);
                SetIntensity(Mathf.MoveTowards(CurrentIntensity, 0f, intensity / 1.5f * dt));
                if (CurrentIntensity <= 0f)
                {
                    Sunk = true;
                    sinking = false;
                    MoveLight(home, homeLocal); // gone under: park the (dark) light back home until re-forming
                    carried.enabled = false;
                    gameObject.SetActive(false);
                }
            }
            else if (Landed)
                body.localRotation = Quaternion.RotateTowards(body.localRotation, restRotation, 900f * dt);
        }

        // Comes to rest on its side, rolled the way it was flung, glass just off the ground.
        void Land()
        {
            Landed = true;
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            Vector3 along = new Vector3(velocity.x, 0f, velocity.z);
            if (along.sqrMagnitude < 0.01f)
                along = Random.insideUnitCircle.normalized;
            restRotation = Quaternion.FromToRotation(Vector3.up, along.normalized) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float size = body.localScale.y;
            body.localPosition = Vector3.up * (0.06f * size) - along.normalized * (0.12f * size); // half its width; base end back
            MoveLight(transform, Vector3.up * lightHeight);
        }

        // The ground below, ignoring characters (their capsules are colliders too). Water if there's nothing solid.
        float FindGround(Vector3 from)
        {
            float best = waterLevel - 1f;
            foreach (var hit in Physics.RaycastAll(from + Vector3.up * 0.2f, Vector3.down, 6f))
                if (!(hit.collider is CharacterController) && hit.point.y > best && hit.point.y <= from.y + 0.2f)
                    best = hit.point.y;
            return Mathf.Max(best, waterLevel - 1f);
        }

        void MoveLight(Transform parent, Vector3 local)
        {
            if (carried == null)
                return;
            carried.transform.SetParent(parent, false);
            if (flicker != null)
                flicker.BasePosition = local;
            else
                carried.transform.localPosition = local;
        }

        float CurrentIntensity => flicker != null ? flicker.BaseIntensity : carried.intensity;

        void SetIntensity(float value)
        {
            if (flicker != null)
                flicker.BaseIntensity = value;
            else
                carried.intensity = value;
        }
    }
}
