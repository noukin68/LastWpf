using Hardcodet.Wpf.TaskbarNotification;
using SocketIOClient;
using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfApp1.Classes;
using WpfApp1.Pages;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Media;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private KeyboardLockManager keyboardLockManager = new KeyboardLockManager();

        private SocketIO socket;
        private DispatcherTimer timer;
        private Questionnaire questionnaire;
        private int currentQuestionIndex;
        private int correctAnswers;
        private TaskbarIcon taskbarIcon;
        private string uid;

        private void LockKeyboard() { keyboardLockManager.LockKeyboard(); }
        private void UnlockKeyboard() { keyboardLockManager.UnlockKeyboard(); }

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
            Closing += Window_Closing;
            uid = UIDGenerator.GenerateUID();
            uidText.Text = "UID: " + uid;
            SaveUidToServerAsync(uid);
            SetHighPriority();
            UpdateAppStatusAsync(true);
        }

        private async void UpdateAppStatusAsync(bool running)
        {
            using (HttpClient client = new HttpClient())
            {
                var data = new { running };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://62.217.182.138:3000/updateAppStatus", content);
                response.EnsureSuccessStatusCode();
            }
        }

        private void Initialize()
        {
            ShowSplashScreen();
            ConnectToServer(true);
            InitializeSocket();
            questionnaire = new Questionnaire();
            InitializeTaskbarIcon();
            HideToTray();
        }

        private void SetHighPriority()
        {
            Process currentProcess = Process.GetCurrentProcess();
            if (currentProcess != null) { currentProcess.PriorityClass = ProcessPriorityClass.High; }
        }

        private void ShowSplashScreen() { Pages.SplashScreen splashScreen = new Pages.SplashScreen(); splashScreen.ShowDialog(); }

        private void InitializeTaskbarIcon()
        {
            taskbarIcon = new TaskbarIcon { Icon = Properties.Resources.icon, ToolTipText = "Родительский контроль" };
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_DoubleClick;
        }

        private void TaskbarIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Show();
            this.Activate();
            taskbarIcon.Visibility = Visibility.Collapsed;
        }

        private void HideToTray() { this.Hide(); taskbarIcon.Visibility = Visibility.Visible; }

        private async void ShowQuestion(int index)
        {
            LockKeyboard();
            if (index < questionnaire.Questions.Count)
            {
                Question question = questionnaire.Questions[index];
                textBlock.Text = question.Text;
                answerStackPanel.Children.Clear();
                StackPanel buttonStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                foreach (string option in question.Options)
                {
                    Button button = new Button
                    {
                        Content = option,
                        Style = (Style)FindResource("ColoredButtonStyle"),
                        Tag = option,
                        Width = 450
                    };
                    button.Click += AnswerButton_Click;
                    buttonStackPanel.Children.Add(button);
                }
                answerStackPanel.Children.Add(buttonStackPanel);
            }
            else
            {
                textBlock.Text = $"Тест завершен и уведомление было отправлено";
                HttpClient client = new HttpClient();
                var response = await client.GetAsync("http://62.217.182.138:3000/notify");
                answerStackPanel.Children.Clear();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) { HideToTray(); }
            else if (WindowState == WindowState.Normal) { taskbarIcon.Visibility = Visibility.Collapsed; }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (timer != null && timer.IsEnabled) { e.Cancel = true; MessageBox.Show("Пожалуйста, дождитесь завершения таймера."); }
            else { e.Cancel = true; HideToTray(); }
            base.OnClosing(e);
        }

        private async void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (currentQuestionIndex >= 0 && currentQuestionIndex < questionnaire.Questions.Count)
            {
                Question question = questionnaire.Questions[currentQuestionIndex];
                int selectedOptionIndex = question.Options.IndexOf(button.Tag.ToString());

                foreach (UIElement child in answerStackPanel.Children)
                {
                    if (child is StackPanel buttonStackPanel)
                    {
                        foreach (UIElement innerChild in buttonStackPanel.Children)
                        {
                            if (innerChild is Button answerButton)
                            {
                                answerButton.IsEnabled = false;
                            }
                        }
                    }
                }

                bool isCorrect = selectedOptionIndex == question.CorrectIndex;
                if (isCorrect)
                {
                    button.Background = Brushes.Green;
                    correctAnswers++;

                    if (correctAnswers > 5) // Check if 5 consecutive correct answers
                    {
                        currentQuestionIndex = 0;
                        correctAnswers = 0;
                        ShowInitialWindowStateAsync();
                        return; // Exit the method
                    }
                }
                else
                {
                    button.Background = Brushes.Red;
                    correctAnswers = 0; // Reset consecutive correct answers count
                }

                await Task.Delay(1500);

                foreach (UIElement child in answerStackPanel.Children)
                {
                    if (child is StackPanel buttonStackPanel)
                    {
                        foreach (UIElement innerChild in buttonStackPanel.Children)
                        {
                            if (innerChild is Button answerButton)
                            {
                                answerButton.IsEnabled = true;
                            }
                        }
                    }
                }

                currentQuestionIndex++;
                ShowQuestion(currentQuestionIndex);
            }
        }

        private async void ShowInitialWindowStateAsync()
        {
            HttpClient client = new HttpClient();
            await client.GetAsync("http://62.217.182.138:3000/restartTimer");
            WindowState = WindowState.Normal;
            HideToTray();
            answerStackPanel.Children.Clear();
            textBlock.Text = "";
            currentQuestionIndex = 0;
        }


        private async void ConnectToServer(bool connect)
        {
            using (HttpClient client = new HttpClient())
            {
                string action = connect ? "connect" : "disconnect";
                Uri url = new Uri($"http://62.217.182.138:3000?action={action}");
                await client.GetAsync(url);
                Console.WriteLine(connect ? "Connected" : "Disconnected");
            }
        }

        private void InitializeSocket()
        {
            socket = new SocketIO("http://62.217.182.138:3000");
            socket.On("time-received", (response) => { int timeInSeconds = response.GetValue<int>(); Dispatcher.Invoke(() => { StartTimer(timeInSeconds); }); });
            //socket.On("uid-authorized", (data) => { Dispatcher.Invoke(() => { AuthStatusText("Соединение установлено"); UpdateConnectionStatusIcon(true); }); });
            //socket.OnDisconnected += (sender, e) => { Dispatcher.Invoke(() => { AuthStatusText("Соединение разорвано"); UpdateConnectionStatusIcon(false); }); };
            socket.On("continue-work", (data) => { HandleAppMinimize(); });
            socket.On("finish-work", (data) => { HandleAppFinish(); });
            socket.ConnectAsync();
        }

        private void HandleAppMinimize() { this.Dispatcher.Invoke(() => { WindowState = WindowState.Normal; UnlockKeyboard(); textBlock.Text = ""; HideToTray(); }); }
        private void HandleAppFinish() { this.Dispatcher.Invoke(() => { UnlockKeyboard(); System.Diagnostics.Process.Start("shutdown", "/s /t 0"); }); }

        /*private void AuthStatusText(string text) { AuthText.Text = text; }

        private void UpdateConnectionStatusIcon(bool isConnected)
        {
            if (isConnected) { ConnectionStatusIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.SmartphoneLink; }
            else { ConnectionStatusIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.SmartphoneLinkOff; }
        } */

        private void StartTimer(int timeInSeconds)
        {
            if (timer != null && timer.IsEnabled) { timer.Stop(); }
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            int remainingTime = timeInSeconds;
            timer.Tick += (sender, e) => {
                remainingTime--;
                if (remainingTime < 0)
                {
                    this.Show();
                    this.Activate();
                    timer.Stop();
                    UpdateTextBlock("Время вышло!");
                    socket.EmitAsync("timer-finished");
                    currentQuestionIndex = 0;
                    correctAnswers = 0;
                    ShowQuestion(currentQuestionIndex);
                    WindowState = WindowState.Maximized;
                    Topmost = true;
                }
            };
            timer.Start();
        }

        private void UpdateTextBlock(string text) { textBlock.Text = text; }
        protected override void OnClosed(EventArgs e) { ConnectToServer(false); base.OnClosed(e); }
        public MainWindow(IntPtr hWnd) : this() { WindowInteropHelper helper = new WindowInteropHelper(this); helper.Owner = hWnd;}
        private void Window_StateChanged(object sender, EventArgs e) { if (WindowState == WindowState.Minimized) { WindowState = WindowState.Normal; Topmost = true; } }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            HideToTray();
        }
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { try { DragMove(); } catch (Exception) {} }
        private void MinimizeBtn(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void CloseBtn(object sender, RoutedEventArgs e) { Close(); UpdateAppStatusAsync(false); }

        private async void SaveUidToServerAsync(string uid)
        {
            using (HttpClient client = new HttpClient())
            {
                string serverUrl = "http://localhost:3000/saveUid";
                var content = new StringContent($"{{ \"uid\": \"{uid}\" }}", Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = await client.PostAsync(serverUrl, content);
                    response.EnsureSuccessStatusCode();
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Ответ сервера: " + responseContent);
                    if (responseContent == "UID уже существует") { Console.WriteLine("UID уже был сохранен на сервере"); }
                }
                catch (HttpRequestException ex) { Console.WriteLine("Ошибка HTTP запроса: " + ex.Message); }
            }
        }

        private async void PackIcon_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.SetText(uid);
            copiedTextBlock.Visibility = Visibility.Visible;
            await Task.Delay(1000);
            copiedTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
