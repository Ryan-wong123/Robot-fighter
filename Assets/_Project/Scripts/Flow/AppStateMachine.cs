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
        public Texture2D Thumbnail;
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
        private CharacterDef madamePlush;
        public bool HasMadamePlush => madamePlush != null;
        public bool HasPuffinPete => puffinPete != null;
        private CharacterDef selectedOpponent;
        private CharacterDef mrJack;
        public bool HasMrJack => mrJack != null;
        public bool HasSirNoodle => sirNoodle != null;
        public string OpponentName => selectedOpponent != null ? selectedOpponent.DisplayName : "Blobby";
        private MatchRules requestedRules = MatchRules.Default;
        public AIDifficulty Difficulty { get; private set; } = AIDifficulty.Normal;
        public MatchMode Mode { get; private set; }
        public bool BossOpponentSelected => selectedOpponent != null && selectedOpponent.Boss != null;
        public bool RulesLocked => BossOpponentSelected || Mode == MatchMode.Arcade ||
            (StageCount > 0 && GetStage(StageIndex).IsBossStage);
        public MatchRules Rules => requestedRules.Resolve(Mode,
            (selectedOpponent != null && selectedOpponent.Boss != null) || (StageCount > 0 && GetStage(StageIndex).IsBossStage));
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
            madamePlush = Resources.Load<CharacterDef>("Characters/MadamePlush");
            mrJack = Resources.Load<CharacterDef>("Characters/MrJack");
            InstallStages();
            DontDestroyOnLoad(gameObject);
            AudioManager.Ensure(transform);
        }

        private void InstallStages()
        {
            var installed = Resources.LoadAll<StageDef>("Stages");
            var choices = new System.Collections.Generic.List<StageChoice>();
            foreach (var choice in stages ?? System.Array.Empty<StageChoice>())
                if (choice != null && TrainingSandboxRules.IsSelectableArena(choice.Scene))
                {
                    if (choice.Thumbnail == null)
                        choice.Thumbnail = Resources.Load<Texture2D>("StagePreviews/" + choice.Scene);
                    choices.Add(choice);
                }
            foreach (var stage in installed)
            {
                if (!TrainingSandboxRules.IsSelectableArena(stage.SceneName)) continue;
                bool exists = false;
                foreach (var choice in choices)
                {
                    exists |= choice.Scene == stage.SceneName;
                }
                if (!exists)
                {
                    choices.Add(new StageChoice
                    {
                        Name = stage.DisplayName, Scene = stage.SceneName, Hazards = stage.HazardHint,
                        Thumbnail = stage.Thumbnail
                    });
                }
            }
            stages = choices.ToArray();
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
                Difficulty = AIDifficulty.Normal;
                selectedFighter = null;
                selectedOpponent = blobby;
                Enter(AppState.Selection);
            }
        }

        public void SetDifficulty(AIDifficulty difficulty)
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Stage || BossOpponentSelected ||
                difficulty < AIDifficulty.Easy || difficulty > AIDifficulty.Hard)
                return;
            Difficulty = difficulty;
            Changed?.Invoke();
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
        public void SelectMadamePlush() => SelectFighter(madamePlush);

        public void ToggleOpponent()
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Fighter ||
                (sirNoodle == null && puffinPete == null && madamePlush == null && mrJack == null))
                return;
            selectedOpponent = selectedOpponent == blobby ? (sirNoodle ?? puffinPete ?? madamePlush ?? mrJack ?? blobby) :
                selectedOpponent == sirNoodle ? (puffinPete ?? madamePlush ?? mrJack ?? blobby) :
                selectedOpponent == puffinPete ? (madamePlush ?? mrJack ?? blobby) :
                selectedOpponent == madamePlush ? (mrJack ?? blobby) : blobby;
            Changed?.Invoke();
        }

        private void SelectFighter(CharacterDef definition)
        {
            if (IsLoading || State != AppState.Selection || Step != SelectionStep.Fighter || definition == null || definition.Boss != null)
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
            controller.ConfigureDifficulty(running.Difficulty);
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
