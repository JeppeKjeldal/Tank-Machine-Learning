using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    [SerializeField] private TMP_Text playerPointsText;
    [SerializeField] private TMP_Text enemyPointsText;

    private int playerPoints;
    private int enemyPoints;

    public static PointsManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerPointsText.text = "Player: " + playerPoints;
        enemyPointsText.text = "Enemy: " + enemyPoints;
    }

    public void AddPlayerPoints()
    {
        playerPoints++;
        UpdateUI();
    }

    public void AddEnemyPoints()
    {
        enemyPoints++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        playerPointsText.text = "Player: " + playerPoints;
        enemyPointsText.text = "Enemy: " + enemyPoints;
    }
}
