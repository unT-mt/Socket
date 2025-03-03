using UnityEngine;

public class PlayerPrefsClearer : MonoBehaviour
{
    void Update()
    {
        // スペースキーを押したら全てのPlayerPrefsを削除
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("All PlayerPrefs have been cleared.");
        }
    }
}
