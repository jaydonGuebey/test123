// Bestand: BaseTest.cs
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using log4net;
using System.Threading;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (BaseTest - Fundament van het Framework)
 * ===================================================================================
 * Dit bestand dient als het technische FUNDAMENT van de E2E-testarchitectuur.
 * Het definieert de basisvereisten voor het uitvoeren van elke individuele test.
 *
 * Wat wordt gerapporteerd:
 *
 * 1. Stabiliteit (`FindWithWait`): Dit is de belangrijkste bijdrage. De methode 
 * `FindWithWait(By by)` dwingt Selenium om te wachten tot elementen zichtbaar/interactief zijn 
 * (10 seconden), waardoor onbetrouwbare (flaky) tests als gevolg van timingproblemen 
 * worden geminimaliseerd.
 *
 * 2. Driver Lifecycle: Beheert de volledige levenscyclus van de browser: 
 * `[SetUp]` start een nieuwe Chrome-sessie (standaard headless) en `[TearDown]` 
 * sluit de browser correct na ELKE test (`_driver.Quit()`).
 *
 * 3. Foutafhandeling (`TakeScreenshot`): De methode zorgt ervoor dat bij elke 
 * testfout een visueel artefact (screenshot) wordt vastgelegd voor snelle debugging.
 * * 4. Core Helpers: Biedt geabstraheerde helpers (`Type`, `Click`, `LogStep`) 
 * die de directe driver-aanroepen vervangen en logging standaardiseren.
 * ===================================================================================
 */

namespace Tests
{
    // Alle setup-logica is hierheen verplaatst
    public abstract class BaseTest
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(BaseTest));

        protected IWebDriver _driver;
        protected WebDriverWait _wait;
        protected string _baseUrl;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _baseUrl = Environment.GetEnvironmentVariable("SELENIUM_BASE_URL") ?? "http://localhost:5070";

            if (string.IsNullOrEmpty(_baseUrl))
            {
                log.Warn("Selenium test skipped because SELENIUM_BASE_URL is not set.");
                Assert.Ignore("Selenium test skipped because SELENIUM_BASE_URL is not set.");
            }

            log.Info($"Test Suite initialized with base URL: {_baseUrl}");
        }

        [SetUp]
        public void SetUp()
        {
            log.Info("Setting up Chrome WebDriver");

            var options = new ChromeOptions();
            var headlessEnv = Environment.GetEnvironmentVariable("SELENIUM_HEADLESS") ?? "false"; // zet naar true om headless te draaien
            var headless = headlessEnv.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (headless)
            {
                options.AddArgument("--headless");
                log.Info("Running in headless mode");
            }
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--allow-insecure-localhost");
            options.AddArgument("--remote-allow-origins=*");
            options.AcceptInsecureCertificates = true;

            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            _driver = new ChromeDriver(service, options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            log.Info("Chrome WebDriver setup complete");
        }

        // Zorg dat deze helper-functie in je BaseTest.cs staat, of voeg hem hier toe:
        public void TakeScreenshot(string testName)
        {
            try
            {
                var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                var filename = $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                screenshot.SaveAsFile(filename);
                log.Info($"Screenshot saved: {filename}");
            }
            catch (Exception screenshotEx)
            {
                log.Error($"Failed to take screenshot: {screenshotEx.Message}");
            }
        }

        [TearDown]
        public void TearDown()
        {
            log.Info("Tearing down Chrome WebDriver");
            _driver?.Quit();
            _driver?.Dispose();
        }

        // --- HELPER FUNCTIES (zoals in Code 2) ---
        // Deze vervangen de directe driver-aanroepen en Thread.Sleep

        protected void LogStep(string message)
        {
            log.Info(message);
        }

        protected void NavigateToLogin()
        {
            LogStep($"Step 1: Navigating to login page: {_baseUrl}");
            _driver.Navigate().GoToUrl(_baseUrl);
            LogStep("Successfully navigated to login page");
        }

        protected IWebElement FindWithWait(By by)
        {
            // Gebruik de expliciete wait om het element te vinden
            // Dit vervangt Thread.Sleep en maakt de test robuust
            return _wait.Until(d => d.FindElement(by));
        }

        protected void Type(By by, string text, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            element.Clear();
            element.SendKeys(text);

            LogStep($"Entered text: {text}");
        }

        protected void Click(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            element.Click();
            LogStep("Clicked element");
        }
    }
}