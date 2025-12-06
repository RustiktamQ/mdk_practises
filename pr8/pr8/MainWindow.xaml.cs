using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace pr8
{
    public class WeatherRow
    {
        public DateTime Time { get; set; }
        public double Temperature { get; set; }
        public int? Pressure { get; set; }
        public int? Humidity { get; set; }
    }

    public class Hourly
    {
        public List<string> time { get; set; }
        public List<double> temperature_2m { get; set; }
        public List<int> relativehumidity_2m { get; set; }
        public List<double> pressure_msl { get; set; }
    }

    public class WeatherApiResponse
    {
        public Hourly hourly { get; set; }
    }

    public partial class MainWindow : Window
    {
        ObservableCollection<WeatherRow> rows = new ObservableCollection<WeatherRow>();

        public MainWindow()
        {
            InitializeComponent();
            WeatherDataGrid.ItemsSource = rows;
            UpdateButton.Click += async (s, e) => await LoadWeather();
            Loaded += async (s, e) => await LoadWeather();
        }

        async Task LoadWeather()
        {
            string url = "https://api.open-meteo.com/v1/forecast?latitude=55.0&longitude=58.0&hourly=temperature_2m,relativehumidity_2m,pressure_msl";
            HttpClient client = new HttpClient();
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync();

            var weather = JsonConvert.DeserializeObject<WeatherApiResponse>(json);

            rows.Clear();
            for (int i = 0; i < weather.hourly.time.Count; i++)
            {
                rows.Add(new WeatherRow
                {
                    Time = DateTime.Parse(weather.hourly.time[i]),
                    Temperature = weather.hourly.temperature_2m[i],
                    Humidity = weather.hourly.relativehumidity_2m?[i],
                    Pressure = (int?)weather.hourly.pressure_msl[i]
                });
            }
        }
    }
}
