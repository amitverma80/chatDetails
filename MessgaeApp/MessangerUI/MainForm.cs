using Agent;

namespace MessangerUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        WhatAppMessagner? Messegner = null;

        bool isLoggedin = false;
        private async void MainForm_Load(object sender, EventArgs e)
        {
            txtMessage.Visible = false;

            txtMobileNumbers.Enabled = btnEmail.Enabled = btnSelectFiles.Enabled = btnSendMessage.Enabled = false;

            textBox1.AppendLine("Starting Application...");

            var result = Validation.ValidateUserMachine();
            if (!result.Item1)
            {
                textBox1.AppendLine($"{result.Item2}");
                return;
            }

            var isError = await this.loadSeleniumDriversAsync();

            if (isError)
            {
                return;
            }

            textBox1.AppendLine("Drivers loaded successfully...");

            textBox1.AppendLine("Starting Whats app on browser and loading QR Code...");

            backgroundWorker1.RunWorkerAsync();
        }

        private async Task<bool> loadSeleniumDriversAsync()
        {
            bool isError = false;
            try
            {
                await this.Invoke(async () =>
                {
                    await Task.Delay(1);

                    Messegner = new WhatAppMessagner(false);
                    Messegner.OnQRReady += Messegner_OnQRReady;
                });
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
                isError = true;
            }

            return isError;
        }

        private void Messegner_OnQRReady(Image qrbmp)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    pbQRCode.Image = qrbmp;
                    textBox1.AppendLine("Scan the QR code using your Whatsapp mobile app to continue login.");
                }));
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isLoggedin)
            {
                Messegner?.Logout();
                Messegner?.Dispose();
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Messegner?.Login();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBox1.AppendLine(e.Error.ToString());
            }

            txtMessage.Visible = true;
            txtMobileNumbers.Enabled = btnEmail.Enabled = btnSelectFiles.Enabled = btnSendMessage.Enabled = true;
            pbQRCode.Visible = false;
            groupBox2.Text = "Type message here";

            isLoggedin = true;
            textBox1.AppendLine("Login Successful.");
        }

        private async void btnSelectFiles_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            try
            {
                if (string.IsNullOrEmpty(txtMobileNumbers.Text.Trim()))
                {
                    textBox1.AppendLine("Mobile numbers cannot be empty.");

                    txtMobileNumbers.Focus();

                    return;
                }

                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Title = "Select Image File",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "Images (*.BMP;*.JPG,*.PNG)|*.BMP;*.JPG;*.PNG;",
                    Multiselect = true
                };



                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    var mobileNumbers = txtMobileNumbers.Text.Trim().Split(Environment.NewLine);

                    foreach (var mobileNumber in mobileNumbers)
                    {
                        if (!string.IsNullOrEmpty(mobileNumber))
                        {
                            foreach (var file in openFileDialog1.FileNames)
                            {
                                var random = new Random();
                                int randomTime = random.Next(5000, 20000);
                                textBox1.AppendLine($"{randomTime} - Time selected");
                                await Task.Delay(randomTime);

                                await Task.Run(() =>
                                {
                                    Messegner?.SendMedia(MediaType.IMAGE_OR_VIDEO, mobileNumber, file, txtMessage.Text);
                                });

                                textBox1.AppendLine($"Mobile Number - {mobileNumber} - image message sent - {Path.GetFileName(file)}.");
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }

        private async void btnSendMessage_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            try
            {
                if (string.IsNullOrEmpty(txtMobileNumbers.Text.Trim()))
                {
                    textBox1.AppendLine("Mobile numbers cannot be empty.");

                    txtMobileNumbers.Focus();

                    return;
                }

                if (string.IsNullOrEmpty(txtMessage.Text.Trim()))
                {
                    textBox1.AppendLine("Message cannot be empty.");

                    txtMessage.Focus();

                    return;
                }

                textBox1.AppendLine("sending text message...");

                var mobileNumbers = txtMobileNumbers.Text.Trim().Split(Environment.NewLine);

                foreach (var mobileNumber in mobileNumbers)
                {
                    if (!string.IsNullOrEmpty(mobileNumber))
                    {
                        var random = new Random();
                        int randomTime = random.Next(5000, 30000);
                        textBox1.AppendLine($"{randomTime} - Time selected");
                        await Task.Delay(randomTime);

                        await Task.Run(() =>
                        {
                            Messegner?.SendMessage(mobileNumber, txtMessage.Text);
                        });

                        textBox1.AppendLine($"Mobile Number - {mobileNumber} - Text message sent.");
                    }
                }


            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }

        private void btnEmail_Click(object sender, EventArgs e)
        {

        }
    }
}
