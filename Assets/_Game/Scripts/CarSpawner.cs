using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Car Prefabs")]
    [SerializeField] private GameObject[] carPrefabs;

    [Header("Camera")]
    [SerializeField] private Camera followCamera;

    private void Awake()
    {
        int index = PlayerPrefs.GetInt("SelectedCar", 0);
        if (index < 0 || index >= carPrefabs.Length) index = 0;

        GameObject car = Instantiate(carPrefabs[index], transform.position, transform.rotation);

        if (followCamera)
        {
            followCamera.target = car.transform;
        }
    }
}