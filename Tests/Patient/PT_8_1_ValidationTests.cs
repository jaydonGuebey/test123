using NUnit.Framework;
using OpenQA.Selenium;
using System;
using Tests; // <-- BELANGRIJK: Zorgt dat deze klasse de BaseTest kan vinden

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (PT-8.1 Login Validatie & Security)
 * ===================================================================================
 * Dit testbestand valideert de robuustheid en veiligheid van het 
 * inlogscherm (Acceptance Criteria PT-8.1).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. PT_8_1_1 (Happy Path):
 * - Rapporteert of een bekende patiënt ('patient'/'Patient1') 
 * succesvol kan inloggen.
 *
 * 2. PT_8_1_2 (Unhappy Path - Security):
 * - Rapporteert of een simpele SQL Injectie poging (' OR '1'='1') 
 * correct wordt geblokkeerd en resulteert in een foutmelding 
 * (en geen crash).
 *
 * 3. PT_8_1_3 & 4 (Unhappy Path - Validatie):
 * - Rapporteert of de HTML5 'required' validatie correct wordt 
 * getoond wanneer de gebruikersnaam of het wachtwoord leeg is.
 * ===================================================================================
 */

namespace Tests.Patient
{
    [TestFixture]
    // Deze klasse erft nu alle setup- en helper-logica van BaseTest
    public class PT_8_1_ValidationTests : BaseTest
    {
        [Test]
        [Category("Happy Path")]
        [Category("PT-8.1")]
        [Category("Authentication")]
        public void PT_8_1_1_SuccessfulLogin_WithCorrectCredentials()
        {
            log.Info("=== Starting PT-8.1.1: Succesvolle login met correcte gegevens ===");

            // Arrange
            string username = "patient";
            string password = "Patient1";
            log.Info($"Test data - Username: {username}");

            try
            {
                // Act
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 2: Locating and typing in Username field");
                Type(By.Name("Password"), password, "Step 3: Locating and typing in Password field");
                Click(By.Name("btn-login"), "Step 4: Locating and clicking login button");

                // Assert
                LogStep("Step 5: Observing system response for success");

                // We verwachten de 'user_name' te vinden als bewijs van succes
                var userNameElement = FindWithWait(By.Name("user_name"));

                Assert.That(userNameElement.Displayed, Is.True, "De gebruikersnaam is niet zichtbaar na login.");
                Assert.That(userNameElement.Text, Does.Contain(username).IgnoreCase,
                    $"Expected: De gebruikersnaam '{username}' (case-insensitive) moet in de navbar staan. Gevonden tekst: '{userNameElement.Text}'");

                log.Info("✓ Assertion passed: Username '{username}' is displayed in navbar");
                log.Info("=== PT-8.1.1 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-8.1.1 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_8_1_1_Failed"); // Maakt screenshot bij falen
                throw;
            }
        }

        [Test]
        [Category("Unhappy Path")]
        [Category("PT-8.1")]
        [Category("Security")]
        [Category("SQL Injection")]
        public void PT_8_1_2_Login_WithSQLInjection_ShouldBeBlocked()
        {
            log.Info("=== Starting PT-8.1.2: Poging tot login met SQL-injectie ===");

            // Arrange
            string sqlInjection = "' OR '1'='1";
            string password = "password";
            log.Info($"Test data - SQL Injection attempt: {sqlInjection}");

            try
            {
                // Act
                NavigateToLogin();
                Type(By.Name("Username"), sqlInjection, "Step 2: Entering SQL injection string");
                Type(By.Name("Password"), password, "Step 3: Entering random password");
                Click(By.Name("btn-login"), "Step 4: Clicking login button");

                // Assert
                LogStep("Step 5: Observing system response for failure");

                // We VERWACHTEN een foutmelding. Als we die vinden, is de test GESLAAGD.
                var errorElement = FindWithWait(By.Id("login-error"));
                log.Info("✓ System did not crash and error element was found.");

                Assert.That(errorElement.Displayed, Is.True,
                    "Expected: Een foutmelding moet zichtbaar zijn");
                Assert.That(_driver.Url, Does.Contain("loginError"),
                    "Expected: Het systeem moet toegang weigeren en 'loginError' in de URL tonen");

                log.Info("✓ Assertion passed: Error message is visible and URL is correct.");
                log.Info("=== PT-8.1.2 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-8.1.2 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_8_1_2_Failed");
                throw;
            }
        }

        [Test]
        [Category("Unhappy Path")]
        [Category("PT-8.1")]
        [Category("Validation")]
        public void PT_8_1_3_Login_WithEmptyPassword_ShouldShowValidationError()
        {
            log.Info("=== Starting PT-8.1.3: Poging tot login met leeg wachtwoordveld ===");

            // Arrange
            string username = "PietJansen";
            string password = ""; // Leeg
            log.Info($"Test data - Username: {username}, Password: [EMPTY]");

            try
            {
                // Act
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 2: Entering username 'PietJansen'");
                Type(By.Name("Password"), password, "Step 3: Entering empty password");
                Click(By.Name("btn-login"), "Step 4: Clicking login button (expecting client-side validation)");

                // Assert
                LogStep("Step 5: Verifying client-side validation message for password field");

                // We VERWACHTEN een validatiefout van de browser zelf (HTML5 'required')
                var passwordField = FindWithWait(By.Name("Password"));
                string? validationMessage = passwordField.GetAttribute("validationMessage");

                Assert.That(string.IsNullOrEmpty(validationMessage), Is.False,
                    "Expected: HTML5 validation message should be shown for empty password.");
                log.Info($"✓ Assertion passed: Validation message found: {validationMessage}");

                // We controleren ook dat we de pagina niet verlaten hebben
                Assert.That(_driver.Url, Does.Contain(_baseUrl),
                    "Expected: Driver should still be on the login page.");
                log.Info("✓ Assertion passed: System did not navigate away.");

                log.Info("=== PT-8.1.3 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-8.1.3 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_8_1_3_Failed");
                throw;
            }
        }

        [Test]
        [Category("Unhappy Path")]
        [Category("PT-8.1")]
        [Category("Validation")]
        public void PT_8_1_4_Login_WithEmptyUsername_ShouldShowValidationError()
        {
            log.Info("=== Starting PT-8.1.4: Poging tot login met leeg gebruikersnaamveld ===");

            // Arrange
            string username = ""; // Leeg
            string password = "password";
            log.Info($"Test data - Username: [EMPTY], Password: {password}");

            try
            {
                // Act
                NavigateToLogin();
                Type(By.Name("Username"), username, "Step 2: Entering empty username");
                Type(By.Name("Password"), password, "Step 3: Entering password 'password'");
                Click(By.Name("btn-login"), "Step 4: Clicking login button (expecting client-side validation)");

                // Assert
                LogStep("Step 5: Verifying client-side validation message for username field");

                // We VERWACHTEN een validatiefout van de browser zelf (HTML5 'required')
                var usernameField = FindWithWait(By.Name("Username"));
                string? validationMessage = usernameField.GetAttribute("validationMessage");

                Assert.That(string.IsNullOrEmpty(validationMessage), Is.False,
                    "Expected: HTML5 validation message should be shown for empty username.");
                log.Info($"✓ Assertion passed: Validation message found: {validationMessage}");

                // We controleren ook dat we de pagina niet verlaten hebben
                Assert.That(_driver.Url, Does.Contain(_baseUrl),
                    "Expected: Driver should still be on the login page.");
                log.Info("✓ Assertion passed: System did not navigate away.");

                log.Info("=== PT-8.1.4 PASSED ===");
            }
            catch (Exception ex)
            {
                log.Error($"PT-8.1.4 FAILED: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("PT_8_1_4_Failed");
                throw;
            }
        }
    }
}