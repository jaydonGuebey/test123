using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using Tests; 

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (HS-1.2 Validatie Voorschrift)
 * ===================================================================================
 * Dit testbestand valideert de kernfunctionaliteit en validatieregels 
 * van het 'Nieuw Voorschrift' formulier voor een Specialist 
 * (Acceptance Criteria HS-1.2).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. HS_1_2_1_CreateSuccessfulWithAllFields (Happy Path):
 * - Rapporteert of een Specialist een volledig en correct ingevuld 
 * voorschrift (incl. medicijn, dosering, instructies) succesvol 
 * kan aanmaken.
 *
 * 2. HS_1_2_2_SaveBlockedByQuantityValidationError (Unhappy Path):
 * - Rapporteert of het systeem de Specialist correct **blokkeert** * (en een foutmelding toont) wanneer het 'Quantity' (Dosering) veld 
 * leeg wordt gelaten. Dit is een cruciale veiligheidscheck.
 * ===================================================================================
 */

namespace Tests.Specialist
{
    [TestFixture]
    public class HS_1_2_voorschrift_quantity_validatie : BaseTest
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
        private static readonly By InstructionsErrorSpan = By.CssSelector("span[data-valmsg-for='Instructions']");


        // --- HELPERFUNCTIES (Voor robuuste invoer en clicks) ---
        
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
        
