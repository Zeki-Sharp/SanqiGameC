using UnityEngine;

public interface IBulletEffectDispatcher
{
    void DispatchEffect(GameObject target, GameObject owner);
}

public class BulletEffectDispatcher : MonoBehaviour, IBulletEffectDispatcher
{
    public EffectData effectData;
    public void DispatchEffect(GameObject target, GameObject owner)
    {
        var controller = target.GetComponent<EffectController>();
        if (controller != null && effectData != null)
        {
            controller.AddEffect(effectData);
        }
    }
} 