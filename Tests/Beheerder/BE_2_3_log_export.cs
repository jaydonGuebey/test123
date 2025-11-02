using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (BE-2.3 Log Export)
 * ===================================================================================
 * Dit testbestand valideert de exportfunctionaliteit van de logbestanden
 * (Acceptance Criteria BE-2.3).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. BE_2_3_2_SuccessfulLogFileDownload (Naam aangepast naar 2.3.1 in latere versies):
 * - Rapporteert of de 'Download' (TXT) knop functioneel is.
 * - De test selecteert een logbestand uit de dropdown en klikt op download.
 * - De test slaagt als de klik op de knop *geen* client-side of 
 * server-side fout (zoals een 'alert-danger') veroorzaakt.
 * - *Noot: Deze test valideert het *triggeren* van de download, 
 * niet de inhoud van het .txt-bestand zelf.*
 * ===================================================================================
 */

namespace Tests.Beheerder
{
    [TestFixture]
    public class BE_2_3_log_export : BaseTest // Klasse BE_2_3
    {
        // Gebruikersgegevens
        private const string AdminUsername = "admin"; // Admin Jansen
        private const string AdminPassword = "admin1";

        // Testdata
        private const string TargetFilterUser = "Dokter De Wit";
        private const string LogFilesUrl = "/LogFiles";


        // Locators
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // Locators op de LogFiles pagina
        private static readonly By LogFileDropdownInput = By.Id("logFileDropdown");
        private static readonly By ApplyFilterButton = By.Id("applyFilter");
        private static readonly By ExportCsvButton = By.Id("exportCsvButton");
        private static readonly By DownloadTxtButton = By.Id("downloadBtn"); // De nieuwe TXT download knop
        private static readonly By FirstLogFileButton = By.XPath("//div[@id='dropdownList']/button[1]");
        private static readonly By LogContentTextarea = By.Id("logContent");
        private static readonly By ResultsTable = By.ClassName("table");
        private static readonly By ErrorAlert = By.CssSelector("div.alert-danger"); // Voor generieke foutcontrole


        // --- HELPERFUNCTIES ---

        private void PerformLogin(string username, string password, string role)
        {
            log.Info($"Logging in as {role} user: {username}");
            NavigateToLogin();
            Type(LoginUsernameField, username, $"Entering username ({username})");
            Type(LoginPasswordField, password, "Entering password");
            Click(LoginButton, "Clicking login button");
            _wait.Until(d => d.Url != _baseUrl + "/Account/Login");
        }

        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced click).");
        }

        // NIEUWE FUNCTIE: Selecteert het eerste logbestand
        private void SelectFirstLogFile()
        {
            // Stap 1: Focus op de input om de dropdown te activeren
            FindWithWait(LogFileDropdownInput).Click();

            // Stap 2: Klik op de eerste knop in de dropdown
            IWebElement logButton = FindWithWait(FirstLogFileButton);
            logButton.Click();

            // Stap 3: Wacht tot de textarea gevuld is
            _wait.Until(d => !string.IsNullOrWhiteSpace(d.FindElement(LogContentTextarea).GetAttribute("value")));
        }

        // --- Deel van HS-2.3.1 code (weggelaten voor beknoptheid) ---


        // ------------------------------------------------------------------
        // --- TC BE-2.3.1: HAPPY PATH (Succesvolle Export) ---
        // (De code voor BE-2.3.1 is hier weggelaten voor beknoptheid, maar hoort in het bestand)
        // ------------------------------------------------------------------


        // ------------------------------------------------------------------
        // --- TC BE-2.3.2: DOWNLOAD (Succesvolle Download van TXT) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("BE-2.3")]
        public void BE_2_3_2_SuccessfulLogFileDownload()
        {
            log.Info("=== Starting BE-2.3.2: Succesvolle download van TXT logbestand ===");

            try
            {
                // Stap 1: Log in als 'Admin Jansen'
                PerformLogin(AdminUsername, AdminPassword, "Admin");

                // Stap 2: Navigeer naar Audittrail
                log.Info($"Navigeren naar {LogFilesUrl}.");
                _driver.Navigate().GoToUrl(_baseUrl + LogFilesUrl);
                FindWithWait(LogFileDropdownInput);

                // Stap 3: Selecteer een logbestand
                log.Info("Stap 3: Selecteer het eerste logbestand via de dropdown.");
                SelectFirstLogFile();

                // Wacht een moment om de knop te laten de-disabelen (indien van toepassing)
                Thread.Sleep(500);

                // Stap 4: Klik op de knop 'Download'
                log.Info("Stap 4: Klikken op de 'Download' (TXT) knop.");

                // Gebruik de robuuste JS click
                SubmitViaJavaScript(DownloadTxtButton, "Activeren van de TXT download.");

                // Expected Result: De download moet starten zonder UI-fouten.
                Thread.Sleep(200);

                // Controleer op een generieke foutmelding na de actie
                if (_driver.FindElements(ErrorAlert).Count > 0)
                {
                    Assert.Fail("FAILURE: Server/client gaf een foutmelding na poging tot download.");
                }

                // Als de code hier komt zonder fout, is de download succesvol getriggerd.
                log.Info("✓ Assertion passed: De TXT download is succesvol getriggerd zonder runtime fouten.");

                log.Info("=== BE-2.3.2 PASSED: Download functionaliteit is getriggerd ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-2.3.2 FAILED: {ex.Message}");
                TakeScreenshot("BE_2_3_2_Failed");
                throw;
            }
        }
    }
}