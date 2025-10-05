using System;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneIntroListener : MonoBehaviour
{
    private MMF_Player _mmfPlayer;
    
    private GameStartManager _gameStartManager;
    private string coverSceneName="Cover";
    private string mainSceneName="Main";
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
        else if (_gameStartManager == null )
        {
            PlayIntroOnce();
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
        if (_mmfPlayer != null)
        {
            _mmfPlayer.PlayFeedbacks();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainSceneName));
        }
        // enabled = false;
    }

    public void ReturnToStart()
    {
        if (_gameStartManager != null) _gameStartManager.BackStartScence();
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(coverSceneName));
    }
}
