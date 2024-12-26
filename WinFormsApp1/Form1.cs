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
        private IMqttClient _mqttClient; // MQTT ������
        private MqttClientOptions _mqttOptions; // ��������� �������
        private System.Windows.Forms.Timer _dataGenerationTimer;
        private double _soilMoisture; // ���������� ��������� ����� (� ���������)
        private bool _pumpActive; // ��������� ������ (���������)
        private bool _autoMode; // ����� ������: true - ��������������, false - ������
        private TelegramBotClient _telegramBot; // Telegram Bot
        private string _telegramToken = "7714274821:AAGhmkMCCdXuy85HFkGbafPbljZeu52HJUw";
        private long _chatId; // ID ���� ��� �������� ���������

        public Form1()
        {
            InitializeComponent();
            _soilMoisture = 40; // ��������� �������� ���������
            _autoMode = false;  // ����� �� ��������� - ������
            _pumpActive = false;
            InitializeTimer();            
            InitializeMqttClient(); // ������������� MQTT �������
            //EnsureMqttConnection();
            InitializeTelegramBot(); // ������������� Telegram ����
        }

        private async void InitializeMqttClient()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 1883) // ���������� �������� ������ Mosquitto
                .Build();

            // ���������� ������������ � �������
            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
                Console.WriteLine("MQTT client connected.");
                await SubscribeToTopics(); // ������������� �� ������ ����� �����������
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to MQTT broker: {ex.Message}");
            }

            // ���������� ��������� ���������
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Received message: {message}");

                // ��������� ��������� ��� ���������� ������� � �������
                if (message == "MANUAL_ON")
                {
                    _pumpActive = true;
                    await SendTelegramMessage("����� �������.");
                }
                else if (message == "MANUAL_OFF")
                {
                    _pumpActive = false;
                    await SendTelegramMessage("����� ��������.");
                }
                else if (message == "AUTO_MODE")
                {
                    _autoMode = true;
                    await SendTelegramMessage("���������� � �������������� �����.");
                }
                else if (message == "MANUAL_MODE")
                {
                    _autoMode = false;
                    await SendTelegramMessage("���������� � ������ �����.");
                }

                // ���������� ��������� UI (��� ����������� ������ ���������)
                UpdateUI();
            };
        }

        private async Task SubscribeToTopics()
        {
            // �������� �� ������ ��� ���������� ������� � �������
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("iot/device/control").Build());
        }

        private void InitializeTimer()
        {
            _dataGenerationTimer = new System.Windows.Forms.Timer();
            _dataGenerationTimer.Interval = 1000; // ��������� ������ ������ 1 �������
            _dataGenerationTimer.Tick += GenerateData;
            _dataGenerationTimer.Start();
        }

        private void GenerateData(object sender, EventArgs e)
        {
            // ������ ��������� ��������� ��������
            if (_pumpActive)
            {
                _soilMoisture = Math.Min(100, _soilMoisture + 5); // ����� ����������� ��������� �����
            }
            else
            {
                _soilMoisture = Math.Max(0, _soilMoisture - 2); // ����� �������� �� ��������
            }

            // � �������������� ������ ����� ����������, ���� ��������� ���� 30%
            if (_autoMode && _soilMoisture < 30)
            {
                _pumpActive = true;
            }
            if (_autoMode && _soilMoisture >= 30)
            {
                _pumpActive = false;
            }

            // ��������� ������ �� MQTT ������
            PublishSensorData();

            UpdateUI(); // ��������� ���������������� ���������
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

            // ��������� ����������� ������
            SoilMoisture.Text = $"��������� �����: {_soilMoisture}%";
            PumpStatus.Text = _pumpActive ? "����� ��������" : "����� �� ��������";
            ModeStatus.Text = _autoMode ? "�����: ��������������" : "�����: ������";
        }

        private async void InitializeTelegramBot()
        {
            _telegramBot = new TelegramBotClient(_telegramToken);

            // ��������� ����������� ���� ��� ��������� ���������
            Task.Run(async () => await StartReceivingUpdatesAsync());
        }

        private async Task StartReceivingUpdatesAsync()
        {
            // �������� ���������� ��� ����
            var offset = 0;
            while (true)
            {
                var updates = await _telegramBot.GetUpdatesAsync(offset);
                foreach (var update in updates)
                {
                    offset = update.Id + 1; // ��������� ��������

                    if (update.Message != null)
                    {
                        await HandleMessageAsync(update.Message);
                    }
                }

                // ����� ����� ��������� ��������� ����������
                await Task.Delay(1000);
            }
        }

        private async Task HandleMessageAsync(Telegram.Bot.Types.Message message)
        {
            // ������������ ���������� �������
            if (message.Text == "/status")
            {
                // �������� �������� ������� ����� Telegram
                string status = $"��������� �����: {_soilMoisture}%\n" +
                                $"�����: {(_pumpActive ? "�������" : "��������")}\n" +
                                $"�����: {(_autoMode ? "��������������" : "������")}";
                await _telegramBot.SendTextMessageAsync(message.Chat, status);
            }
            else if (message.Text == "/manual_on")
            {
                //_pumpActive = true;
                await SendTelegramMessage("����� �������.");
                await ControlPumpOnMqtt(); // ���������� ������� � MQTT ��� ��������� ������
            }
            else if (message.Text == "/manual_off")
            {
                //_pumpActive = false;
                await SendTelegramMessage("����� ��������.");
                await ControlPumpOffMqtt(); // ���������� ������� � MQTT ��� ���������� ������
            }
            else if (message.Text == "/auto_mode")
            {
                //_autoMode = true;
                await SendTelegramMessage("���������� � �������������� �����.");
                await SetAutoModeMqtt(); // ���������� ������� � MQTT ��� ��������� ��������������� ������
            }
            else if (message.Text == "/manual_mode")
            {
                //_autoMode = false;
                await SendTelegramMessage("���������� � ������ �����.");
                await SetManualModeMqtt(); // ���������� ������� � MQTT ��� ��������� ������� ������
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
            // ����������� ����� (��������������/������)
            _autoMode = !_autoMode;

            // � �������������� ������ ����� ����������, ���� ��������� ����� ���� 30%
            if (_autoMode && _soilMoisture < 30)
            {
                _pumpActive = true;
            }
        }

        private void btnTogglePump_Click(object sender, EventArgs e)
        {
            // ������ �����: ��������� ������� �������
            _pumpActive = !_pumpActive;
        }
    }
}
