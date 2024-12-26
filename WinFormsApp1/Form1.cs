using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private IMqttClient _mqttClient; // MQTT клиент
        private MqttClientOptions _mqttOptions; // Настройки клиента
        private System.Windows.Forms.Timer _dataGenerationTimer;
        private double _soilMoisture; // Показатель влажности почвы (в процентах)
        private bool _pumpActive; // Состояние насоса (актуатора)
        private bool _autoMode; // Режим работы: true - автоматический, false - ручной
        private TelegramBotClient _telegramBot; // Telegram Bot
        private string _telegramToken = "токенмбота";
        private long _chatId; // ID чата для отправки сообщений

        public Form1()
        {
            InitializeComponent();
            _soilMoisture = 40; // Начальное значение влажности
            _autoMode = false;  // Режим по умолчанию - ручной
            _pumpActive = false;
            InitializeTimer();            
            InitializeMqttClient(); // Инициализация MQTT клиента
            //EnsureMqttConnection();
            InitializeTelegramBot(); // Инициализация Telegram бота
        }

        private async void InitializeMqttClient()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 1883) // Используем открытый сервер Mosquitto
                .Build();

            // Асинхронно подключаемся к серверу
            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
                Console.WriteLine("MQTT client connected.");
                await SubscribeToTopics(); // Подписываемся на топики после подключения
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to MQTT broker: {ex.Message}");
            }

            // Обработчик получения сообщений
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Received message: {message}");

                // Обработка сообщений для управления насосом и режимом
                if (message == "MANUAL_ON")
                {
                    _pumpActive = true;
                    await SendTelegramMessage("Насос включен.");
                }
                else if (message == "MANUAL_OFF")
                {
                    _pumpActive = false;
                    await SendTelegramMessage("Насос выключен.");
                }
                else if (message == "AUTO_MODE")
                {
                    _autoMode = true;
                    await SendTelegramMessage("Переключен в автоматический режим.");
                }
                else if (message == "MANUAL_MODE")
                {
                    _autoMode = false;
                    await SendTelegramMessage("Переключен в ручной режим.");
                }

                // Обновление состояния UI (для отображения нового состояния)
                UpdateUI();
            };
        }

        private async Task SubscribeToTopics()
        {
            // Подписка на топики для управления насосом и режимом
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("iot/device/control").Build());
        }

        private void InitializeTimer()
        {
            _dataGenerationTimer = new System.Windows.Forms.Timer();
            _dataGenerationTimer.Interval = 1000; // Генерация данных каждые 1 секунду
            _dataGenerationTimer.Tick += GenerateData;
            _dataGenerationTimer.Start();
        }

        private void GenerateData(object sender, EventArgs e)
        {
            // Логика изменения показаний датчиков
            if (_pumpActive)
            {
                _soilMoisture = Math.Min(100, _soilMoisture + 5); // Насос увеличивает влажность почвы
            }
            else
            {
                _soilMoisture = Math.Max(0, _soilMoisture - 2); // Почва высыхает со временем
            }

            // В автоматическом режиме насос включается, если влажность ниже 30%
            if (_autoMode && _soilMoisture < 30)
            {
                _pumpActive = true;
            }
            if (_autoMode && _soilMoisture >= 30)
            {
                _pumpActive = false;
            }

            // Публикуем данные на MQTT сервер
            PublishSensorData();

            UpdateUI(); // Обновляем пользовательский интерфейс
        }

        private async void PublishSensorData()
        {
            if (_mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("iot/device/sensor")
                    .WithPayload($"{{\"soilMoisture\": {_soilMoisture}, \"pumpActive\": {_pumpActive}}}")
                    .Build();

                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published: {message.Payload}");
            }
        }

        private async Task ControlPumpOnMqtt()
        {
            if (_mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("iot/device/control")
                    .WithPayload("MANUAL_ON")
                    .Build();

                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published: {message.Payload}");
            }
        }

        private async Task ControlPumpOffMqtt()
        {
            if (_mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("iot/device/control")
                    .WithPayload("MANUAL_OFF")
                    .Build();

                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published: {message.Payload}");
            }
        }

        private async Task SetAutoModeMqtt()
        {
            if (_mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("iot/device/control")
                    .WithPayload("AUTO_MODE")
                    .Build();

                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published: {message.Payload}");
            }
        }

        private async Task SetManualModeMqtt()
        {
            if (_mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("iot/device/control")
                    .WithPayload("MANUAL_MODE")
                    .Build();

                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published: {message.Payload}");
            }
        }

        


        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateUI));
                return;
            }

            // Обновляем отображение данных
            SoilMoisture.Text = $"Влажность почвы: {_soilMoisture}%";
            PumpStatus.Text = _pumpActive ? "Насос работает" : "Насос не работает";
            ModeStatus.Text = _autoMode ? "Режим: Автоматический" : "Режим: Ручной";
        }

        private async void InitializeTelegramBot()
        {
            _telegramBot = new TelegramBotClient(_telegramToken);

            // Запускаем асинхронный цикл для получения сообщений
            Task.Run(async () => await StartReceivingUpdatesAsync());
        }

        private async Task StartReceivingUpdatesAsync()
        {
            // Получаем обновления для бота
            var offset = 0;
            while (true)
            {
                var updates = await _telegramBot.GetUpdatesAsync(offset);
                foreach (var update in updates)
                {
                    offset = update.Id + 1; // Обновляем смещение

                    if (update.Message != null)
                    {
                        await HandleMessageAsync(update.Message);
                    }
                }

                // Пауза перед следующей проверкой обновлений
                await Task.Delay(1000);
            }
        }

        private async Task HandleMessageAsync(Telegram.Bot.Types.Message message)
        {
            // Обрабатываем полученные команды
            if (message.Text == "/status")
            {
                // Отправка текущего статуса через Telegram
                string status = $"Влажность почвы: {_soilMoisture}%\n" +
                                $"Насос: {(_pumpActive ? "Включен" : "Выключен")}\n" +
                                $"Режим: {(_autoMode ? "Автоматический" : "Ручной")}";
                await _telegramBot.SendTextMessageAsync(message.Chat, status);
            }
            else if (message.Text == "/manual_on")
            {
                //_pumpActive = true;
                await SendTelegramMessage("Насос включен.");
                await ControlPumpOnMqtt(); // Отправляем команду в MQTT для включения насоса
            }
            else if (message.Text == "/manual_off")
            {
                //_pumpActive = false;
                await SendTelegramMessage("Насос выключен.");
                await ControlPumpOffMqtt(); // Отправляем команду в MQTT для выключения насоса
            }
            else if (message.Text == "/auto_mode")
            {
                //_autoMode = true;
                await SendTelegramMessage("Переключен в автоматический режим.");
                await SetAutoModeMqtt(); // Отправляем команду в MQTT для включения автоматического режима
            }
            else if (message.Text == "/manual_mode")
            {
                //_autoMode = false;
                await SendTelegramMessage("Переключен в ручной режим.");
                await SetManualModeMqtt(); // Отправляем команду в MQTT для включения ручного режима
            }
        }


        private async Task SendTelegramMessage(string message)
        {
            if (_chatId != 0)
            {
                await _telegramBot.SendTextMessageAsync(_chatId, message);
            }
        }

        private void btnToggleMode_Click(object sender, EventArgs e)
        {
            // Переключаем режим (автоматический/ручной)
            _autoMode = !_autoMode;

            // В автоматическом режиме насос включается, если влажность почвы ниже 30%
            if (_autoMode && _soilMoisture < 30)
            {
                _pumpActive = true;
            }
        }

        private void btnTogglePump_Click(object sender, EventArgs e)
        {
            // Ручной режим: управляем насосом вручную
            _pumpActive = !_pumpActive;
        }
    }
}
