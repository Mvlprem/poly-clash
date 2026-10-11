using System.Collections;
using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField] private float minWait = 5f;
    [SerializeField] private float maxWait = 12f;
    [SerializeField] private float baseIntensity = 0.2f;
    [SerializeField] private float flashIntensity = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource thunderSource;
    [SerializeField] private AudioClip thunderSound;

    private void Start()
    {
        if (!targetLight) targetLight = GetComponent<Light>();
        StartCoroutine(LightningLoop());
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWait, maxWait));

            if (thunderSource && thunderSound)
                thunderSource.PlayOneShot(thunderSound);

            targetLight.intensity = flashIntensity;
            yield return new WaitForSeconds(0.05f);
            targetLight.intensity = 0.6f;
            yield return new WaitForSeconds(0.04f);
            targetLight.intensity = flashIntensity * 1.2f;
            yield return new WaitForSeconds(0.06f);
            targetLight.intensity = baseIntensity;
        }
    }
}