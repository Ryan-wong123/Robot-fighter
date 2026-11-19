using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Keeps the extended test floor safe without changing real arena bounds.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TrainingSandbox : MonoBehaviour
    {
        [SerializeField] private TrainingSandboxLayout layout;
        [SerializeField] private Fighter[] fighters;
        [SerializeField] private Camera fightCamera;
        private bool isTraining;
        private void Awake()
        {
            isTraining = TrainingSandboxRules.AllowsShortcuts(gameObject.scene.name);
            if (!isTraining) return;
            foreach (var fighter in fighters) fighter.SetTrainingBounds(-layout.HalfWidth, layout.HalfWidth);
        }
        private void FixedUpdate()
        {
            if (!isTraining) return;
            foreach (var fighter in fighters) fighter.ConstrainToStage(-layout.HalfWidth, layout.HalfWidth);
        }

        private void LateUpdate()
        {
            if (fightCamera == null || !isTraining ||
                (!fighters[0].CanInteractWithItems && !fighters[1].CanInteractWithItems)) return;
            fightCamera.orthographicSize = Mathf.Max(layout.CameraSize, (layout.HalfWidth + 1f) / fightCamera.aspect);
        }

    }
}
