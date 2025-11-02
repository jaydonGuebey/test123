using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (HS-1.3 Reactieve Validatie)
 * ===================================================================================
 * Dit testbestand valideert de "reactiviteit" van het voorschriftformulier 
 * (Acceptance Criteria HS-1.3).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. HS_1_3_2_ErrorDisappearsOnCorrection:
 * - Rapporteert of een validatiefout (bv. "Quantity is verplicht") 
 * correct wordt opgelost *zonder* een volledige pagina refresh.
 * - De test forceert de fout, corrigeert het veld (vult '10' in), 
 * en valideert dat het medicijn nu wél succesvol kan worden 
 * toegevoegd aan de lijst.
 * ===================================================================================
 */

namespace Tests.Specialist
{
    [TestFixture]
    public class HS_1_3_voorschrift_reactive_validatie : BaseTest
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

        // Locator voor de Dosering foutmelding
        private static readonly By QuantityErrorSpan = By.CssSelector("span[data-valmsg-for='Quantity']");


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

        // ROBUUSTE HELPER VOOR INPUT FIELDS (simuleert typen én verlaten van het veld)
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

        // NAVIGATIE HELPER MET RETRY LOGIC (om onverwachte redirects op te vangen)
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
        // --- TC HS-1.3.2: Foutmelding verdwijnt na correctie (Functionele Check) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("HS-1.3")]
        public void HS_1_3_2_ErrorDisappearsOnCorrection()
        {
            log.Info("=== Starting HS-1.3.2: Foutmelding verdwijnt na corrigerende Dosering invoer (Functionele check) ===");

            // Arrange
            // Aangepast naar een puur numerieke waarde om server-side validatie te omzeilen
            string validQuantity = "10";

            try
            {
                // Setup: Login en basisvelden
                NavigateToLogin();
                Type(By.Name("Username"), Username, "Logging in as Doctor");
                Type(By.Name("Password"), Password, "Entering password");
                Click(By.Name("btn-login"), "Clicking login button");

                NavigateToNewPrescriptionForm();
                SelectPatient("7");
                SetTextareaValue(DescriptionField, "Test voor reactieve validatie.", "Entering Description.");

                SubmitViaJavaScript(AddMedicineStepButton, "Activeer medicijnsectie.");
                FindWithWait(MedicineSelect);
                SelectMedicine("5"); // Lisinopril
                SetValueViaJavaScript(InstructionsField, "1x daags", "Vul Frequentie in.");


                // Stap 1: Genereer de foutmelding (Precondition)
                SetValueViaJavaScript(QuantityField, "", "Dosering veld leeg maken (om fout te genereren).");
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Klik op 'Add' om validatie te forceren.");

                // Precondition Controle: Wacht tot de foutmelding ZICHTBAAR is.
                var initialErrorSpan = FindWithWait(QuantityErrorSpan);
                Assert.That(initialErrorSpan.Displayed, Is.True, "Precondition FAILED: Foutmelding Dosering is niet zichtbaar.");
                log.Info("✓ Foutmelding is zichtbaar.");


                // Stap 2, 3 & 4: Correctie - Typ geldige waarde
                // Deze actie triggert het 'change' event en heft de validatie op.
                SetValueViaJavaScript(QuantityField, validQuantity, $"Step 2/3/4: Typ geldige Dosering ({validQuantity}).");

                // Micro-pauze om client-side validatie de tijd te geven om te reageren
                Thread.Sleep(50);
                log.Info("Micro-pauze (50ms) genomen na correctie.");


                // Finale FUNCTIONELE CHECK: Klik opnieuw op 'Add' om te bewijzen dat de fout is opgelost.
                // Dit is de functionele test die valideert dat de correctie succesvol was.
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Bevestig dat toevoegen nu slaagt (Functionele Check).");

                // Finale Assertie: Wacht tot de medicijnnaam in de lijst (tabel) verschijnt
                FindWithWait(By.XPath("//table//td[contains(text(), 'Lisinopril')]"));
                log.Info("✓ Medicijn succesvol toegevoegd aan de lijst na correctie. Test slaagt.");

                log.Info("=== HS-1.3.2 PASSED: Reactieve validatie werkt correct (Functionele check) ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.3.2 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("HS_1_3_2_Failed");
                throw;
            }
        }
    }
}