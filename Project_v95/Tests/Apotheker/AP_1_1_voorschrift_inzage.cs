using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using Tests;
using OpenQA.Selenium.Interactions;
using Assert = NUnit.Framework.Assert;

namespace Tests.Apotheker
{
    [TestFixture]
    public class AP_1_1_voorschrift_inzage : BaseTest // Nieuwe klasse AP_1_1
    {
        // Gebruikersgegevens
        private const string Username = "apothecary";
        private const string Password = "apothecary1"; // Aangenomen wachtwoord

        // Test Data
        private const string PatientSearchTerm = "Patient";
        private const string PatientTargetUsername = "Patient";
        private const string ExpectedMiddel = "Panadol";
        private const string ExpectedDosering = "1x daags"; // Dosering
        private const string ExpectedHoeveelheid = "90"; // Hoeveelheid
        private const string PatientInfoUrl = "/PatientInfo";

        // Locators
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // Locators voor het PatientInfo scherm
        private static readonly By SearchTermField = By.Id("searchTerm");
        private static readonly By FirstAutocompleteItem = By.CssSelector("ul#autocomplete-list > li:first-child");
        private static readonly By SelectButton = By.Id("search-btn");
        private static readonly By ConfirmButton = By.CssSelector("button[name='action'][value='confirm']");

        // Locator voor de Voorschriftentabel
        private static readonly By PrescriptionTable = By.ClassName("table");

        // Final locator om de kritieke voorschriftendetails te verifiëren
        private static By GetPrescriptionDetailLocator(string middel, string hoeveelheid)
        {
            // Zoekt een tabelcel die de Medicijnnaam (Middel) bevat, en in de volgende cellen de Hoeveelheid.
            return By.XPath($"//table//td[contains(text(), '{middel}')]/following-sibling::td[contains(text(), '{hoeveelheid}')]");
        }


        // --- HELPERFUNCTIES ---

        private void PerformLogin(string username, string password, string role)
        {
            log.Info($"Logging in as {role} user: {username}");
            NavigateToLogin();
            Type(LoginUsernameField, username, $"Entering username ({username})");
            Type(LoginPasswordField, password, "Entering password");
            Click(LoginButton, "Clicking login button");
            _wait.Until(d => !d.Url.Contains("/Account/Login"));
        }

        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }


        // ------------------------------------------------------------------
        // --- TC AP-1.1.1: HAPPY PATH (Succesvol Voorschrift Inzien) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("AP-1.1")]
        public void AP_1_1_1_SuccessfulPrescriptionRetrieval()
        {
            log.Info("=== Starting AP-1.1.1: Succesvol ophalen van een volledig en actueel voorschrift ===");

            try
            {
                // Stap 1: Log in als 'Apotheker De Wit'
                PerformLogin(Username, Password, "Apotheker");

                // Stap 2: Zoek en open het dossier van 'Patiënt Jansen'
                log.Info($"Stap 2: Navigeer naar {PatientInfoUrl} en zoek patiënt.");
                _driver.Navigate().GoToUrl(_baseUrl + PatientInfoUrl);

                // Voer zoekterm in
                Type(SearchTermField, PatientTargetUsername, $"Typen van zoekterm '{PatientTargetUsername}'.");

                // Wacht tot de dropdown verschijnt
                _wait.Until(d => d.FindElement(FirstAutocompleteItem).Displayed);

                // Klik het eerste item in de dropdown
                FindWithWait(FirstAutocompleteItem).Click();

                // Klik de Select knop
                Click(SelectButton, "Klikken op de 'Select' knop.");

                // De pagina moet nu omleiden naar een bevestigingsscherm of direct naar het dossier.
                // We wachten op de Confirm knop.
                FindWithWait(ConfirmButton);

                // Klik de Confirm knop om het dossier te openen
                Click(ConfirmButton, "Klikken op de 'Confirm' knop om het dossier te openen.");

                // Wacht tot de voorschriftenlijst geladen is
                _wait.Until(d => d.Url.Contains("/Prescriptions"));

                // Stap 3 & 4: Observeer de details van het nieuwste voorschrift (De lijst tonen)

                // Expected Result: Alle velden (Middel, Sterkte, Dosering, Hoeveelheid) worden correct weergegeven
                log.Info($"Stap 4: Verifiëren van kritieke voorschriftendetails (Middel: {ExpectedMiddel}, Hoeveelheid: {ExpectedHoeveelheid}).");

                By detailLocator = GetPrescriptionDetailLocator(ExpectedMiddel, ExpectedHoeveelheid);
                IWebElement prescriptionDetail = FindWithWait(detailLocator);

                // Controleer de inhoud van de rij/element dat is gevonden
                string rowText = prescriptionDetail.FindElement(By.XPath("./..")).Text;
                Assert.That(rowText, Does.Contain(ExpectedDosering).IgnoreCase, "FAILURE: Dosering is niet correct weergegeven.");
                Assert.That(rowText, Does.Contain(ExpectedHoeveelheid).IgnoreCase, "FAILURE: Hoeveelheid is niet correct weergegeven.");

                log.Info("✓ Assertion passed: Alle kritieke voorschriftgegevens zijn correct weergegeven.");

                log.Info("=== AP-1.1.1 PASSED: Voorschrift succesvol opgehaald en gecontroleerd ===");
            }
            catch (Exception ex)
            {
                log.Error($"AP-1.1.1 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("AP_1_1_1_Failed");
                throw;
            }
        }
    }
}