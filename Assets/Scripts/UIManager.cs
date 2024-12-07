using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI reputationText;
    public Image reputationGauge;
    public GameObject levelUpScreen;
    public AudioClip levelUpSound;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        updateMoneyUI();
        updateReputationUI();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateMoneyUI()
    {
        moneyText.text = gameManager.money.ToString();
    }

    public void updateReputationUI()
    {
        reputationText.text = "Lv " + gameManager.reputation.ToString();
        float fillAmount = gameManager.reputationValue / 100f;
        reputationGauge.fillAmount = fillAmount;
    }

    /// <summary>
    /// 레벨 업 화면 열기 
    /// </summary>
    public void ShowLevelUpScreen()
    {
        levelUpScreen.SetActive(true);
        PlayLevelUpSound();
    }

    /// <summary>
    /// 레벨 업 화면 닫기 
    /// </summary>
    public void CloseLevelUpScreen()
    {
        levelUpScreen.SetActive(false);
    }

    private void PlayLevelUpSound()
    {
        audioSource.PlayOneShot(levelUpSound);
    }
}
