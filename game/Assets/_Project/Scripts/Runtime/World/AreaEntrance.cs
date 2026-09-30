using UnityEngine;

namespace MyGame
{
    // Where an Area begins: the first time the Apprentice steps into this box, its name is announced (the HUD's
    // title card). Once per visit to the scene.
    public sealed class AreaEntrance : MonoBehaviour
    {
        public static event System.Action<string> Entered;

        public string areaName = "The Drowned City";
        public Vector3 size = new Vector3(6f, 4f, 1.5f);

        public bool Announced { get; private set; }
        Apprentice apprentice;

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            if (Announced || apprentice == null || !apprentice.IsAlive)
                return;
            Vector3 local = transform.InverseTransformPoint(apprentice.transform.position);
            if (Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f)
            {
                Announced = true;
                Entered?.Invoke(areaName);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
