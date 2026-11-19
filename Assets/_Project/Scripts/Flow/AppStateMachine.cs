using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WobbleBrawl.Data;
using WobbleBrawl.Core;

namespace WobbleBrawl.Flow
{
    public enum AppState { Boot, MainMenu, Selection, Match, Results }
    public enum SelectionStep { Fighter, Stage }

    [Serializable]
    public sealed class StageChoice
    {
        public string Name;
        public string Scene;
        public string Hazards;
        public bool IsBossStage;
    }

    [DefaultExecutionOrder(-100)]
    public sealed class AppStateMachine : MonoBehaviour
    {
        [SerializeField] private string bootScene = "Boot";
        [SerializeField] private CharacterDef blobby;
        [SerializeField] private StageChoice[] stages;
        [SerializeField, Min(0f)] private float resultsDelay = 1.5f;

        private static AppStateMachine running;
        private MatchController match;
        private Coroutine pendingResults;
        private string matchScene;
        private CharacterDef selectedFighter;
        private CharacterDef sirNoodle;
        private CharacterDef puffinPete;
        public bool HasPuffinPete => puffinPete != null;
        private CharacterDef selectedOpponent;
        public bool HasSirNoodle => sirNoodle != null;
        public string OpponentName => selectedOpponent != null ? selectedOpponent.DisplayName : "Blobby";
        private MatchRules requestedRules = MatchRules.Default;
        public MatchMode Mode { get; private set; }
        public bool RulesLocked => Mode == MatchMode.Arcade ||
            (StageCount > 0 && GetStage(StageIndex).IsBossStage);
        public MatchRules Rules => requestedRules.Resolve(Mode,
            StageCount > 0 && GetStage(StageIndex).IsBossStage);
        public bool IsPrimary => running == this;
        public bool IsLoading { get; private set; }
        public AppState State { get; private set; }
        public SelectionStep Step { get; private set; }
        public int StageCount => stages?.Length ?? 0;
        public int StageIndex { get; private set; }
        public StageChoice GetStage(int index) => stages[index];
        public bool CanStartMatch => StageCount > 0 && selectedFighter != null &&
            Application.CanStreamedLevelBeLoaded(GetStage(StageIndex).Scene);
        public RoundResult Result { get; private set; }
        public int PlayerWins { get; private set; }
        public int OpponentWins { get; private set; }
        public bool IsPaused => MatchPause.IsPaused;
        public bool CanPause => State == AppState.Match && !IsLoading && match != null && match.IsRoundRunning;
        public string MatchSummary => $"{PlayerWins} – {OpponentWins}";
        public event Action Changed;

        private void Awake()
        {
            if (running != null && running != this)
            {
                Destroy(gameObject);
                return;
            }
            running = this;
            sirNoodle = Resources.Load<CharacterDef>("Characters/SirNoodle");
            puffinPete = Resources.Load<CharacterDef>("Characters/PuffinPete");
            DontDestroyOnLoad(gameObject);
            AudioManager.Ensure(transform);
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
            {
                MatchPause.Resume();
                running = null;
            }
        }

        public void TogglePause()
        {
            if (IsPaused)
                MatchPause.Resume();
            else if (CanPause)
                MatchPause.Pause();
            else
                return;
            Changed?.Invoke();
        }

        public void OpenSelection() => OpenSelection(MatchMode.QuickFight);

        public void OpenSelection(MatchMode mode)
        {
            if (!IsLoading && State == AppState.MainMenu)
            {
                Step = SelectionStep.Fighter;
                Mode = mode;
                requestedRules = MatchRules.Default;
                selectedFighter = null;
                selectedOpponent = blobby;
                Enter(AppState.Selection);
            }
        }

        public void SetHazardsEnabled(bool enabled) => SetRules(enabled, Rules.ItemsEnabled);
        public void SetItemsEnabled(bool enabled) => SetRules(Rules.HazardsEnabled, enabled);

        private void SetRules(bool hazards, bool items)
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Stage || RulesLocked)
                return;
            requestedRules = new MatchRules(hazards, items);
            Changed?.Invoke();
        }

        public void SelectBlobby() => SelectFighter(blobby);
        public void SelectSirNoodle() => SelectFighter(sirNoodle);
        public void SelectPuffinPete() => SelectFighter(puffinPete);

        public void ToggleOpponent()
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Fighter ||
                (sirNoodle == null && puffinPete == null))
                return;
            selectedOpponent = selectedOpponent == blobby ? (sirNoodle != null ? sirNoodle : puffinPete) :
                selectedOpponent == sirNoodle && puffinPete != null ? puffinPete : blobby;
            Changed?.Invoke();
        }

        private void SelectFighter(CharacterDef definition)
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Fighter || definition == null)
                return;
            selectedFighter = definition;
            Step = SelectionStep.Stage;
            Changed?.Invoke();
        }

        public void SelectStage(int index)
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Stage ||
                index < 0 || index >= StageCount)
                return;
            StageIndex = index;
            Changed?.Invoke();
        }

        public void StartMatch()
        {
            if (IsLoading)
                return;
            if (State == AppState.Selection && Step == SelectionStep.Stage && CanStartMatch)
            {
                matchScene = GetStage(StageIndex).Scene;
                StartCoroutine(Load(matchScene, AppState.Match));
            }
            else if (State == AppState.Results && Application.CanStreamedLevelBeLoaded(matchScene))
                StartCoroutine(Load(matchScene, AppState.Match));
        }

        public void ReturnToMenu()
        {
            if (IsLoading)
                return;
            if (State == AppState.Selection)
            {
                if (Step == SelectionStep.Stage)
                {
                    Step = SelectionStep.Fighter;
                    Changed?.Invoke();
                }
                else
                    Enter(AppState.MainMenu);
            }
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
            controller.ConfigurePlayer(running.selectedFighter);
            controller.ConfigureOpponent(running.selectedOpponent);
            controller.ConfigureRules(running.Rules);
            controller.MatchEnded += running.OnMatchEnded;
            controller.RoundStarted += running.OnRoundStarted;
            controller.RoundEnded += running.OnRoundEnded;
        }

        private void OnRoundStarted(int _) => Changed?.Invoke();
        private void OnRoundEnded(RoundResult _) => Changed?.Invoke();

        private void DetachMatch()
        {
            if (match != null)
            {
                match.MatchEnded -= OnMatchEnded;
                match.RoundStarted -= OnRoundStarted;
                match.RoundEnded -= OnRoundEnded;
            }
            match = null;
        }

        private void OnMatchEnded(RoundResult result)
        {
            PlayerWins = match.PlayerWins;
            OpponentWins = match.OpponentWins;
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
            MatchPause.Resume();
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
