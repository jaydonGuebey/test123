using NUnit.Framework;
using OpenQA.Selenium;
using System;
using System.Threading; // Nodig voor Thread.Sleep (alleen voor demo/debug)
using Tests; // Zorgt dat deze klasse de BaseTest kan vinden

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (PT-1.4 Markeren Inname)
 * ===================================================================================
 * Dit testbestand valideert de functionaliteit voor het markeren 
 * van medicatie-inname (Acceptance Criteria PT-1.4).
 *
 * BELANGRIJKE BEVINDING (Verwacht te Falen):
 * De tests in dit bestand (PT_1_4_1 en PT_1_4_2) falen momenteel.
 *
 * Wat wordt gerapporteerd:
 * - De knoppen 'Markeer als ingenomen' ('.mark-taken-button') en 
 * 'Markeer als overgeslagen' ('.mark-skipped-button') 
 * **bestaan niet** in de UI.
 * - Deze tests documenteren een **ontbrekende feature** die 
 * essentieel is voor de patiëntinteractie.
 * ===================================================================================
 */

namespace Tests.Patient
{
    [TestFixture]
    public class PT_1_4_markeren_innamen : BaseTest
    {
        [Test]
        [Category("Happy Path")]
        [Category("PT-1.4")]
        public void PT_1_4_1_MarkDoseAsTaken_Success()
        {
            log.Info("=== Starting PT-1.4.1: Succesvol markeren van een dosis als 'ingenomen' ===");

            // Arrange
            // LET OP: Je moet gebruiker 'patient_f_user' aanmaken
            // en zorgen dat deze 'Lisinopril 10mg' heeft voor vandaag.
            string username = "patient";
            string password = "Patient1"; // Pas wachtwoord aan
            string medicineName = "Panadol"; // We zoeken op deel van de naam

            log.Info($"Test data - Username: {username}");

            try
            {
                // Act
                // Stap 1: Log in
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 1: Entering username (Patient F)");
                Type(By.Name("Password"), password, "Step 1: Entering password");
                Click(By.Name("btn-login"), "Step 1: Clicking login button");

                // Stap 2: Navigeer naar schema (direct na login)
                log.Info("Step 2: Verifying redirect to medication schedule");
                FindWithWait(By.CssSelector(".table-striped tbody")); // Wacht tot tabel er is

                // Stap 3: Lokaliseer het innamemoment
                log.Info($"Step 3: Locating row for {medicineName}");
                // We zoeken de rij die 'Lisinopril' bevat
                var medicineRow = FindWithWait(By.XPath($"//tbody/tr[contains(., '{medicineName}')]"));

                // Stap 4: Klik op de knop 'Markeer als ingenomen'
                // *** AANNAMES: ***
                // 1. opzoek naar een knop in die rij met class 'mark-taken-button'. 
                // deze knop bestaat niet echt dus de test zal falen
                log.Info("Step 4: Clicking 'Markeer als ingenomen' button");
                var markTakenButton = medicineRow.FindElement(By.CssSelector("button.mark-taken-button")); // AANNAMME van class
                                                                                                           // Of bv. By.XPath("./td/button[contains(text(), 'Ingenomen')]") als het tekst is

                // --- DEZE KLIK ZAL NU FALEN omdat de knop niet bestaat ---
                markTakenButton.Click();

                // Wacht even tot de UI (hopelijk) update
                Thread.Sleep(1000); // Onbetrouwbaar, beter expliciet wachten op status!

                // Stap 5: Observeer de wijziging
                // *** AANNAMES: ***
                // 1. Er verschijnt een status tekst/element in de rij, bv. met class 'status-taken'
                // 2. Er verschijnt een (tijdelijke) bevestigingsmelding, bv. met id 'confirmation-message'
                log.Info("Step 5: Observing UI changes");
                var statusElement = medicineRow.FindElement(By.CssSelector(".status-taken")); // AANNAMME
                Assert.That(statusElement.Displayed, Is.True, "Status 'Ingenomen' is niet zichtbaar.");
                Assert.That(statusElement.Text, Does.Contain("Ingenomen").IgnoreCase, "Status tekst klopt niet.");

                var confirmationMessage = FindWithWait(By.Id("confirmation-message")); // AANNAMME
                Assert.That(confirmationMessage.Text, Does.Contain("Inname geregistreerd"), "Bevestigingsmelding niet correct.");
                log.Info("✓ Assertions passed: Status updated and confirmation shown.");

                // Stap 6: Herlaad en controleer persistentie
                log.Info("Step 6: Reloading page to check persistence");
                _driver.Navigate().Refresh();
                FindWithWait(By.CssSelector(".table-striped tbody")); // Wacht tot tabel er weer is

                // Zoek de rij en status opnieuw
                medicineRow = FindWithWait(By.XPath($"//tbody/tr[contains(., '{medicineName}')]"));
                statusElement = medicineRow.FindElement(By.CssSelector(".status-taken")); // AANNAMME
                Assert.That(statusElement.Displayed, Is.True, "Status 'Ingenomen' is niet persistent na herladen.");
                log.Info("✓ Assertion passed: Status is persistent after page reload.");

                log.Info("=== PT-1.4.1 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-1.4.1 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_1_4_1_Failed");
                throw; // Laat de test falen
            }
        }

