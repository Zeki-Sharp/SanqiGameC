using System.Collections.Generic;
using UnityEngine;

public class EffectController : MonoBehaviour
{
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    public void AddEffect(EffectData effectData)
    {
        var effect = new ActiveEffect(effectData);
        activeEffects.Add(effect);
        effect.OnApply(this.gameObject);
    }

    public void RemoveEffect(string effectName)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Data.effectName == effectName)
            {
                activeEffects[i].OnRemove(this.gameObject);
                activeEffects.RemoveAt(i);
            }
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].UpdateEffect(this.gameObject, deltaTime))
            {
                activeEffects[i].OnRemove(this.gameObject);
                activeEffects.RemoveAt(i);
            }
        }
    }
}

[System.Serializable]
public class EffectData
{
    public string effectName;
    public float modifier;
    public float duration;
    public GameObject effectFx;
    public float effectFxDuration;
    public AudioClip sfx;
}

public class ActiveEffect
{
    public EffectData Data { get; private set; }
    private float timer;
    private float originalSpeed;
    public ActiveEffect(EffectData data)
    {
        Data = data;
        timer = data.duration;
    }
    public void OnApply(GameObject target)
    {
        if (Data.effectName == "Speed")
        {
            var move = target.GetComponent<EnemyController>();
            if (move != null)
            {
                Debug.Log("Speed Effect Applied");
                originalSpeed = move.MoveSpeed;
                move.MoveSpeed += Data.modifier;
            }
        }
        // 可扩展：播放特效、音效等
    }
    public void OnRemove(GameObject target)
    {
        if (Data.effectName == "Speed")
        {
            var move = target.GetComponent<EnemyController>();
            if (move != null)
            {
                move.MoveSpeed = originalSpeed;
            }
        }
        // 可扩展：移除特效、还原属性等
    }
    public bool UpdateEffect(GameObject target, float deltaTime)
    {
        timer -= deltaTime;
        // 可扩展：持续效果逻辑
        return timer <= 0f;
    }
} 