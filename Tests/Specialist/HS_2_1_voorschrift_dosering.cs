using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using System.Collections.ObjectModel;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (HS-2.1 Dosering en Waarschuwing)
 * ===================================================================================
 * Dit testbestand valideert het instellen van de dosering en de 
 * klinische beslissingsondersteuning (Acceptance Criteria HS-2.1).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. HS_2_1_1_SetStructuredDosageSuccessfully (Happy Path):
 * - Rapporteert of een Arts succesvol een specifieke dosering 
 * (bv. '1' [tablet] en '500mg' [instructie]) kan invoeren en opslaan.
 * - Slaagt als het voorschrift succesvol wordt aangemaakt en de 
 * arts wordt teruggestuurd naar de index.
 *
 * 2. HS_2_1_2_SystemWarnsOnHighDosage (Unhappy Path - VERWACHT TE FALEN):
 * - Deze test rapporteert een **FUNCTIONEEL GAT**.
 * - De test voert een ongebruikelijk hoge dosering in (bv. '2000mg').
 * - De test valideert vervolgens of het systeem de arts *niet blokkeert* * (professionele autonomie, dat is goed), MAAR...
 * - De test **FAALT** omdat het verwacht dat er een waarschuwing 
 * (bv. "Dosering is hoger dan gebruikelijk") verschijnt, 
 * die momenteel ontbreekt.
 * ===================================================================================
 */

namespace Tests.Specialist
{
    [TestFixture]
    public class HS_2_1_voorschrift_dosering : BaseTest // KLASNAAM HS_2_1
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
        private static readonly By QuantityField = By.Id("Quantity"); // Hoeveelheid/Vorm
        private static readonly By InstructionsField = By.Id("Instructions"); // Frequentie

        private static readonly By AddMedicineConfirmationButton = By.CssSelector("button[name='action'][value='add']");
        private static readonly By CreatePrescriptionButton = By.CssSelector("button.btn.btn-primary[type='submit']");

        // Locator voor een generieke waarschuwing bij dosering (Gebruikt in HS-2.1.2)
        private static readonly By DosageWarningMessage = By.CssSelector("div.alert.alert-warning, span.text-warning, span[data-valmsg-for*='Quantity']");

        // Verwachte URLs
        private const string PrescriptionsIndexUrl = "/Prescriptions";


        // --- HELPERFUNCTIES ---

        private void PerformLogin(string username, string password, string role)
        {
            log.Info($"Logging in as {role} user: {username}");
            NavigateToLogin();
            Type(By.Name("Username"), username, $"Entering username ({username})");
            Type(By.Name("Password"), password, "Entering password");
            Click(By.Name("btn-login"), "Clicking login button");
            _wait.Until(d => !d.Url.Contains("/Account/Login"));
        }

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
            log.Info("Navigating to Prescriptions Index and opening form...");
            _driver.Navigate().GoToUrl(_baseUrl + "/Prescriptions");
            Click(By.CssSelector("a[href='/Prescriptions/new']"), "Clicking 'New Prescription' link.");