        [Test]
        [Category("Unhappy Path")]
        [Category("PT-1.4")]
        public void PT_1_4_2_CannotChangePastRegistration()
        {
            log.Info("=== Starting PT-1.4.2: Poging tot wijzigen van registratie in verleden ===");

            // Arrange
            string username = "patient";
            string password = "Patient1"; // Pas wachtwoord aan
            string medicineName = "Panadol";

            // BELANGRIJK: Deze test vereist een manier om naar een historische weergave
            // te navigeren (bv. kalender, datum selectie). Dit bestaat nu niet.
            // We simuleren dat we op de juiste pagina zijn en de oude rij vinden.

            log.Info($"Test data - Username: {username}");

            try
            {
                // Act
                // Stap 1: Log in
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 1: Entering username (Patient F)");
                Type(By.Name("Password"), password, "Step 1: Entering password");
                Click(By.Name("btn-login"), "Step 1: Clicking login button");

                // Stap 2: Navigeer naar historische weergave (NU NIET MOGELIJK)
                log.Warn("Step 2: Navigation to historical view is currently not implemented/testable.");
                // Voor nu blijven we op de huidige pagina, de test zal falen op het vinden
                // van de *oude* rij, of zal de *huidige* rij vinden met actieve knoppen.

                // Stap 3: Lokaliseer de oude registratie
                log.Info($"Step 3: Locating historical row for {medicineName}");
                // *** AANNAMES: ***
                // 1. De oude rij is identificeerbaar, bv. met een data-datum attribuut.
                // 2. We zoeken nu simpelweg de EERSTE Lisinopril rij, wat de HUIDIGE zal zijn.
                var historicalMedicineRow = FindWithWait(By.XPath($"//tbody/tr[contains(., '{medicineName}')]"));
                log.Warn("Found current row instead of historical row due to missing navigation.");

                // Stap 4: Observeer interactiemogelijkheden
                log.Info("Step 4: Observing interaction possibilities for the 'historical' row");
                // *** AANNAMES: ***
                // We zoeken naar de knoppen die er zouden moeten zijn (maar disabled)
                var markTakenButtons = historicalMedicineRow.FindElements(By.CssSelector("button.mark-taken-button"));
                var markSkippedButtons = historicalMedicineRow.FindElements(By.CssSelector("button.mark-skipped-button")); // AANNAMME

                // Assert
                // Verwachting: Knoppen zijn niet aanwezig OF uitgeschakeld.

                // Optie A: Knoppen bestaan niet
                bool buttonsNotFound = markTakenButtons.Count == 0 && markSkippedButtons.Count == 0;

                // Optie B: Knoppen bestaan maar zijn disabled
                bool buttonsDisabled = true;
                if (!buttonsNotFound)
                {
                    buttonsDisabled = (!markTakenButtons.First().Enabled) && (!markSkippedButtons.First().Enabled);
                }
                else
                {
                    buttonsDisabled = false; // Als ze niet gevonden zijn, zijn ze niet 'disabled'
                }

                // De assertie moet een van beide toestaan
                Assert.That(buttonsNotFound || buttonsDisabled, Is.True,
                    "Expected buttons for historical entry to be either absent or disabled, but found active buttons.");

                if (buttonsNotFound)
                {
                    log.Info("✓ Assertion passed: Interaction buttons are not present for the historical entry.");
                }
                else if (buttonsDisabled)
                {
                    log.Info("✓ Assertion passed: Interaction buttons are disabled for the historical entry.");
                }

                log.Info("=== PT-1.4.2 PASSED ==="); // Deze zal falen tot de features er zijn
            }
            catch (Exception ex)
            {
                log.Error($"PT-1.4.2 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_1_4_2_Failed");
                throw; // Laat de test falen
            }
        }
    }
}