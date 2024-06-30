using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private BackgroundWorker backgroundWorker;
        private Random random = new Random();
        private string selectedFolderPath;
        private DateTime startTime;
        private System.Windows.Forms.Timer timer;
        private int testDurationSeconds;

        public Form1()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
            StopButton.Enabled = false;
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += backgroundWorker1_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(productIdTextBox.Text))
            {
                MessageBox.Show("Введите наименование продукта");
                return;
            }

            if (testComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите тест");
                return;
            }

            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                MessageBox.Show("Пожалуйста, выберите папку для сохранения результатов теста");
                return;
            }

            testDurationSeconds = GetTestDuration(testComboBox.SelectedIndex);

            statusLabel.Text = "Тестирование...";
            StartButton.Enabled = false;
            StopButton.Enabled = true;
            progressBar.Minimum = 0;
            progressBar.Maximum = testDurationSeconds;
            progressBar.Value = 0;
            startTime = DateTime.Now;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += Timer_Tick;
            timer.Start();

            backgroundWorker.RunWorkerAsync(testComboBox.SelectedIndex);
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
                statusLabel.Text = "Тест отменен";
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int testIndex = (int)e.Argument;

            for (int i = 0; i < testDurationSeconds; i++)
            {
                Thread.Sleep(1000);

                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                backgroundWorker.ReportProgress((i + 1) * 100 / testDurationSeconds);
            }

            bool isSuccess = random.Next(2) == 0;
            string errorMessage = isSuccess ? string.Empty : "Произошла случайная ошибка.";

            var result = new TestResult
            {
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Data = GenerateTestData(testIndex)
            };

            e.Result = result;
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage * progressBar.Maximum / 100;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StartButton.Enabled = true;
            StopButton.Enabled = false;
            timer.Stop();
            progressBar.Value = progressBar.Maximum; // Проверка заполненость шаклы прогресс бара

            if (e.Cancelled)
            {
                statusLabel.Text = "Тест отменен.";
                progressBar.Value = 0;
            }
            else if (e.Error != null)
            {
                statusLabel.Text = "Тест провален: " + e.Error.Message;
            }
            else
            {
                var result = (TestResult)e.Result;
                SaveResultToFile(result);
                statusLabel.Text = "Тест пройден успешно.";
                progressBar.Value = 0;
            }
        }

        private dynamic GenerateTestData(int testIndex)
        {
            switch (testIndex)
            {
                case 0:
                    return new { Data1 = random.Next(100), Data2 = DateTime.Now };
                case 1:
                    return new { Data1 = random.NextDouble(), Data2 = Guid.NewGuid() };
                case 2:
                    return new { Data1 = random.Next(1, 10), Data2 = "Тест", Data3 = random.Next(1000) };
                default:
                    return null;
            }
        }

        private void SaveResultToFile(TestResult result)
        {
            string filePath = Path.Combine(selectedFolderPath, productIdTextBox.Text + ".txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Success: " + result.IsSuccess);
                if (!result.IsSuccess)
                {
                    writer.WriteLine("Error: " + result.ErrorMessage);
                }
                writer.WriteLine("Data: " + result.Data);
            }
        }

        private int GetTestDuration(int testIndex)
        {
            switch (testIndex)
            {
                case 0: return 10;  // Тест 1 10 сек
                case 1: return 20;  // Тест 2 20 сек
                case 2: return 30;  // Тест 3 30 сек
                default: return 10;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan timeSpan = DateTime.Now - startTime;
            timeLabel.Text = "Время: " + timeSpan.ToString(@"hh\:mm\:ss");

            if (backgroundWorker.IsBusy)
            {
                int progress = (int)((timeSpan.TotalSeconds / testDurationSeconds) * progressBar.Maximum);
                progressBar.Value = Math.Min(progress, progressBar.Maximum);
            }
        }

        private void selectFolderButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = folderBrowserDialog.SelectedPath;
                MessageBox.Show("Выбранная папка для сохранения результатов: " + selectedFolderPath);
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            testComboBox.Items.AddRange(new string[] { "Тест 1", "Тест 2", "Тест 3" });
        }
    }

    public class TestResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public dynamic Data { get; set; }
    }
}



