using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WeatherAppController : MonoBehaviour
{
    VisualElement root;
    VisualElement searchScreen;
    VisualElement detailScreen;

    TextField cityInput;
    Button searchButton;
    ListView resultsList;

    Button backButton;
    Label cityNameLabel;
    Label currentTempLabel;
    Label currentConditionLabel;
    ListView dailyList;

    List<GeoResult> searchResults = new List<GeoResult>();
    DailyForecast currentDaily;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        searchScreen = root.Q<VisualElement>("search-screen");
        detailScreen = root.Q<VisualElement>("detail-screen");

        cityInput = root.Q<TextField>("city-input");
        searchButton = root.Q<Button>("search-button");
        resultsList = root.Q<ListView>("results-list");

        backButton = root.Q<Button>("back-button");
        cityNameLabel = root.Q<Label>("city-name-label");
        currentTempLabel = root.Q<Label>("current-temp-label");
        currentConditionLabel = root.Q<Label>("current-condition-label");
        dailyList = root.Q<ListView>("daily-list");

        searchButton.clicked += OnSearchClicked;
        backButton.clicked += ShowSearchScreen;

        resultsList.makeItem = () => new Label();
        resultsList.bindItem = (element, i) =>
        {
            var r = searchResults[i];
            (element as Label).text = $"{r.name} — {r.admin1}, {r.country}";
        };
        resultsList.selectionChanged += OnCitySelected;

        dailyList.makeItem = () => new Label();
        dailyList.bindItem = (element, i) =>
        {
            var label = element as Label;
            label.text = $"{currentDaily.time[i]}: {currentDaily.temperature_2m_min[i]:0}°/{currentDaily.temperature_2m_max[i]:0}° — {DescreverClima(currentDaily.weather_code[i])}";
        };

        ShowSearchScreen();
    }

    void OnSearchClicked()
    {
        string city = cityInput.value;
        if (string.IsNullOrWhiteSpace(city)) return;
        StartCoroutine(SearchCity(city));
    }

    IEnumerator SearchCity(string cityName)
    {
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={UnityWebRequest.EscapeURL(cityName)}&count=10&language=pt";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<GeoSearchResponse>(req.downloadHandler.text);
                searchResults = data.results ?? new List<GeoResult>();
                resultsList.itemsSource = searchResults;
                resultsList.RefreshItems();
                resultsList.ClearSelection();
            }
            else
            {
                Debug.LogError("Erro na busca: " + req.error);
            }
        }
    }

    void OnCitySelected(IEnumerable<object> selection)
    {
        foreach (var item in selection)
        {
            if (item is GeoResult city)
                StartCoroutine(FetchWeather(city));
            break;
        }
    }

    IEnumerator FetchWeather(GeoResult city)
    {
        string lat = city.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string lon = city.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                     "&current=temperature_2m,weather_code" +
                     "&daily=temperature_2m_max,temperature_2m_min,weather_code" +
                     "&timezone=auto";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var weather = JsonUtility.FromJson<WeatherResponse>(req.downloadHandler.text);
                currentDaily = weather.daily;

                cityNameLabel.text = city.name;
                currentTempLabel.text = $"{weather.current.temperature_2m:0}°C";
                currentConditionLabel.text = DescreverClima(weather.current.weather_code);

                dailyList.itemsSource = currentDaily.time;
                dailyList.RefreshItems();

                ShowDetailScreen();
            }
            else
            {
                Debug.LogError("Erro ao buscar clima: " + req.error);
            }
        }
    }

    void ShowSearchScreen()
    {
        searchScreen.style.display = DisplayStyle.Flex;
        detailScreen.style.display = DisplayStyle.None;
    }

    void ShowDetailScreen()
    {
        searchScreen.style.display = DisplayStyle.None;
        detailScreen.style.display = DisplayStyle.Flex;
    }

    string DescreverClima(int code)
    {
        if (code == 0) return "Céu limpo";
        if (code <= 3) return "Parcialmente nublado";
        if (code == 45 || code == 48) return "Nevoeiro";
        if (code >= 51 && code <= 67) return "Chuva";
        if (code >= 71 && code <= 77) return "Neve";
        if (code >= 80 && code <= 82) return "Pancadas de chuva";
        if (code >= 95) return "Tempestade";
        return "Condição desconhecida";
    }
}