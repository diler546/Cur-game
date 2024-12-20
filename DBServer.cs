using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;

public class DBServer : MonoBehaviour
{
    private const string ServerUrl = "http://shuler.xn--80ahdri7a.site/UnityServer.php";

    // Ссылки на UI-элементы
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text feedbackText;

    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text levelsFinishedText;

    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject profilePanel;

    public static TMP_Text CoinsText;
    public static TMP_Text DeathsText;
    public static TMP_Text LevelsFinishedText;
    public static GameObject AuthPanel;
    public static GameObject ProfilePanel;
    public static int LoggedInUserId;

    [SerializeField] private TMP_Text name4Text; // Объект текста Name 4
    [SerializeField] private TMP_Text name5Text; // Объект текста Name 4


    private int loggedInUserId;

    private static DBServer instance;

    void Awake()
    {
        // Проверяем, существует ли уже экземпляр DBServer
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Сохраняем объект при смене сцен
        }
        else
        {
            //Destroy(gameObject); // Уничтожаем дублирующиеся объекты
        }
    }

    public static DBServer Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("DBServer instance not found. Make sure it's present on the initial scene.");
            }
            return instance;
        }
    }

    public void CheckUserRegistration()
    {
        // Загрузка
        int id = PlayerPrefs.GetInt("id", 0); // 0 — значение по умолчанию
        Debug.Log(id);
        if (id != 0)
        {
            GetPlayerStats(id); // Загружаем статистику
        }
    }

    private void SwitchToProfile()
    {
        authPanel.SetActive(false);
        profilePanel.SetActive(true);
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string status;
        public int user_id; // ID пользователя, возвращаемый сервером
        public string message;
    }

    private class RegisterResponse
    {
        public string status;
        public string message;
    }

    private void OnLoginSuccess(int userId)
    {
        loggedInUserId = userId; // Сохраняем ID пользователя
        GetPlayerStats(loggedInUserId); // Загружаем статистику
    }

    public void Exit()
    {
        PlayerPrefs.SetInt("id", 0);
        PlayerPrefs.Save();
        loggedInUserId = 0;
    }

    public void OnRegisterButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("Please fill in all fields.");
            return;
        }

        StartCoroutine(RegisterCoroutine(username, password));
    }

    public void OnLoginButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("Please enter username and password.");
            return;
        }

        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "register");
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                RegisterResponse loginResponse = JsonUtility.FromJson<RegisterResponse>(responseText);
                if (loginResponse.status == "success")
                {
                    ShowFeedback(loginResponse.message);
                }
                else
                {
                    ShowFeedback(loginResponse.message);
                }
            }
            else
            {
                ShowFeedback("Error during registration: " + www.error);
            }
        }
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "login");
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log(responseText);
                //ShowFeedback("Login success: " + www.downloadHandler.text);
                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(responseText);
                if (loginResponse.status == "success")
                {
                    PlayerPrefs.SetInt("id", loginResponse.user_id);
                    PlayerPrefs.Save();          
                    OnLoginSuccess(loginResponse.user_id); // Передаем ID пользователя
                }
                else
                {
                    ShowFeedback("Login error: " + loginResponse.message);
                }
            }
            else
            {
                ShowFeedback("Error during login: " + www.error);
            }
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        Debug.Log(message);
    }

    public void GetPlayerStats(int userId)
    {
        StartCoroutine(GetLeaderboard());
        StartCoroutine(CheckAchievements(userId));
        StartCoroutine(GetPlayerStatsCoroutine(userId));
    }

    private IEnumerator GetPlayerStatsCoroutine(int userId)
    {
        string url = "http://shuler.xn--80ahdri7a.site/UnityServer.php";

        WWWForm form = new WWWForm();
        form.AddField("action", "stats");
        form.AddField("user_id", userId);

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log(responseText);
                Debug.Log("Stats received: " + responseText);

                PlayerStatsResponse statsResponse = JsonUtility.FromJson<PlayerStatsResponse>(responseText);

                if (statsResponse.status == "success")
                {
                    DisplayPlayerStats(statsResponse.stats);
                }
                else
                {
                    ShowFeedback("Error retrieving stats: " + statsResponse.message);
                }
            }
            else
            {
                ShowFeedback("Error: " + www.error);
            }
        }
    }

    [System.Serializable]
    private class PlayerStatsResponse
    {
        public string status;
        public PlayerStats stats;
        public string message;
    }

    [System.Serializable]
    private class PlayerStats
    {
        public int CountCoins;
        public int CountDeaths;
        public int CountLevelFinished;
        public float BestTime;
    }

    private void DisplayPlayerStats(PlayerStats stats)
    {
        SwitchToProfile(); // Переключаем панели
        coinsText.text = "Coins: " + stats.CountCoins;
        deathsText.text = "Deaths: " + stats.CountDeaths;
        levelsFinishedText.text = "Levels Finished: " + stats.CountLevelFinished;
    }

    public void SendPlayerStats(int countCoins, int countDeaths, int countLevelFinished, float betterTime)
    {
        StartCoroutine(SendStatsCoroutine(PlayerPrefs.GetInt("id", 0), countCoins, countDeaths, countLevelFinished, betterTime));
    }

    private IEnumerator SendStatsCoroutine(int userId, int countCoins, int countDeaths, int countLevelFinished, float betterTime)
    {
        string url = "http://shuler.xn--80ahdri7a.site/UnityServer.php";

        WWWForm form = new WWWForm();
        form.AddField("action", "poststats");
        form.AddField("user_id", userId);
        form.AddField("count_coins", countCoins);
        form.AddField("count_deaths", countDeaths);
        form.AddField("count_level_finished", countLevelFinished);

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log("Server response: " + responseText);

                try
                {
                    // Обработка ответа сервера
                    if (responseText.Contains("success"))
                    {
                        Debug.Log("Statistics successfully updated!");
                    }
                    else
                    {
                        Debug.LogError("Failed to update stats: " + responseText);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing server response: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error sending stats: " + www.error);
            }
        }
    }

    public IEnumerator CheckAchievements(int idUser)
    {
        string url = "http://shuler.xn--80ahdri7a.site/check_achievements.php";

        WWWForm form = new WWWForm();
        form.AddField("idUser", idUser);

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log("Server response: " + responseText);

                AchievementResponse response = JsonUtility.FromJson<AchievementResponse>(responseText);
                if (response != null && response.status == "success")
                {
                    bool hasAchievementC = false;
                    bool hasAchievementD = false;

                    foreach (var achievement in response.achievements)
                    {
                        if (achievement.name == "100 Coins Collector") // Сравнение с названием нужной ачивки
                        {
                            hasAchievementC = true;
                        }
                        if (achievement.name == "10 Deaths") // Сравнение с названием нужной ачивки
                        {
                            hasAchievementD = true;
                        }

                    }

                    // Изменение цвета текста в зависимости от наличия ачивки
                    if (hasAchievementC)
                    {
                        name4Text.color = Color.green; // Зеленый цвет
                    }
                    else
                    {
                        name4Text.color = Color.red; // Красный цвет
                    }

                    // Изменение цвета текста в зависимости от наличия ачивки
                    if (hasAchievementD)
                    {
                        name5Text.color = Color.green; // Зеленый цвет
                    }
                    else
                    {
                        name5Text.color = Color.red; // Красный цвет
                    }
                }
                else
                {
                    Debug.LogError("Invalid response or no achievements found.");
                }

            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }


    [System.Serializable]
    public class Achievement
    {
        public int id;
        public string name;
        public string description;
    }

    [System.Serializable]
    public class AchievementResponse
    {
        public string status;
        public Achievement[] achievements;
    }

    private string leaderboardUrl = "http://shuler.xn--80ahdri7a.site/getLeaderboard.php";

    [SerializeField] private GameObject leaderboardContent; // Контейнер для таблицы
    [SerializeField] private GameObject leaderboardTemplate; // Шаблон строки

    IEnumerator GetLeaderboard()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(leaderboardUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                LeaderboardResponse leaderboardData = JsonUtility.FromJson<LeaderboardResponse>(jsonResponse);
                Debug.Log(leaderboardData.leaderboard);

                if (leaderboardData.status == "success")
                {
                    PopulateLeaderboard(leaderboardData.leaderboard);
                }
                else
                {
                    Debug.LogError("Ошибка загрузки рейтинга: " + leaderboardData.message);
                }
            }
            else
            {
                Debug.LogError("Ошибка запроса: " + request.error);
            }
        }
    }

    void PopulateLeaderboard(List<LeaderboardEntry> leaderboard)
    {
        foreach (Transform child in leaderboardContent.transform)
        {
            Destroy(child.gameObject); // Удаляем старые записи перед добавлением новых
        }

        int rank = 1; // Переменная для хранения места пользователя
        foreach (var entry in leaderboard)
        {
            GameObject newEntry = Instantiate(leaderboardTemplate, leaderboardContent.transform);
            newEntry.SetActive(true);

            TMP_Text[] texts = newEntry.GetComponentsInChildren<TMP_Text>();
            texts[0].text = rank.ToString(); // Место в рейтинге
            texts[1].text = entry.username; // Имя пользователя
            texts[2].text = entry.CountCoins.ToString(); // Монеты
            texts[3].text = entry.CountDeaths.ToString(); // Смерти
            texts[4].text = entry.CountLevelFinished.ToString(); // Уровни
            texts[5].text = entry.TotalScore.ToString(); // Общий результат

            // Чередование цветов
            Image backgroundImage = newEntry.GetComponent<Image>();
            if (rank % 2 == 0) // Четная строка
            {
                backgroundImage.color = new Color(0.9f, 0.9f, 0.9f); // Светло-серый
            }
            else // Нечетная строка
            {
                backgroundImage.color = Color.white; // Белый
            }

            rank++; // Увеличиваем место
        }
    }



    [System.Serializable]
    public class LeaderboardResponse
    {
        public string status;
        public string message;
        public List<LeaderboardEntry> leaderboard;
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string username;
        public int CountCoins;
        public int CountDeaths;
        public int CountLevelFinished;
        public int TotalScore;
    }

}

