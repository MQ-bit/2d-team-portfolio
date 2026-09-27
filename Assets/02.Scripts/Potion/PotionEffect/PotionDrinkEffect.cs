using UnityEngine;

public class PotionDrinkEffect : MonoBehaviour
{
    public bool Apply(PotionData data, GameObject targetObject)
    {
        if (data == null || targetObject == null)
        {
            Debug.LogWarning(
                $"[PotionDrinkEffect] Apply failed: dataMissing={data == null}, " +
                $"targetMissing={targetObject == null}",
                this);
            return false;
        }

        IPotionEffectTarget target = PotionEffectResolver.FindTarget(targetObject.transform);
        if (target == null)
        {
            Debug.LogWarning(
                $"[PotionDrinkEffect] Apply failed: target={targetObject.name} has no IPotionEffectTarget.",
                targetObject);
            return false;
        }

        Debug.Log(
            $"[PotionDrinkEffect] Apply: potion={data.Name}, target={targetObject.name}, " +
            $"healthAmount={data.DrinkEffectAmount}, thirstAmount={data.DrinkThirstRecoveryAmount}",
            targetObject);
        PotionEffectResolver.ApplyDrinkEffect(data, target);
        return true;
    }
}
