using HEHA.Obby.Core;
using UnityEngine;

namespace HEHA.Obby.Obstacles
{
    public enum RotatingObstacleMode
    {
        KillOnContact,
        KnockbackOnly
    }

    public class RotatingObstacle : MonoBehaviour
    {
        [SerializeField] Vector3 rotationAxis = Vector3.up;
        [SerializeField] float degreesPerSecond = 90f;
        [SerializeField] RotatingObstacleMode mode = RotatingObstacleMode.KillOnContact;
        [SerializeField] float knockbackForce = 12f;

        ObstacleContactKill contactKill;

        void Awake()
        {
            if (mode == RotatingObstacleMode.KillOnContact)
            {
                contactKill = GetComponent<ObstacleContactKill>();
                if (contactKill == null)
                    contactKill = gameObject.AddComponent<ObstacleContactKill>();
            }
        }

        void Update()
        {
            transform.Rotate(rotationAxis.normalized, degreesPerSecond * Time.deltaTime, Space.Self);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (mode != RotatingObstacleMode.KnockbackOnly)
                return;

            if (!PlayerDeathUtility.TryGetDeathHandler(collision.collider, out _))
                return;

            Rigidbody rb = collision.rigidbody;
            if (rb == null)
                return;

            Vector3 push = (collision.contacts.Length > 0 ? collision.contacts[0].normal : transform.forward) * knockbackForce;
            rb.AddForce(push, ForceMode.Impulse);
        }
    }
}
