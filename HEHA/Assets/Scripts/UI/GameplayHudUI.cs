using HEHA.Obby.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HEHA.Obby.UI
{
    public class GameplayHudUI : MonoBehaviour
    {
        [SerializeField] Text deathCounterText;

        void OnEnable()
        {
            if (GameOutcomeManager.Instance != null)
                GameOutcomeManager.Instance.DeathCountChanged += HandleDeathCountChanged;

            Refresh();
        }

        void OnDisable()
        {
            if (GameOutcomeManager.Instance != null)
                GameOutcomeManager.Instance.DeathCountChanged -= HandleDeathCountChanged;
        }

        void HandleDeathCountChanged(int deaths, int maxDeaths) => Refresh(deaths, maxDeaths);

        void Refresh()
        {
            if (GameOutcomeManager.Instance != null)
                Refresh(GameOutcomeManager.Instance.DeathCount, GameOutcomeManager.Instance.MaxDeaths);
            else
                Refresh(0, 10);
        }

        void Refresh(int deaths, int maxDeaths)
        {
            if (deathCounterText == null)
                return;

            deathCounterText.text = $"Deaths: {deaths} / {maxDeaths}";
        }
    }
}
