using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneResetter : MonoBehaviour
{
    [SerializeField] private KeyCode resetKey = KeyCode.R;  // change this in the Inspector

    void Update()
    {
        if (Input.GetKeyUp(resetKey))
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }
}
