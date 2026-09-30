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
    VisualElement favoritesScreen;

    TextField cityInput;
    Button searchButton;
    Button favoritesButton;
    ListView resultsList;

    Button backButton;
    Button favoriteButton;
    Label cityNameLabel;
    Label currentTempLabel;
    Label currentConditionLabel;
    ListView dailyList;

    Button favoritesBackButton;
    ListView favoritesList;
    Button viewFavoriteButton;
    Button removeFavoriteButton;
    FavoriteCity selectedFavorite;

    List<GeoResult> searchResults = new List<GeoResult>();
    DailyForecast currentDaily;
    GeoResult currentCity;
    List<FavoriteCity> favorites = new List<FavoriteCity>();

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        searchScreen = root.Q<VisualElement>("search-screen");
        detailScreen = root.Q<VisualElement>("detail-screen");
        favoritesScreen = root.Q<VisualElement>("favorites-screen");

        cityInput = root.Q<TextField>("city-input");
        searchButton = root.Q<Button>("search-button");
        favoritesButton = root.Q<Button>("favorites-button");
        resultsList = root.Q<ListView>("results-list");

        backButton = root.Q<Button>("back-button");
        favoriteButton = root.Q<Button>("favorite-button");
        cityNameLabel = root.Q<Label>("city-name-label");
        currentTempLabel = root.Q<Label>("current-temp-label");
        currentConditionLabel = root.Q<Label>("current-condition-label");
        dailyList = root.Q<ListView>("daily-list");

        favoritesBackButton = root.Q<Button>("favorites-back-button");
        favoritesList = root.Q<ListView>("favorites-list");
        viewFavoriteButton = root.Q<Button>("view-favorite-button");
        removeFavoriteButton = root.Q<Button>("remove-favorite-button");

        searchButton.clicked += OnSearchClicked;
        favoritesButton.clicked += ShowFavoritesScreen;
        backButton.clicked += ShowSearchScreen;
        favoritesBackButton.clicked += ShowSearchScreen;
        favoriteButton.clicked += OnFavoriteClicked;
        viewFavoriteButton.clicked += OnViewFavoriteClicked;
        removeFavoriteButton.clicked += OnRemoveFavoriteClicked;

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

        favoritesList.makeItem = () => new Label();
        favoritesList.bindItem = (element, i) =>
        {
            (element as Label).text = favorites[i].name;
        };
        favoritesList.selectionChanged += OnFavoriteSelected;

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

    void OnFavoriteSelected(IEnumerable<object> selection)
    {
        foreach (var item in selection)
        {
            if (item is FavoriteCity fav)
                selectedFavorite = fav;
            break;
        }
    }

    IEnumerator FetchWeather(GeoResult city)
    {
        currentCity = city;

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

    void OnFavoriteClicked()
    {
        if (currentCity == null) return;

        bool jaExiste = favorites.Exists(f => f.name == currentCity.name);
        if (!jaExiste)
        {
            favorites.Add(new FavoriteCity
            {
                name = currentCity.name,
                latitude = currentCity.latitude,
                longitude = currentCity.longitude
            });
        }
    }

    void OnViewFavoriteClicked()
    {
        if (selectedFavorite == null) return;
        var city = new GeoResult { name = selectedFavorite.name, latitude = selectedFavorite.latitude, longitude = selectedFavorite.longitude };
        StartCoroutine(FetchWeather(city));
    }

    void OnRemoveFavoriteClicked()
    {
        if (selectedFavorite == null) return;
        favorites.Remove(selectedFavorite);
        selectedFavorite = null;
        favoritesList.itemsSource = favorites;
        favoritesList.RefreshItems();
        favoritesList.ClearSelection();
    }

    void ShowSearchScreen()
    {
        searchScreen.style.display = DisplayStyle.Flex;
        detailScreen.style.display = DisplayStyle.None;
        favoritesScreen.style.display = DisplayStyle.None;
    }

    void ShowDetailScreen()
    {
        searchScreen.style.display = DisplayStyle.None;
        detailScreen.style.display = DisplayStyle.Flex;
        favoritesScreen.style.display = DisplayStyle.None;
    }

    void ShowFavoritesScreen()
    {
        favoritesList.itemsSource = favorites;
        favoritesList.RefreshItems();
        favoritesList.ClearSelection();

        searchScreen.style.display = DisplayStyle.None;
        detailScreen.style.display = DisplayStyle.None;
        favoritesScreen.style.display = DisplayStyle.Flex;
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