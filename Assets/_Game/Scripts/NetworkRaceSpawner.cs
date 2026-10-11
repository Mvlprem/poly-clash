using Unity.Netcode;
using UnityEngine;

public class NetworkRaceSpawner : NetworkBehaviour
{
    [Header("Car Prefabs")]
    [SerializeField] private GameObject[] carPrefabs;

    [Header("Starting Grid Spots")]
    [SerializeField] private Transform gridSpot1;
    [SerializeField] private Transform gridSpot2;

    public override void OnNetworkSpawn()
    {
        int myCarIndex = PlayerPrefs.GetInt("SelectedCar", 0);
        SpawnCarRpc(myCarIndex, NetworkManager.Singleton.LocalClientId);

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnCarRpc(int carIndex, ulong clientId)
    {
        Transform spawnPoint = (clientId == NetworkManager.ServerClientId) ? gridSpot1 : gridSpot2;

        Vector3 pos = spawnPoint ? spawnPoint.position : new Vector3(0, 0.5f, 0);
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        if (carIndex < 0 || carIndex >= carPrefabs.Length) carIndex = 0;

        GameObject car = Instantiate(carPrefabs[carIndex], pos, rot);
        car.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            int myCarIndex = PlayerPrefs.GetInt("SelectedCar", 0);
            if (myCarIndex < 0 || myCarIndex >= carPrefabs.Length) myCarIndex = 0;

            Vector3 pos = gridSpot1 ? gridSpot1.position : new Vector3(0, 0.5f, 0);
            Quaternion rot = gridSpot1 ? gridSpot1.rotation : Quaternion.identity;

            GameObject car = Instantiate(carPrefabs[myCarIndex], pos, rot);
            Camera chaseCam = FindFirstObjectByType<Camera>();
            if (chaseCam) chaseCam.target = car.transform;
        }
    }
}