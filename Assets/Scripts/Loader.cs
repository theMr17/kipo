using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class Loader {
    public enum Scene
    {
        LoadingScene,
        MainMenuScene,
        LobbyScene,
        CharacterSelectScene,
        SampleScene
    }

    public static Scene targetScene;

    public static void Load(Scene targetScene) {
        Loader.targetScene = targetScene;

        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoadNetwork(Scene targetScene) {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoaderCallback() {
        SceneManager.LoadScene(targetScene.ToString());
    }
}
