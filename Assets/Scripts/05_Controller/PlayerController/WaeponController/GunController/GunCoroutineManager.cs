using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunCoroutineManager : MonoBehaviour
{
    public void StartCoroutine(GameObject bullet, float bulletSpeed)
    {
        StartCoroutine(FireBulletRoutine(bullet, bulletSpeed));
        StartCoroutine(DestroyNoHitBulletRoutine(bullet));
    }

    /// <summary>
    /// ÃÑ¾Ë ¹ß»ç
    /// </summary>
    /// <param name="bullet"></param>
    /// <param name="bulletSpeed"></param>
    /// <returns></returns>
    private IEnumerator FireBulletRoutine(GameObject bullet, float bulletSpeed)
    {
        while (true)
        {
            if (bullet == null) yield break;
            bullet.transform.Translate(bulletSpeed * Time.deltaTime * Vector2.right);
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    /// <summary>
    /// No Hit ÃÑ¾Ë Á¦°Å
    /// </summary>
    /// <param name="bullet"></param>
    /// <returns></returns>
    private IEnumerator DestroyNoHitBulletRoutine(GameObject bullet)
    {
        float bulletDurationTime = 3f;

        while (true)
        {
            if (bulletDurationTime <= 0)
            {
                Destroy(bullet);
                yield break;
            }
            bulletDurationTime -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
