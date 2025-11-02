using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (HS-1.4 Datum Validatie)
 * ===================================================================================
 * Dit testbestand valideert de logische datumvalidatie op het 
 * voorschriftformulier (Acceptance Criteria HS-1.4).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. HS_1_4_1_CreateSuccessfulWithValidDates (Happy Path):
 * - Rapporteert of het opslaan slaagt als de Stopdatum (vandaag+7) 
 * na de Startdatum (vandaag) ligt.
 *
 * 2. HS_1_4_2_SaveBlockedByInvalidEndDate (Unhappy Path):
 * - Rapporteert of het systeem de Specialist **blokkeert** als de 
 * Stopdatum (gisteren) vóór de Startdatum (vandaag) ligt.
 * - Valideert dat de correcte Engelse foutmelding 
 * ("end date must be... start date") wordt getoond.
 * ===================================================================================
 */

namespace Tests.Specialist
{
    [TestFixture]
    public class HS_1_4_voorschrift_datum_validatie : BaseTest // Nieuwe klasse HS-1.4
    {
        // Gebruiker voor de specialist
        private const string Username = "specialist";
        private const string Password = "specialist1";

        // Locators
        private static readonly By NewPrescriptionLink = By.CssSelector("a[href='/Prescriptions/new']");
        private static readonly By PatientSelect = By.Id("patientSelect");
        private static readonly By StartDateField = By.Id("Prescription_PrescriptionStartDate");
        private static readonly By EndDateField = By.Id("Prescription_PrescriptionEndDate");
        private static readonly By DescriptionField = By.Id("descBox");

        private static readonly By AddMedicineStepButton = By.CssSelector("button[formaction$='handler=AddMedicine']");

        private static readonly By MedicineSelect = By.Id("SelectedMedicineId");
        private static readonly By QuantityField = By.Id("Quantity"); // Dosering
        private static readonly By InstructionsField = By.Id("Instructions"); // Frequentie

        private static readonly By AddMedicineConfirmationButton = By.CssSelector("button[name='action'][value='add']");
        private static readonly By CreatePrescriptionButton = By.CssSelector("button.btn.btn-primary[type='submit']");

        // Locator voor de Foutmelding bij Stopdatum (belangrijk voor HS-1.4.2)
        private static readonly By EndDateErrorSpan = By.CssSelector("span[data-valmsg-for='Prescription.PrescriptionEndDate']");


        // --- HELPERFUNCTIES ---

        private void SetDateValue(By by, string dateString, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript($"arguments[0].value = '{dateString}';", element);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", element);
        }

        private void SetTextareaValue(By by, string text, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            element.Clear();
            ((IJavaScriptExecutor)_driver).ExecuteScript($"arguments[0].value = '{text}';", element);
        }

        private void SetValueViaJavaScript(By by, string text, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            element.Clear();
            ((IJavaScriptExecutor)_driver).ExecuteScript($"arguments[0].value = '{text}';", element);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", element);
            log.Info($"Value '{text}' set via JavaScript on element {by}.");
        }

        private void SelectPatient(string value)
        {
            LogStep($"Selecting patient with value: {value}");
            var selectElement = new SelectElement(FindWithWait(PatientSelect));
            selectElement.SelectByValue(value);
        }

        private void SelectMedicine(string value)
        {
            LogStep($"Selecting medicine with value: {value}");
            var selectElement = new SelectElement(FindWithWait(MedicineSelect));
            selectElement.SelectByValue(value);
        }

