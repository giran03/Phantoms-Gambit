using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IDestroyable
{
    void Damage(int damageAmount);
}

public class DestroyableStats
{
    int _health;
    GameObject _currentObect;
    public DamageMultiplier _damageMultiplier;

    public enum DamageMultiplier    // 100 = 10% , 10 = 1% , 5 = 0.5%
    {
        Fragile = 100,
        Normal = 10,
        Tanky = 3,
    }


    public DestroyableStats(GameObject currentObject, int health, DamageMultiplier damageMultiplier)
    {
        _damageMultiplier = damageMultiplier;
        _currentObect = currentObject;
        _health = health;
    }

    public void Hit(int damageAmount, List<GameObject> propsModel = null, bool isRespawnable = false)
    {
        float multiplier = (float)_damageMultiplier * .1f;

        _health -= (int)(damageAmount * multiplier);

        Debug.Log($"Damage: {damageAmount * multiplier}");
        Debug.Log($"Multiplier: {multiplier}");
        Debug.Log($"Health: {_health}");

        if (isRespawnable)
        {
            if (_health <= 0)
                _currentObect.GetComponent<MonoBehaviour>().StartCoroutine(Respawn());
        }
        else
        {
            if (_health <= 0)
            {
                foreach (var prop in propsModel)
                    Object.Destroy(prop);

                if (_currentObect != null)
                    _currentObect = null;
            }
        }

        IEnumerator Respawn()
        {
            propsModel.ForEach(x => x.SetActive(false));
            propsModel.ForEach(x => x.GetComponentInParent<Collider>().enabled = false);
            yield return new WaitForSeconds(5);
            propsModel.ForEach(x => x.SetActive(true));
            propsModel.ForEach(x => x.GetComponentInParent<Collider>().enabled = true);
        }
    }


    public Vector3 GetRandomPointInRadius(float radius)
    {
        Vector3 pointAboveTerrain;
        RaycastHit hit;
        do
        {
            float randomX = Random.Range(-radius, radius);
            float randomZ = Random.Range(-radius, radius);

            pointAboveTerrain = _currentObect.transform.position + new Vector3(randomX, 100, randomZ);

        } while (Physics.Raycast(pointAboveTerrain, Vector3.down, out hit, 200f, LayerMask.GetMask("Terrain")) == false);

        Vector3 targetPoint = hit.point;
        targetPoint.y += 4f;   // offset from ground

        return targetPoint;
    }
}
