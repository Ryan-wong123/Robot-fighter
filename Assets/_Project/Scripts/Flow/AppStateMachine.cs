using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WobbleBrawl.Flow
{
    public enum AppState { Boot, MainMenu, Selection, Match, Results }

    [DefaultExecutionOrder(-100)]
    public sealed class AppStateMachine : MonoBehaviour
    {
        [SerializeField] private string bootScene = "Boot";
        [SerializeField] private string matchScene = "CombatSandbox";
        [SerializeField, Min(0f)] private float resultsDelay = 1.5f;

        private static AppStateMachine running;
        private MatchController match;
        private Coroutine pendingResults;
        public bool IsPrimary => running == this;
        public bool IsLoading { get; private set; }
        public AppState State { get; private set; }
        public RoundResult Result { get; private set; }
        public event Action Changed;

        private void Awake()
        {
            if (running != null && running != this)
            {
                Destroy(gameObject);
                return;
            }
            running = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (IsPrimary)
                Enter(AppState.MainMenu);
        }

        private void OnDestroy()
        {
            DetachMatch();
            if (IsPrimary)
                running = null;
        }

        public void OpenSelection()
        {
            if (!IsLoading && State == AppState.MainMenu)
                Enter(AppState.Selection);
        }

        public void StartMatch()
        {
            if (!IsLoading && (State == AppState.Selection || State == AppState.Results) &&
                Application.CanStreamedLevelBeLoaded(matchScene))
                StartCoroutine(Load(matchScene, AppState.Match));
        }

        public void ReturnToMenu()
        {
            if (IsLoading)
                return;
            if (State == AppState.Selection)
                Enter(AppState.MainMenu);
            else if ((State == AppState.Results || State == AppState.Match) &&
                     Application.CanStreamedLevelBeLoaded(bootScene))
                StartCoroutine(Load(bootScene, AppState.MainMenu));
        }

        internal static void AttachMatch(MatchController controller)
        {
            if (running == null || running.State != AppState.Match || controller.gameObject.scene.name != running.matchScene)
                return;

            running.DetachMatch();
            running.match = controller;
            controller.MatchEnded += running.OnMatchEnded;
        }

        private void DetachMatch()
        {
            if (match != null)
                match.MatchEnded -= OnMatchEnded;
            match = null;
        }

        private void OnMatchEnded(RoundResult result)
        {
            DetachMatch();
            Result = result;
            pendingResults = StartCoroutine(ShowResults());
        }

        private IEnumerator ShowResults()
        {
            yield return new WaitForSecondsRealtime(resultsDelay);
            pendingResults = null;
            Enter(AppState.Results);
        }

        private IEnumerator Load(string sceneName, AppState next)
        {
            IsLoading = true;
            DetachMatch();
            if (pendingResults != null)
            {
                StopCoroutine(pendingResults);
                pendingResults = null;
            }
            if (next == AppState.Match)
                Enter(AppState.Match);
            else
                Changed?.Invoke();

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            IsLoading = false;
            Enter(next);
        }

        private void Enter(AppState state)
        {
            State = state;
            Changed?.Invoke();
        }
    }
}
