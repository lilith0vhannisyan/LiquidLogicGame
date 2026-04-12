using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnExitButton()
    {
        Application.Quit();
        Debug.Log("Exit!");
    }
}