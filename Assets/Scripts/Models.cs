using System;
using System.Collections.Generic;

[Serializable]
public class GeoResult
{
    public string name;
    public string country;
    public string admin1; // estado/região
    public double latitude;
    public double longitude;
}

[Serializable]
public class GeoSearchResponse
{
    public List<GeoResult> results;
}

[Serializable]
public class CurrentWeather
{
    public float temperature_2m;
    public int weather_code;
}

[Serializable]
public class DailyForecast
{
    public List<string> time;
    public List<float> temperature_2m_max;
    public List<float> temperature_2m_min;
    public List<int> weather_code;
}

[Serializable]
public class WeatherResponse
{
    public CurrentWeather current;
    public DailyForecast daily;
}

[Serializable]
public class FavoriteCity
{
    public string name;
    public double latitude;
    public double longitude;
}