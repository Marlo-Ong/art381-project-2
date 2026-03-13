using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RootSceneLoader : MonoBehaviour
{
    private const string CollectSceneName = "CollectScene";
    private const string ViewSceneName = "ViewScene";

    private enum ManagedScene
    {
        CollectScene,
        ViewScene
    }

    [Header("Boot")]
    [SerializeField] private ManagedScene startupScene = ManagedScene.CollectScene;
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Additive;

    private static RootSceneLoader instance;

    private bool isTransitioning;
    private string currentSceneName = string.Empty;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (loadMode == LoadSceneMode.Single)
            DontDestroyOnLoad(gameObject);

        currentSceneName = GetLoadedManagedSceneName();
    }

    private void Start()
    {
        TryBeginSwitch(GetSceneName(startupScene));
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void LoadCollectScene()
    {
        TryBeginSwitch(CollectSceneName);
    }

    public void LoadViewScene()
    {
        TryBeginSwitch(ViewSceneName);
    }

    public void ToggleScene()
    {
        var loadedSceneName = GetLoadedManagedSceneName();
        if (!string.IsNullOrEmpty(loadedSceneName))
            currentSceneName = loadedSceneName;

        var nextSceneName = currentSceneName == ViewSceneName
            ? CollectSceneName
            : ViewSceneName;

        TryBeginSwitch(nextSceneName);
    }

    private void TryBeginSwitch(string targetSceneName)
    {
        if (!Application.isPlaying || isTransitioning)
            return;

        StartCoroutine(SwitchToSceneRoutine(targetSceneName));
    }

    private IEnumerator SwitchToSceneRoutine(string targetSceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"Scene '{targetSceneName}' is not in Build Settings.", this);
            yield break;
        }

        isTransitioning = true;

        if (loadMode == LoadSceneMode.Additive)
            yield return SwitchAdditiveRoutine(targetSceneName);
        else
            yield return SwitchSingleRoutine(targetSceneName);

        isTransitioning = false;
    }

    private IEnumerator SwitchAdditiveRoutine(string targetSceneName)
    {
        currentSceneName = GetLoadedManagedSceneName();
        if (currentSceneName == targetSceneName)
        {
            var targetScene = SceneManager.GetSceneByName(targetSceneName);
            if (targetScene.IsValid() && targetScene.isLoaded)
                SceneManager.SetActiveScene(targetScene);

            yield break;
        }

        var rootScene = gameObject.scene;
        if (rootScene.IsValid() && rootScene.isLoaded)
            SceneManager.SetActiveScene(rootScene);

        yield return UnloadManagedScenesExcept(targetSceneName);

        var existingTargetScene = SceneManager.GetSceneByName(targetSceneName);
        if (!existingTargetScene.isLoaded)
        {
            var loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"Failed to start loading scene '{targetSceneName}'.", this);
                yield break;
            }

            yield return loadOperation;
            existingTargetScene = SceneManager.GetSceneByName(targetSceneName);
        }

        if (existingTargetScene.IsValid() && existingTargetScene.isLoaded)
            SceneManager.SetActiveScene(existingTargetScene);

        currentSceneName = targetSceneName;
    }

    private IEnumerator SwitchSingleRoutine(string targetSceneName)
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == targetSceneName && activeScene.isLoaded)
        {
            currentSceneName = targetSceneName;
            yield break;
        }

        var loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogError($"Failed to start loading scene '{targetSceneName}'.", this);
            yield break;
        }

        yield return loadOperation;
        currentSceneName = targetSceneName;
    }

    private IEnumerator UnloadManagedScenesExcept(string sceneNameToKeep)
    {
        if (sceneNameToKeep != CollectSceneName)
            yield return UnloadSceneIfLoaded(CollectSceneName);

        if (sceneNameToKeep != ViewSceneName)
            yield return UnloadSceneIfLoaded(ViewSceneName);
    }

    private static IEnumerator UnloadSceneIfLoaded(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
            yield break;

        var unloadOperation = SceneManager.UnloadSceneAsync(scene);
        if (unloadOperation != null)
            yield return unloadOperation;
    }

    private static string GetSceneName(ManagedScene scene)
    {
        return scene == ManagedScene.ViewScene
            ? ViewSceneName
            : CollectSceneName;
    }

    private string GetLoadedManagedSceneName()
    {
        var currentScene = SceneManager.GetSceneByName(currentSceneName);
        if (!string.IsNullOrEmpty(currentSceneName) && currentScene.IsValid() && currentScene.isLoaded)
            return currentSceneName;

        var collectScene = SceneManager.GetSceneByName(CollectSceneName);
        if (collectScene.IsValid() && collectScene.isLoaded)
            return CollectSceneName;

        var viewScene = SceneManager.GetSceneByName(ViewSceneName);
        if (viewScene.IsValid() && viewScene.isLoaded)
            return ViewSceneName;

        return string.Empty;
    }
}
