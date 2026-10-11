using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenuManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Showroom Cars")]
    [SerializeField] private GameObject[] showroomCars;
    private int currentCarIndex = 0;

    private void Start()
    {
        currentCarIndex = PlayerPrefs.GetInt("SelectedCar", 0);
        UpdateShowroom();
    }

    public void NextCar()
    {
        PlayClickSound();
        currentCarIndex = (currentCarIndex + 1) % showroomCars.Length;
        UpdateShowroom();
    }

    public void PrevCar()
    {
        PlayClickSound();
        currentCarIndex--;
        if (currentCarIndex < 0) currentCarIndex = showroomCars.Length - 1;
        UpdateShowroom();
    }

    private void UpdateShowroom()
    {
        for (int i = 0; i < showroomCars.Length; i++)
        {
            if (showroomCars[i])
                showroomCars[i].SetActive(i == currentCarIndex);
        }

        PlayerPrefs.SetInt("SelectedCar", currentCarIndex);
        PlayerPrefs.Save();
    }

    public void PlayClickSound()
    {
        if (sfxSource && clickSound)
            sfxSource.PlayOneShot(clickSound);
    }

    public void PlayGame()
    {
        PlayClickSound();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("TestDrive", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("TestDrive");
        }
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}