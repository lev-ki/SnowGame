using MyBox;
using UnityEngine;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowGrabber : MonoBehaviour
    {
        public float pushForce = 1;
        public Vector2 pushDirection = Vector2.right;
        public float rotationSpeed = -0.15f;
        public float grabbingCost = 0.0001f;
        public float valueMult = 100000f;
        public int spread = 10;
        public int pickingMaxPointSpread = 10;
        private Rigidbody2D rb2d;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            rb2d.linearVelocity = pushDirection * pushForce;
            rb2d.AddTorque(rotationSpeed, ForceMode2D.Force);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            ApplyGrabbing(other);
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            ApplyGrabbing(other);
        }

        private void ApplyGrabbing(Collision2D other)
        {
            if (other.gameObject.HasComponent<SnowShaderWriter>())
            {
                foreach(ContactPoint2D point in other.contacts)
                {
                    SnowShaderWriter Snow = other.gameObject.GetComponent<SnowShaderWriter>();
                    int snowIndex = Snow.WorldPositionToArrayIndex(point.point);
                    if (snowIndex != -1)
                    {
                        snowIndex = Snow.FindMaxIndexWithinRange(snowIndex, pickingMaxPointSpread);
                        Snow.GatherSnow(point.point, snowIndex, grabbingCost, spread, 0.5f, valueMult);
                        // Snow.AdjustSnowAtPoint_Immediate(snowIndex, grabbingCost);
                        // NRGameResourceManager.Instance.ResourceSnow += (int)(grabbingCost * valueMult);
                    }
                }
            }
        }
    }
}
