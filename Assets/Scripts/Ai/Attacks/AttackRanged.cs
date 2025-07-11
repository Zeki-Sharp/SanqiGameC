using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack with ranged weapon
/// </summary>
public class AttackRanged : Attack
{
    // Prefab for arrows
    public GameObject arrowPrefab;
    // From this position arrows will fired
    public Transform firePoint;

    // Animation controller for this AI
	private Animator anim;
    // Counter for cooldown calculation
    private float cooldownCounter;

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
		anim = GetComponentInParent<Animator>();
        cooldownCounter = cooldown;
        Debug.Assert(arrowPrefab && firePoint, "Wrong initial parameters");
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    void FixedUpdate()
    {
        if (cooldownCounter < cooldown)
        {
			cooldownCounter += Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Attack the specified target if cooldown expired
    /// </summary>
    /// <param name="target">Target.</param>
	public override void TryAttack(Transform target)
    {
        if (cooldownCounter >= cooldown)
        {
            cooldownCounter = 0f;
            Fire(target);
        }
    }

    /// <summary>
    /// Make ranged attack
    /// </summary>
    /// <param name="target">Target.</param>
	public override void Fire(Transform target)
    {
        if (target != null)
        {
            // Create arrow
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            IBullet bullet = arrow.GetComponent<IBullet>();
            bullet.SetDamage(damage);
            bullet.Fire(target);
			// If unit has animator
			if (anim != null && anim.runtimeAnimatorController != null)
			{
				// Search for clip
				foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
				{
					if (clip.name == "Attack")
					{
						// Play animation
						anim.SetTrigger("attack");
						break;
					}
				}
			}
			// Play sound effect
			if (sfx != null && AudioManager.instance != null)
			{
				AudioManager.instance.PlayAttack(sfx);
			}
        }
    }
}
