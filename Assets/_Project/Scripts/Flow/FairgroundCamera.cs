using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.Flow
{
    /// <summary>Fits both fighters during boosted bounces; yields the camera to KO presentation.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class FairgroundCamera : MonoBehaviour
    {
        [SerializeField] private Camera fightCamera;
        [SerializeField] private RoundController round;
        [SerializeField] private FairgroundHazards hazards;
        [SerializeField] private FairgroundDef definition;
        private Vector3 restPosition;
        private float restSize;

        private void Awake()
        {
            restPosition = fightCamera.transform.position;
            restSize = fightCamera.orthographicSize;
        }

        private void LateUpdate()
        {
            if (!round.Running)
            {
                return;
            }
            Vector3 first = round.Player.transform.position;
            Vector3 second = round.Opponent.transform.position;
            float top = Mathf.Max(first.y + round.Player.CharacterHeight, second.y + round.Opponent.CharacterHeight)
                + definition.CameraHeadroom;
            float tiltOffset = -fightCamera.transform.forward.y * Mathf.Abs(restPosition.z);
            float height = Mathf.Max(restPosition.y, top * .5f + tiltOffset);
            float size = Mathf.Max(restSize * hazards.CurrentHalfWidth / definition.HalfWidth, top * .5f + .5f);
            size = Mathf.Max(size, (Mathf.Abs(first.x - second.x) * .5f + definition.CameraHeadroom) / fightCamera.aspect);
            float visibleHalfWidth = size * fightCamera.aspect;
            float centerRange = Mathf.Max(0f, hazards.CurrentHalfWidth - visibleHalfWidth);
            Vector3 target = restPosition;
            target.x = Mathf.Clamp((first.x + second.x) * .5f, -centerRange, centerRange);
            target.y = height;
            // Snap outward fast enough to retain an 18 m/s boosted jump; ease back toward the floor.
            float weight = size > fightCamera.orthographicSize ? 1f :
                1f - Mathf.Exp(-definition.CameraResponse * Time.deltaTime);
            fightCamera.orthographicSize = Mathf.Lerp(fightCamera.orthographicSize, size, weight);
            fightCamera.transform.position = Vector3.Lerp(fightCamera.transform.position, target, weight);
        }
    }
}
