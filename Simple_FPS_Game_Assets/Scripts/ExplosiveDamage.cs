using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveDamage : MonoBehaviour
{
    [HideInInspector] public float explosionDamage;
    [SerializeField] private float explosiveRadius = 3f;
    // Start is called before the first frame update
    void OnEnable()
    {
        ExplosionDamage(transform.position, explosiveRadius);
        gameObject.SetActive(false);
    }

    //might need for object pooling
    /*
    IEnumerator WaitOneFrame()
    {
        //returning 0 will make it wait 1 frame
        yield return 0;
    }
    */
    void ExplosionDamage(Vector3 center, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GetComponent<EnemyHealth>() != null)
            {
                EnemyHealth eh = hitCollider.GetComponent<EnemyHealth>();
                eh.health -= explosionDamage;
            }
        }
    }
}
