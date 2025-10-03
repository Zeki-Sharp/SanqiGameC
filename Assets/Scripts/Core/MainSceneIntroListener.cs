using System;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;

public class MainSceneIntroListener : MonoBehaviour
{
    private MMF_Player _mmfPlayer;
    
    private GameStartManager _gameStartManager;

    private void Start()
    {
        if (_mmfPlayer == null)
        {
            _mmfPlayer = this.GetComponent<MMF_Player>();
        }
        _gameStartManager = GameObject.FindObjectOfType<GameStartManager>();
        if (_gameStartManager != null)
        {
            _gameStartManager.OnAnimationComplete.AddListener(PlayIntroOnce);

        }
    }

    private void OnDisable()
    {
        if (_gameStartManager != null)
        {
            _gameStartManager.OnAnimationComplete.RemoveListener(PlayIntroOnce);
        }
    }

    void PlayIntroOnce()
    {
        if (_mmfPlayer != null) _mmfPlayer.PlayFeedbacks();
        // enabled = false;
    }
}
