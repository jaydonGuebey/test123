using NUnit.Framework;
using OpenQA.Selenium;
using System;
using System.Linq; // Nodig voor .First() in de optionele assertie
using Tests; // Zorgt dat deze klasse de BaseTest kan vinden

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (PT-1.5 Waarschuwing Overslaan)
 * ===================================================================================
 * Dit testbestand valideert de waarschuwingen bij het overslaan 
 * van medicatie (Acceptance Criteria PT-1.5).
 *
 * BELANGRIJKE BEVINDING (Verwacht te Falen):
 * De tests in dit bestand (PT_1_5_1 en PT_1_5_2) falen momenteel.
 *
 * Wat wordt gerapporteerd:
 * - De UI-elementen voor het markeren van inname (vereist voor deze test) 
 * en de waarschuwings-elementen (bv. '#skip-alert') 
 * **bestaan niet** in de UI.
 * - Deze tests documenteren een **ontbrekende feature** met betrekking 
 * tot patiëntveiligheid en feedback.
 * ===================================================================================
 */

namespace Tests.Patient
{
    [TestFixture]
    public class PT_1_5_waarschuwing_overslaan : BaseTest
    {
        [Test]
        [Category("Happy Path")]
        [Category("PT-1.5")]
        public void PT_1_5_1_ReceiveWarningAfterSkippingDose()
        {
            log.Info("=== Starting PT-1.5.1: Ontvangen van waarschuwing na 'overgeslagen' ===");

            // Arrange
            string username = "patient";
            string password = "Patient1";
            string medicineName = "Panadol";
            string skipWarningMessage = "U heeft een dosis overgeslagen"; // Een deel van de verwachte waarschuwing

            log.Info($"Test data - Username: {username}");

            try
            {
                // Act
                // Stap 1: Log in
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 1: Entering username (Patient)");
                Type(By.Name("Password"), password, "Step 1: Entering password");
                Click(By.Name("btn-login"), "Step 1: Clicking login button");

                // Stap 2: Navigeer naar schema
                FindWithWait(By.CssSelector(".table-striped tbody"));

                // Stap 3: Lokaliseer het innamemoment (Panadol)
                log.Info($"Step 3: Locating row for {medicineName}");
                var medicineRow = FindWithWait(By.XPath($"//tbody/tr[contains(., '{medicineName}')]"));

                // Stap 4: Klik op de knop 'Markeer als overgeslagen'
                log.Info("Step 4: Clicking 'Markeer als overgeslagen'");
                var markSkippedButton = medicineRow.FindElement(By.CssSelector(".mark-skipped-button")); // AANNAMME
                markSkippedButton.Click();

                // Stap 5 & Verwachting 1: Observeer de statuswijziging
                log.Info("Step 5: Observing status change and warning");
                var statusElement = medicineRow.FindElement(By.CssSelector(".status-skipped"));
                Assert.That(statusElement.Displayed, Is.True, "Status 'Overgeslagen' is niet zichtbaar.");
                log.Info("✓ Assertion passed: Status changed to 'Overgeslagen'.");

                // Verwachting 2: Waarschuwing verschijnt
                var warningAlert = FindWithWait(By.Id("skip-alert")); // AANNAMME
                Assert.That(warningAlert.Displayed, Is.True, "Waarschuwingsmelding is NIET verschenen na overslaan.");
                Assert.That(warningAlert.Text, Does.Contain(skipWarningMessage).IgnoreCase,
                    $"Waarschuwingstekst is onjuist. Verwacht: '{skipWarningMessage}'. Gevonden: '{warningAlert.Text}'");
                log.Info("✓ Assertion passed: Correct warning message is displayed.");

                log.Info("=== PT-1.5.1 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-1.5.1 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_1_5_1_Failed");
                throw;
            }
        }

        [Test]
        [Category("Unhappy Path")]
        [Category("PT-1.5")]
        public void PT_1_5_2_NoWarningWhenDoseIsTaken()
        {
            log.Info("=== Starting PT-1.5.2: Geen waarschuwing bij markeren als 'ingenomen' ===");

            // Arrange
            string username = "patient";
            string password = "Patient1";
            string medicineName = "Panadol";
            string confirmationMessage = "Inname geregistreerd"; // Positieve bevestiging

            log.Info($"Test data - Username: {username}");

            try
            {
                // Act
                // Stap 1: Log in
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 1: Entering username (Patient)");
                Type(By.Name("Password"), password, "Step 1: Entering password");
                Click(By.Name("btn-login"), "Step 1: Clicking login button");

                // Stap 2: Navigeer naar schema
                FindWithWait(By.CssSelector(".table-striped tbody"));

                // Stap 3: Lokaliseer het innamemoment (Panadol)
                log.Info($"Step 3: Locating row for {medicineName}");
                var medicineRow = FindWithWait(By.XPath($"//tbody/tr[contains(., '{medicineName}')]"));

                // Stap 4: Klik op de knop 'Markeer als ingenomen'
                log.Info("Step 4: Clicking 'Markeer als ingenomen'");
                var markTakenButton = medicineRow.FindElement(By.CssSelector(".mark-taken-button")); // AANNAMME
                markTakenButton.Click();

                // Verwachting 1: Status verandert naar 'Ingenomen'
                log.Info("Step 5: Observing status change and checking for warning");
                var statusElement = medicineRow.FindElement(By.CssSelector(".status-taken"));
                Assert.That(statusElement.Displayed, Is.True, "Status 'Ingenomen' is niet zichtbaar.");

                // Verwachting 2: GEEN waarschuwing
                // Controleer of het waarschuwings-element NIET bestaat
                var warningAlerts = _driver.FindElements(By.Id("skip-alert")); // Zoek de waarschuwing
                Assert.That(warningAlerts.Count, Is.EqualTo(0), "Waarschuwingsmelding is onterecht verschenen na 'ingenomen'.");
                log.Info("✓ Assertion passed: No skip warning alert was displayed.");

                // Optionele Assertie: Controleer op POSITIEVE bevestiging (zoals 'Inname geregistreerd')
                var confirmationMessages = _driver.FindElements(By.Id("confirmation-message")); // AANNAMME
                if (confirmationMessages.Count > 0)
                {
                    Assert.That(confirmationMessages.First().Text, Does.Contain(confirmationMessage), "Positieve bevestiging is onjuist.");
                    log.Info("✓ Optional: Correct positive confirmation message found.");
                }

                log.Info("=== PT-1.5.2 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-1.5.2 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_1_5_2_Failed");
                throw;
            }
        }
    }
}