        // ROBUUSTE HELPER VOOR INPUT FIELDS (Quantity, Instructions)
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
            Click(NewPrescriptionLink, "Clicking 'New Prescription' link.");
            // Wacht op de formulierknop om te bevestigen dat het formulier geladen is
            FindWithWait(CreatePrescriptionButton); 
            log.Info("Successfully navigated to the prescription form.");
        }
        
        // ROBUUSTE CLICK HELPER
        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }
        
        
        // ------------------------------------------------------------------
        // --- TC HS-1.2.1: HAPPY PATH (Volledige Flow) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("HS-1.2")]
        public void HS_1_1_1_CreateSuccessfulWithAllFields()
        {
            log.Info("=== Starting HS-1.2.1: Voorschrift succesvol aangemaakt (volledige flow) ===");

            // Arrange
            string username = Username;
            string password = Password;
            string patientValue = "7";
            string descriptionText = "Succesvolle voorschrifttest voor Panadol.";
            
            try
            {
                // Act - Login
                NavigateToLogin();
                Type(By.Name("Username"), username, "Logging in as Doctor");
                Type(By.Name("Password"), password, "Entering password");
                Click(By.Name("btn-login"), "Clicking login button");

                // Stap 1: Vul de initiële velden
                NavigateToNewPrescriptionForm();
                SelectPatient(patientValue);
                SetDateValue(StartDateField, DateTime.Now.ToString("yyyy-MM-dd"), "Entering Start Date.");
                SetDateValue(EndDateField, DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), "Entering End Date.");
                SetTextareaValue(DescriptionField, descriptionText, "Entering Description.");

                // Stap 2: Klik op 'Add Medicine' om de sectie te laden
                SubmitViaJavaScript(AddMedicineStepButton, "Step 2: Clicking 'Add Medicine' via JavaScript.");
                FindWithWait(MedicineSelect);

                // Stap 3: Vul de medicijnvelden in
                SelectMedicine("1"); // Selecteer Panadol
                SetValueViaJavaScript(QuantityField, "30", "Entering Quantity.");
                SetValueViaJavaScript(InstructionsField, "1x per dag", "Entering Instructions.");

                // Stap 4: Klik op de interne 'Add' knop
                SubmitViaJavaScript(AddMedicineConfirmationButton, "Clicking the small 'Add' button to list the medicine via JavaScript.");
                FindWithWait(By.XPath("//table//td[contains(text(), 'Panadol')]"));

                // Stap 5: Klik op de finale 'Create Prescription' button
                SubmitViaJavaScript(CreatePrescriptionButton, "Clicking final 'Create Prescription' button via JavaScript.");

                // Assertie: Controleer op succesvolle redirect
                _wait.Until(d => d.Url.Contains("/Prescriptions") && !d.Url.Contains("/Prescriptions/new"));

                log.Info("✓ Assertion passed: Succesvolle navigatie na het aanmaken van het voorschrift.");
                log.Info("=== HS-1.2.1 PASSED: Voorschrift succesvol aangemaakt ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.2.1 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_2_1_Failed");
                throw;
            }
        }
        
        // ------------------------------------------------------------------
        // --- TC HS-1.2.2: UNHAPPY PATH (Dosering leeg) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("HS-1.2")]
        public void HS_1_2_2_SaveBlockedByQuantityValidationError()
        {
            log.Info("=== Starting HS-1.2.2: Opslaan geblokkeerd door ontbrekende Dosering (Quantity) ===");

            // Arrange
            string username = Username; 
            string password = Password;
            string patientValue = "7"; 
            string descriptionText = "Test voor incomplete medicijn invoer: Dosering ontbreekt.";
            
            string startDate = DateTime.Now.ToString("yyyy-MM-dd");
            string endDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            string expectedPage = "/Prescriptions/new"; 
            
            try
            {
                // Login en navigatie
                NavigateToLogin();
                Type(By.Name("Username"), username, "Logging in as Doctor");
                Type(By.Name("Password"), password, "Entering password");
                Click(By.Name("btn-login"), "Clicking login button");

                // Stap 1: Open het voorschriftformulier en vul initiële velden
                NavigateToNewPrescriptionForm();
                SelectPatient(patientValue);
                SetDateValue(StartDateField, startDate, "Entering Start Date.");
                SetDateValue(EndDateField, endDate, "Entering End Date.");
                SetTextareaValue(DescriptionField, descriptionText, "Entering Description."); 

                // Stap 2: Klik op 'Add Medicine' om de sectie te laden
                SubmitViaJavaScript(AddMedicineStepButton, "Step 2: Clicking 'Add Medicine' via JavaScript.");
                FindWithWait(MedicineSelect);

                // Stap 3: Selecteer Medicijn en vul Frequentie in
                SelectMedicine("5"); // Selecteer Lisinopril
                SetValueViaJavaScript(InstructionsField, "1x daags", "Step 4: Filling Instructions/Frequentie.");

                // Stap 4: Laat Dosering (Quantity) leeg
                log.Info("Step 3: Quantity/Dosering field intentionally left empty (cleared via JS).");
                SetValueViaJavaScript(QuantityField, "", "Dosering/Quantity is leeg."); // Verwijder de waarde

                // Stap 5: Klik op de interne 'Add' knop om validatie te triggeren
                SubmitViaJavaScript(AddMedicineConfirmationButton, "**CRUCIAAL** Step 5: Clicking internal 'Add' button to trigger validation.");

                // Assertie: Wacht expliciet totdat de foutmelding verschijnt.
                log.Info("Wachten op de validatiemelding voor Dosering (Quantity)...");
                _wait.Until(d => FindWithWait(QuantityErrorSpan).Displayed); 
                
                // Korte pauze tegen redirects.
                Thread.Sleep(250); 

                // Controle 1 (Expected Result): De pagina mag NIET zijn doorgestuurd
                var currentUrl = _driver.Url;
                Assert.That(currentUrl, Does.Contain(expectedPage).IgnoreCase.Or.Contain("/Prescriptions/New").IgnoreCase.Or.Contain("/Prescriptions/AddMedicineToPrescription"), 
                    $"FAILURE: Pagina is onterecht doorgestuurd na validatiefout. Huidige URL: {currentUrl}");

                // Controle 2 (Expected Result): Validatiemelding is zichtbaar bij het Dosering veld
                var errorSpan = FindWithWait(QuantityErrorSpan);

                Assert.That(errorSpan.Displayed, Is.True, "FAILURE: Validatiemelding is NIET zichtbaar bij Dosering.");
                Assert.That(errorSpan.Text, Does.Contain("required").IgnoreCase.Or.Contain("verplicht").IgnoreCase.Or.Contain("Value ").IgnoreCase, 
                    $"FAILURE: De foutmelding is niet duidelijk. Gevonden tekst: '{errorSpan.Text}'");

                log.Info("=== HS-1.2.2 PASSED: Blokkeren van incomplete voorschriften (Dosering) is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.2.2 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_2_2_Quantity_Failed");
                throw;
            }
        }
    }
}