using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Collections.ObjectModel;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (HS-1.9 Autorisatie Voorschrijven)
 * ===================================================================================
 * Dit testbestand valideert de toegangscontrole voor de 'Nieuw Voorschrift' 
 * functionaliteit (Acceptance Criteria HS-1.9).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. HS_1_9_1_UserWithCorrectRoleCanPrescribe (Happy Path):
 * - Rapporteert of een 'Specialist' de '/Prescriptions/new' link 
 * kan zien en de pagina succesvol kan openen.
 *
 * 2. HS_1_9_2_UserWithIncorrectRoleCannotPrescribe (Unhappy Path):
 * - Rapporteert of een 'Patient' de '/Prescriptions/new' link 
 * NIET kan zien.
 * - Valideert dat de 'Patient' wordt geredirect naar zijn eigen 
 * overzicht ('/MyPrescriptions') als hij de URL toch probeert te 
 * benaderen (URL-manipulatie).
 * - Dit is een kritieke veiligheidstest.
 * ===================================================================================
 */

namespace Tests.Specialist
{
    [TestFixture]
    public class HS_1_9_autorisatie_voorschrijven : BaseTest
    {
        // Gebruikersgegevens
        private const string SpecialistUsername = "specialist";
        private const string SpecialistPassword = "specialist1";
        private const string PatientUsername = "patient";
        private const string PatientPassword = "Patient1"; // Wachtwoord gecorrigeerd

        // Locators
        private static readonly By NewPrescriptionLink = By.CssSelector("a[href='/Prescriptions/new']");
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // Elementen op de doelpagina
        private static readonly By CreatePrescriptionButton = By.CssSelector("button.btn.btn-primary[type='submit']");
        private static readonly By PatientSelect = By.Id("patientSelect");

        // Verwachte en kritieke URLs
        private const string PrescriptionsIndexUrl = "/Prescriptions";
        private const string NewPrescriptionFormUrl = "/Prescriptions/new";
        private const string PatientPrescriptionsUrl = "/Prescriptions/MyPrescriptions";

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

        // *** FIX: Robuuste navigatie door te wachten op de body tag ***
        private void NavigateToPrescriptionsIndex()
        {
            log.Info("Navigating to Prescriptions Index to observe UI...");
            _driver.Navigate().GoToUrl(_baseUrl + PrescriptionsIndexUrl);

            // Wacht tot de URL de Index bevat
            _wait.Until(d => d.Url.Contains(PrescriptionsIndexUrl));

            // Wacht op de body-tag om te zorgen dat de DOM geladen is
            FindWithWait(By.TagName("body"));
            log.Info("DOM structure confirmed loaded.");
        }

        // ------------------------------------------------------------------
        // --- TC HS-1.9.1: HAPPY PATH (Arts kan voorschrijven) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("HS-1.9")]
        public void HS_1_9_1_UserWithCorrectRoleCanPrescribe()
        {
            log.Info("=== Starting HS-1.9.1: Gebruiker met rol 'Arts' kan voorschrijven ===");

            try
            {
                // Stap 1: Log in als de gebruiker met de rol 'Arts'
                PerformLogin(SpecialistUsername, SpecialistPassword, "Specialist");

                // Stap 2: Navigeer naar Prescriptions Index
                NavigateToPrescriptionsIndex();

                // Expected Result 1: De knop 'Nieuw voorschrift' is zichtbaar
                log.Info("Stap 3: Waiting for visibility of 'Nieuw voorschrift' link...");
                IWebElement newPrescriptionLink = FindWithWait(NewPrescriptionLink); // FIX: Wacht nu robuuster

                Assert.That(newPrescriptionLink.Displayed, Is.True, "FAILURE: De 'Nieuw voorschrift' knop is niet zichtbaar voor de Arts.");
                log.Info("✓ Knop 'Nieuw voorschrift' is zichtbaar.");

                // Stap 4: Klik de link
                newPrescriptionLink.Click();

                // Expected Result 2: Navigatie naar de juiste URL (Eerst wachten op een uniek formulier element)
                log.Info("Waiting for destination page element (Patient Select) to load...");
                FindWithWait(PatientSelect);

                // Valideer de uiteindelijke URL
                _wait.Until(d => d.Url.Contains(NewPrescriptionFormUrl));
                log.Info($"✓ Assertion passed: Succesvol genavigeerd naar: {_driver.Url}");

                log.Info("=== HS-1.9.1 PASSED: Positieve autorisatie-check is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.9.1 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_9_1_Failed");
                throw;
            }
        }


        // ------------------------------------------------------------------
        // --- TC HS-1.9.2: UNHAPPY PATH (Patient kan niet voorschrijven) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("HS-1.9")]
        public void HS_1_9_2_UserWithIncorrectRoleCannotPrescribe()
        {
            log.Info("=== Starting HS-1.9.2: Gebruiker met rol 'patient' kan niet voorschrijven ===");

            try
            {
                // Stap 1: Log in als de gebruiker met de rol 'patient'
                PerformLogin(PatientUsername, PatientPassword, "Patient");

                // Stap 2: Open patiëntdossier (Navigeer naar Prescriptions Index)
                NavigateToPrescriptionsIndex();

                // Expected Result 1: De knop 'Nieuw voorschrift' is niet zichtbaar
                log.Info("Stap 3: Checking that 'Nieuw voorschrift' link is NOT visible...");

                ReadOnlyCollection<IWebElement> elements = _driver.FindElements(NewPrescriptionLink);

                Assert.That(elements.Count, Is.EqualTo(0), "FAILURE: De 'Nieuw voorschrift' knop is zichtbaar/bestaat voor een Patient.");
                log.Info("✓ Assertion passed: 'Nieuw voorschrift' knop is niet zichtbaar op de indexpagina.");


                // Expected Result 2: Als de gebruiker de URL direct zou benaderen, resulteert dit in een blokkade
                log.Info("Stap 4: Proberen de verboden URL direct te benaderen...");
                _driver.Navigate().GoToUrl(_baseUrl + NewPrescriptionFormUrl);

                // Assertie: De gebruiker is niet op de target URL. Redirect moet plaatsvinden.
                _wait.Until(d => !d.Url.Contains(NewPrescriptionFormUrl));

                string currentUrl = _driver.Url;

                // Controleer op de verwachte redirects (/Prescriptions of /Prescriptions/MyPrescriptions)
                Assert.That(
                    currentUrl,
                    Does.Contain(PrescriptionsIndexUrl).IgnoreCase.Or.Contain(PatientPrescriptionsUrl).IgnoreCase,
                    $"FAILURE: Gebruiker is niet correct geblokkeerd/omgeleid. Huidige URL: {currentUrl}");

                log.Info($"✓ Assertion passed: Gebruiker is geblokkeerd en correct omgeleid. Huidige URL: {currentUrl}");

                log.Info("=== HS-1.9.2 PASSED: Negatieve autorisatie-check is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"HS-1.9.2 FAILED: {ex.Message}");
                TakeScreenshot("HS_1_9_2_Failed");
                throw;
            }
        }
    }
}