            FindWithWait(PatientSelect);
            log.Info("Successfully navigated to the prescription form.");
        }

        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }

        // Haal de waarde van een veld op via JS (voor validatie)
        private string GetValueViaJavaScript(By by)
        {
            var element = FindWithWait(by);
            return (string)((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].value;", element);
        }

        // ------------------------------------------------------------------
        // --- TC HS-2.1.1: HAPPY PATH (Succesvol Dosering Instellen) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("HS-2.1")]
        public void HS_2_1_1_SetStructuredDosageSuccessfully()
        {
            log.Info("=== Starting HS-2.1.1: Succesvol instellen van een gestructureerde dosering ===");

            // Arrange
            string patientValue = "7";
            string medicineId = "1";
            string medicineName = "Panadol"; // Pas dit aan indien nodig
            string dosageQuantity = "1";
            string dosageStrength = "500mg";

            try
            {
                // Stap 1: Log in als de gebruiker
                PerformLogin(Username, Password, "Specialist");

                // Stap 2: Start een nieuw voorschrift en vul basisdata
                NavigateToNewPrescriptionForm();
                SelectPatient(patientValue);
                SetDateValue(StartDateField, DateTime.Now.ToString("yyyy-MM-dd"), "Entering Start Date.");
                SetDateValue(EndDateField, DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"), "Entering End Date.");
                SetTextareaValue(DescriptionField, "Doseringstest (Happy Path).", "Entering Description.");

                // Activeer en vul medicijnsectie
                SubmitViaJavaScript(AddMedicineStepButton, "Activate medicine section.");
                FindWithWait(MedicineSelect);
                SelectMedicine(medicineId);
                SetValueViaJavaScript(QuantityField, dosageQuantity, $"Entering Hoeveelheid: {dosageQuantity}.");
                SetValueViaJavaScript(InstructionsField, $"{dosageStrength}, 1x daags", $"Entering Sterkte en Frequentie.");

                // Voeg medicijn toe aan de lijst
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Clicking 'Add Medicine' button.");
                FindWithWait(By.XPath($"//table//td[contains(text(), '{dosageQuantity}')]"));

                // Stap 6: Klik op 'Voorschrijven en opslaan'
                SubmitViaJavaScript(CreatePrescriptionButton, "Clicking final 'Create Prescription' button.");

                // Controleer op succesvolle redirect naar index/detail pagina
                _wait.Until(d => d.Url.Contains(PrescriptionsIndexUrl) && !d.Url.Contains("/new"));

                log.Info($"✓ Assertion passed: Succesvolle redirect naar Index pagina ({_driver.Url}).");
                log.Info("✓ Test geslaagd: Dosering succesvol ingesteld en opgeslagen (Happy Path).");
                log.Info("=== HS-2.1.1 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-2.1.1 FAILED: {ex.Message}");
                TakeScreenshot("HS_2_1_1_Failed");
                throw;
            }
        }


        // ------------------------------------------------------------------
        // --- TC HS-2.1.2: UNHAPPY PATH (Waarschuwing bij Hoge Dosering) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("HS-2.1")]
        public void HS_2_1_2_SystemWarnsOnHighDosage()
        {
            log.Info("=== Starting HS-2.1.2: Waarschuwing bij ongebruikelijk hoge dosering (2000mg Paracetamol) ===");

            // Arrange
            string patientValue = "7";
            string medicineId = "1"; // Paracetamol (aangenomen)
            string highDosage = "2000mg"; // Sterkte (hoge dosering)
            string dosageQuantity = "1"; // Hoeveelheid (1 tablet/capsule)

            try
            {
                // Stap 1: Log in als de gebruiker en start een voorschrift
                PerformLogin(Username, Password, "Specialist");

                NavigateToNewPrescriptionForm();
                SelectPatient(patientValue);
                SetTextareaValue(DescriptionField, "Test hoge dosering waarschuwing.", "Entering Description.");

                // Activeer medicijnsectie
                SubmitViaJavaScript(AddMedicineStepButton, "Activate medicine section.");
                FindWithWait(MedicineSelect);
                SelectMedicine(medicineId);

                // Stap 2: Stel dosering (sterkte) in op de hoge waarde
                SetValueViaJavaScript(QuantityField, dosageQuantity, $"Entering Hoeveelheid: {dosageQuantity}.");
                // Stap 3: Typ '2000mg' in het veld (InstructionsField voor de sterkte)
                SetValueViaJavaScript(InstructionsField, highDosage + ", 1x daags", $"Entering Sterkte: {highDosage}.");

                // Stap 4: Observeer de reactie van het systeem
                log.Info("Wachten op de verwachte NIET-BLOKKERENDE waarschuwingsmelding...");

                // --- KERN VAN DE TEST (VERWACHTE FALEN OP WAARSCHUWING) ---

                // Zoek naar het waarschuwingsbericht (moet er zijn als de functionaliteit bestaat)
                ReadOnlyCollection<IWebElement> warningElements = _driver.FindElements(DosageWarningMessage);

                // Expected Result 1: Waarschuwing is zichtbaar
                Assert.That(warningElements.Count, Is.GreaterThan(0),
                    "FAILURE: De waarschuwing bij ongebruikelijk hoge dosering is NIET verschenen (Functionele fout: ontbrekende beslissingsondersteuning).");

                // Valideer de inhoud als de waarschuwing is gevonden
                if (warningElements.Count > 0)
                {
                    Assert.That(warningElements[0].Text,
                        Does.Contain("hoger dan gebruikelijk").IgnoreCase.Or.Contain("high dosage").IgnoreCase.Or.Contain("warning").IgnoreCase,
                        $"FAILURE: Waarschuwing is gevonden, maar de tekst is onduidelijk: '{warningElements[0].Text}'");
                    log.Info("✓ Waarschuwing is zichtbaar en de tekst is relevant (succes op AC).");
                }

                // Expected Result 2: De arts kan voorbij de waarschuwing (Niet-blokkerende check)

                // Probeer medicijn toe te voegen (moet slagen)
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Proberen toe te voegen (moet slagen, niet-blokkerend).");
                FindWithWait(By.XPath($"//table//td[contains(text(), '{highDosage}')]")); // Wacht op toevoeging in de lijst

                // Probeer op te slaan (moet ook slagen)
                SubmitViaJavaScript(CreatePrescriptionButton, "Clicking final 'Create Prescription' button.");
                _wait.Until(d => d.Url.Contains(PrescriptionsIndexUrl) && !d.Url.Contains("/new"));

                log.Info("✓ Assertion passed: Systeem blokkeert niet en voorschrift is opgeslagen (Bevestigt professionele autonomie).");
                log.Info("=== HS-2.1.2 PASSED: De niet-blokkerende logica is correct. (De test FAALT als de waarschuwing niet verschijnt.) ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-2.1.2 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("HS_2_1_2_Failed");
                throw; // Gooi de fout om de falende test te registreren
            }
        }
    }
}