        private void NavigateToNewPrescriptionForm()
        {
            const string basePrescriptionsUrl = "/Prescriptions";
            const string redirectIndexPart = "/?page=%2FIndex";

            log.Info("Navigating to Prescriptions Index and opening form...");

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                if (attempt > 1) log.Warn($"Retry {attempt - 1} failed. Starting attempt {attempt}.");

                _driver.Navigate().GoToUrl(_baseUrl + basePrescriptionsUrl);

                Click(NewPrescriptionLink, $"Clicking 'New Prescription' link (Attempt {attempt}).");

                if (_driver.Url.Contains(redirectIndexPart))
                {
                    log.Warn($"Redirect naar index/login gedetecteerd na klik. Herstart navigatie...");
                    if (attempt == 2) throw new WebDriverException($"Navigatie naar voorschriftformulier mislukt na 2 pogingen. Blijft redirecten naar: {_driver.Url}");
                    continue;
                }

                try
                {
                    FindWithWait(CreatePrescriptionButton);
                    log.Info("Successfully navigated to the prescription form.");
                    return;
                }
                catch (WebDriverTimeoutException)
                {
                    if (attempt == 2) throw;
                }
            }
        }

        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }


        // ------------------------------------------------------------------
        // --- TC HS-1.4.1: HAPPY PATH (Stopdatum > Startdatum) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("HS-1.4")]
        public void HS_1_4_1_CreateSuccessfulWithValidDates()
        {
            log.Info("=== Starting HS-1.4.1: Voorschrift succesvol aangemaakt (logische datums) ===");

            // Arrange
            string startDate = DateTime.Now.ToString("yyyy-MM-dd");
            string happyEndDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
            string patientValue = "7";

            try
            {
                // Act - Login
                NavigateToLogin();
                Type(By.Name("Username"), Username, "Logging in as Doctor");
                Type(By.Name("Password"), Password, "Entering password");
                Click(By.Name("btn-login"), "Clicking login button");

                // Stap 1: Open het formulier
                NavigateToNewPrescriptionForm();
                SelectPatient(patientValue);
                SetTextareaValue(DescriptionField, "Antibioticakuur voor 7 dagen.", "Entering Description.");

                // Stap 2 & 3: Stel datums in (Startdatum = Vandaag, Stopdatum = Vandaag + 7 dagen)
                SetDateValue(StartDateField, startDate, $"Setting Start Date to {startDate}");
                SetDateValue(EndDateField, happyEndDate, $"Setting End Date to {happyEndDate}");

                // Voeg medicijn toe om het voorschrift compleet te maken
                SubmitViaJavaScript(AddMedicineStepButton, "Activate medicine section.");
                FindWithWait(MedicineSelect);
                SelectMedicine("1");
                SetValueViaJavaScript(QuantityField, "14", "Entering Quantity (2 per dag * 7 dagen).");
                SetValueViaJavaScript(InstructionsField, "2x per dag", "Entering Instructions.");
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Clicking 'Add' to list the medicine.");
                FindWithWait(By.XPath("//table//td[contains(text(), 'Panadol') or contains(text(), 'Lisinopril')]"));

                // Stap 5: Klik op 'Opslaan'
                SubmitViaJavaScript(CreatePrescriptionButton, "Clicking final 'Create Prescription' button.");

                // Expected Result: Het voorschrift wordt succesvol opgeslagen
                _wait.Until(d => d.Url.Contains("/Prescriptions") && !d.Url.Contains("/Prescriptions/new"));

                log.Info("✓ Assertion passed: Succesvolle navigatie na het aanmaken van het voorschrift.");
                log.Info("=== HS-1.4.1 PASSED: Voorschrift met logische datums succesvol aangemaakt ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.4.1 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_4_1_Failed");
                throw;
            }
        }


        // ------------------------------------------------------------------
        // --- TC HS-1.4.2: UNHAPPY PATH (Stopdatum < Startdatum) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("HS-1.4")]
        public void HS_1_4_2_SaveBlockedByInvalidEndDate()
        {
            log.Info("=== Starting HS-1.4.2: Systeem blokkeert opslaan (Stopdatum < Startdatum) ===");

            // Arrange
            string startDate = DateTime.Now.ToString("yyyy-MM-dd");
            string unhappyEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"); // Gisteren
            string expectedPage = "/Prescriptions/new";

            try
            {
                // Act - Login
                NavigateToLogin();
                Type(By.Name("Username"), Username, "Logging in as Doctor");
                Type(By.Name("Password"), Password, "Entering password");
                Click(By.Name("btn-login"), "Clicking login button");

                // Stap 1: Open het formulier
                NavigateToNewPrescriptionForm();
                SelectPatient("7");
                SetTextareaValue(DescriptionField, "Test: Stopdatum ligt voor Startdatum.", "Entering Description.");

                // Stap 2 & 3: Stel datums in (Startdatum = Vandaag, Stopdatum = Gisteren)
                SetDateValue(StartDateField, startDate, $"Setting Start Date to {startDate}");
                SetDateValue(EndDateField, unhappyEndDate, $"Setting invalid End Date to {unhappyEndDate}");

                // Voeg geldig medicijn toe om te controleren of de datumfout de enige blokkade is
                SubmitViaJavaScript(AddMedicineStepButton, "Activate medicine section.");
                FindWithWait(MedicineSelect);
                SelectMedicine("5"); // Lisinopril
                SetValueViaJavaScript(QuantityField, "10", "Entering Quantity.");
                SetValueViaJavaScript(InstructionsField, "1x per dag", "Entering Instructions.");
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Clicking 'Add' to list the medicine.");

                // Stap 4: Probeer op te slaan
                SubmitViaJavaScript(CreatePrescriptionButton, "Attempting to click 'Create Prescription' button.");

                // Wacht tot de foutmelding verschijnt (Expected Result)
                log.Info("Wachten tot de Stopdatum foutmelding zichtbaar wordt...");
                var errorSpan = FindWithWait(EndDateErrorSpan);

                // Assertie A: URL is niet veranderd (opslaan geblokkeerd)
                Assert.That(_driver.Url, Does.Contain(expectedPage).IgnoreCase, "FAILURE: Pagina is onterecht doorgestuurd na validatiefout.");

                // Assertie B: De correcte foutmelding is zichtbaar en bevat relevante trefwoorden
                // Aangepaste Assertie om de Engelse melding op te vangen:
                Assert.That(errorSpan.Text,
                    Does.Contain("end date").IgnoreCase.And.Contain("start date").IgnoreCase // Zoek naar functionele trefwoorden in de Engelse melding
                    .Or.Contain("vóór").IgnoreCase // Valideer Nederlandse term
                    .Or.Contain("kan niet"), // Valideer algemene Nederlandse verboden term
                    $"FAILURE: Foutmelding is onduidelijk. Gevonden tekst: '{errorSpan.Text}'");

                log.Info("✓ Assertions passed: Opslaan is geblokkeerd en correcte foutmelding is getoond.");
                log.Info("=== HS-1.4.2 PASSED: Logische datumvalidatie is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.4.2 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_4_2_Failed");
                throw;
            }
        }
    }
}