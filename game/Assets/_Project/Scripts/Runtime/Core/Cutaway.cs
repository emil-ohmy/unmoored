using UnityEngine;

namespace MyGame
{
    // Tells the Toon shader where the Apprentice is every frame, so architecture standing between the camera and them
    // opens up around them (walls, houses, a tower's near wall, the floor above). Nothing else changes.
    public sealed class Cutaway : MonoBehaviour
    {
        public float radius = 1.5f;
        public float chest = 0.7f;
        static readonly int Centre = Shader.PropertyToID("_CutawayCentre");

        Apprentice apprentice;

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void LateUpdate()
        {
            if (apprentice == null)
            {
                Shader.SetGlobalVector(Centre, Vector4.zero);
                return;
            }
            Vector3 p = apprentice.transform.position + Vector3.up * chest;
            Shader.SetGlobalVector(Centre, new Vector4(p.x, p.y, p.z, radius));
        }

        void OnDisable() => Shader.SetGlobalVector(Centre, Vector4.zero);
    }
}
