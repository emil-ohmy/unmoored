using UnityEngine;

namespace MyGame
{
    // What an Area does to everything in it. The Orchard of Last Autumn is dry as old leaves: everything there burns
    // (fire's harm, and how long it burns, both multiplied). One per scene; none, and nothing's changed.
    public sealed class AreaTraits : MonoBehaviour
    {
        public static AreaTraits Current { get; private set; }

        [Tooltip("Fire's harm to the Soured here, times this (the Orchard: more)")]
        public float fireHarm = 1f;
        [Tooltip("How long they burn, times this")]
        public float burnLength = 1f;

        public static float FireHarm => Current != null ? Current.fireHarm : 1f;
        public static float BurnLength => Current != null ? Current.burnLength : 1f;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }
    }
}
