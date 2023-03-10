using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Image = System.Drawing.Image;

namespace Agent
{
    public class WhatAppMessagner
    {
        private const string BASE_URL = "https://web.whatsapp.com";
        private readonly string codebase = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
        private readonly IWebDriver driver;
        private string handle;

        public bool IsDisposed { get; set; } = false;
        public delegate void OnDisposedEventHandler();
        public event OnDisposedEventHandler OnDisposed;

        public delegate void OnQRReadyEventHandler(Image qrbmp);
        public event OnQRReadyEventHandler OnQRReady;

        public WhatAppMessagner(bool hideWindow = false)
        {
            try
            {
                var options = new ChromeOptions()
                {
                    LeaveBrowserRunning = false,
                    UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
                };
                options.AddArgument($"--user-data-dir={codebase.Replace("\\","\\\\")}\\\\UserData");
                if (hideWindow) {
                    options.AddArgument("--headless");
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--window-size=1920,1080");
                    options.AddArgument("no-sandbox");
                    options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
                }
                var chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                driver = new ChromeDriver(chromeDriverService, options);
                driver.Manage().Window.Minimize();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public void Login(uint login_timeout = 100)
        {
            try
            {
                driver.Url = BASE_URL;

                foreach (var handle in driver.WindowHandles)
                {
                    if (handle != null && !handle.Equals(driver.CurrentWindowHandle))
                    {
                        driver.SwitchTo().Window(handle);
                        driver.Close();
                    }
                }

                handle = driver.CurrentWindowHandle;

                WaitForQRAndLogin(login_timeout);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }


        private void WaitForQRAndLogin(uint login_timeout)
        {
            var foundQR = false;
            new WebDriverWait(driver, TimeSpan.FromSeconds(login_timeout)).Until(x => {
                if (!CheckWindowState(false))
                    return true;

                var elms = x.FindElements(By.CssSelector("[data-testid='input-placeholder']"));
                if (elms.Count() > 0)
                    return true;

                if (!foundQR)
                {
                    elms = x.FindElements(By.CssSelector("canvas"));
                    if (elms.Count() > 0)
                    {
                        var qrcanvas = elms.First();
                        var qrbmp = GetQRCodeAsImage(qrcanvas);
                        OnQRReady?.Invoke(qrbmp);
                        foundQR = true;
                    }
                }

                return false;
            });

            CheckWindowState();
        }

        private Image GetQRCodeAsImage(IWebElement ele)
        {           
            var base64Img = driver.ExecuteJavaScript<string>("return arguments[0].toDataURL('image/png').substring(22);", ele);
            
            Image img = null;
            using (var stream = new MemoryStream(Convert.FromBase64String(base64Img)))
            {
                img = Image.FromStream(stream);
            }
            return img;
        }

        public void Dispose()
        {
            try
            {
                driver?.Quit();
            }
            catch (Exception)
            {

                throw;
            }
            try
            {
                driver?.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                IsDisposed = true;
                OnDisposed?.Invoke();
            }
        }

        public void SendMessage(string number, string message, uint load_timeout = 30, uint ticks_timeout = 10)
        {
            try
            {
                if(string.IsNullOrEmpty(number))
                    throw new ArgumentException(nameof(number) + " is required.");

                if (string.IsNullOrEmpty(message))
                    throw new ArgumentException(nameof(message) + " is required.");

                CheckWindowState();

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                var textbox = WaitForCSSElemnt("[data-testid='conversation-compose-box-input']", load_timeout);
                foreach (var line in message.Split('\n').Where(x => x.Trim().Length > 0))
                {
                    textbox.SendKeys(line);
                    var actions = new Actions(driver);
                    actions.KeyDown(Keys.Shift);
                    actions.KeyDown(Keys.Enter);
                    actions.KeyUp(Keys.Enter);
                    actions.KeyUp(Keys.Shift);
                    actions.Perform();
                }
                textbox.SendKeys(Keys.Enter);
                TryDismissAlert();

                WaitForLastMessage(ticks_timeout);
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        public void SendMedia(MediaType mediaType, string number, string path, string caption=null, uint load_timeout = 30, uint ticks_timeout = 20)
        {
            try
            {
                CheckWindowState();

                if (string.IsNullOrEmpty(number))
                    throw new ArgumentException(nameof(number) + " is required.");

                if (!File.Exists(path))
                    throw new FileNotFoundException(path);

                var fi = new FileInfo(path);
                if (fi.Length == 0 || fi.Length > 16000000)
                    throw new ArgumentException("File size out of allowed bounds [1Byte, 16MB].");

                driver.Url = $"https://web.whatsapp.com/send?phone={number}&text&type=phone_number&app_absent=1";

                WaitForCSSElemnt("[data-testid='conversation-compose-box-input']", load_timeout);

                var clip = WaitForCSSElemnt("[data-testid='conversation-clip']");
                clip.Click();

                var attachImage = WaitForCSSElemnt($"[data-testid='{(mediaType == MediaType.IMAGE_OR_VIDEO ? "attach-image" : "attach-document")}']");
                var fileinput = attachImage.FindElement(By.XPath("../input"));
                fileinput.SendKeys(path);

                var textbox = WaitForCSSElemnt("[data-testid='media-caption-input-container']");
                if(!string.IsNullOrEmpty(caption))
                    foreach (var line in caption.Split('\n').Where(x=>x.Trim().Length>0))
                    {
                        textbox.SendKeys(line);
                        var actions = new Actions(driver);
                        actions.KeyDown(Keys.Shift);
                        actions.KeyDown(Keys.Enter);
                        actions.KeyUp(Keys.Enter);
                        actions.KeyUp(Keys.Shift);
                        actions.Perform();
                    }

                textbox.SendKeys(Keys.Enter);
                TryDismissAlert();

                WaitForLastMessage(ticks_timeout);
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        public void Logout()
        {
            try
            {
                var elms = driver.FindElements(By.CssSelector("[data-testid='mi-logout menu-item']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    var confirmBtn = WaitForCSSElemnt("[data-testid='popup-controls-ok']");
                    confirmBtn.Click();
                    Wait(4);
                    Dispose();
                    return;
                }

                elms = driver.FindElements(By.CssSelector("[data-testid='menu-bar-menu']"));
                if (elms.Count > 0)
                {
                    elms.First().Click();
                    var logoutBtn = WaitForCSSElemnt("[data-testid='mi-logout menu-item']");
                    logoutBtn.Click();

                    var confirmBtn = WaitForCSSElemnt("[data-testid='popup-controls-ok']");
                    confirmBtn.Click();
                    Wait(4);
                    Dispose();
                }
                else
                {
                    throw new Exception("unable to logout.");
                }
            }
            catch (NoSuchWindowException)
            {
                Dispose();
                throw;
            }
        }

        private void Wait(uint seconds)
        {
            var timenow = DateTime.Now;
            new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(x => DateTime.Now - timenow >= TimeSpan.FromMilliseconds(seconds * 1000));
        }

        private IWebElement WaitForCSSElemnt(string selector, uint timeout = 3)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !CheckWindowState(false) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElement(By.CssSelector(selector));
        }

        private void WaitForLastMessage(uint seconds)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(seconds)).Until(x => {
                if (!CheckWindowState(false))
                    return true;

                var elms = x.FindElements(By.CssSelector("[data-testid='msg-dblcheck']"));
                if (elms.Count > 0)
                {
                    var label = elms.Last().GetAttribute("aria-label").ToLower().Trim();
                    if (label.Equals("send") || label.Equals("delivered") || label.Equals("read"))
                    {
                        return true;
                    }
                }

                return false;
            });

            CheckWindowState();
        }

        private ReadOnlyCollection<IWebElement> WaitForCSSElemnts(string selector, int timeout = 3)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until(x => !CheckWindowState(false) || x.FindElements(By.CssSelector(selector)).Count > 0);
            CheckWindowState();
            return driver.FindElements(By.CssSelector(selector));
        }

        private bool CheckWindowState(bool raiseError = true)
        {
            if (!driver.WindowHandles.Contains(handle) || !driver.Url.StartsWith(BASE_URL, StringComparison.InvariantCultureIgnoreCase)) {
                Dispose();
                if (raiseError)
                    throw new NoSuchWindowException("window closed.");
                else
                    return false;
            }
            return true;
        }

        private void TryDismissAlert()
        {
            try
            {
                driver.SwitchTo().Alert().Accept();
            }
            catch (Exception)
            {
            }
        }
    }
}