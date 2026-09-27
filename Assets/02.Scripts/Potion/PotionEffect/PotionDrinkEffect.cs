using UnityEngine;

public sealed class PotionDrinkEffect : MonoBehaviour
{
    [SerializeField] private bool _enableDebugLogs = true;

    public bool Apply(PotionData data, GameObject targetObject)
    {
        if (data == null || targetObject == null)
        {
            LogWarning(
                $"Apply failed: dataMissing={data == null}, targetMissing={targetObject == null}",
                this);
            return false;
        }

        if (!PotionEffectResolver.TryFindTarget(targetObject.transform, out IPotionEffectTarget target))
        {
            LogWarning($"Apply failed: target={targetObject.name} has no IPotionEffectTarget.", targetObject);
            return false;
        }

        if (_enableDebugLogs)
        {
            Debug.Log(
                $"[PotionDrinkEffect] Apply: potion={data.Name}, target={targetObject.name}, " +
                $"healthAmount={data.DrinkEffectAmount}, thirstAmount={data.DrinkThirstRecoveryAmount}",
                targetObject);
        }
        PotionEffectResolver.ApplyDrinkEffect(data, target);
        return true;
    }

    private void LogWarning(string message, Object context)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PotionDrinkEffect] {message}", context);
        }
    }
}
