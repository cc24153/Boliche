using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class IrAoJogo : MonoBehaviour
{
    private const string SimulatorScene = "Boliche";
#if UNITY_EDITOR
    private const string SimulatorScenePath = "Assets/Scenes/Boliche.unity";
#endif
    private static bool startOnLoad;
    private BowlingGame game;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPendingStart()
    {
        startOnLoad = false;
    }

    public void Configure(BowlingGame target)
    {
        game = target;
    }

    public void Jogar()
    {
        if (game != null)
        {
            game.StartMatch();
            return;
        }

        if (startOnLoad)
            return;

        if (Application.CanStreamedLevelBeLoaded(SimulatorScene))
        {
            startOnLoad = true;
            SceneManager.LoadScene(SimulatorScene);
            return;
        }

#if UNITY_EDITOR
        // A scene opened directly in the Editor may be outside the active build profile.
        startOnLoad = true;
        var scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            SimulatorScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        if (!scene.IsValid())
            startOnLoad = false;
#else
        Debug.LogError("A cena Boliche precisa estar na lista de cenas do build.", this);
#endif
    }

    public static bool ConsumePendingStart()
    {
        bool pending = startOnLoad;
        startOnLoad = false;
        return pending;
    }
}
