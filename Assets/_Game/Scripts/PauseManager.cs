using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnect;
        }
    }

    private void PlayClick()
    {
        if (sfxSource && clickSound)
            sfxSource.PlayOneShot(clickSound);
    }

    public void PauseGame()
    {
        PlayClick();
        if (pausePanel) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        PlayClick();
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ExitToMainMenu()
    {
        PlayClick();
        Time.timeScale = 1f;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void OnDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            PlayerPrefs.SetString("MenuStatus", "HOST CLOSED THE SESSION!");
            PlayerPrefs.Save();

            Time.timeScale = 1f;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
    }
}