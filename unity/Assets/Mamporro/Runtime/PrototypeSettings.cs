using UnityEngine;

namespace Mamporro.U1
{
    [CreateAssetMenu(menuName = "Mamporro/U1 Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        public float moveSpeed = 9.5f;
        public float groundAcceleration = 75f;
        public float airAcceleration = 30f;
        public float deceleration = 55f;
        public float gravity = 27f;
        public float jumpSpeed = 9.8f;
        public float coyoteSeconds = .1f;
        public float jumpBufferSeconds = .13f;
        public float radius = .4f;
        public float stepHeight = .45f;
        public float slopeLimit = 48f;
        public float enemySpeed = 4.2f;
        public int seed = 6741;
        public int initialEnemies = 300;
        public int internalHeight = 360;
    }
}
