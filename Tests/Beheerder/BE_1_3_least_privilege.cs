using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (BE-1.3 Least Privilege)
 * ===================================================================================
 * Dit testbestand valideert de "Least Privilege" (minimale rechten)
 * autorisatieregels voor niet-Admin rollen (Acceptance Criteria BE-1.3).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. BE_1_3_1_VerifyPatientLeastPrivilege:
 * - Rapporteert of de 'Patiënt'-rol correct is beperkt.
 * - Controleert toegang tot het eigen dossier ('/MyPrescriptions').
 * - Valideert dat admin-menu's (zoals '/Users') NIET zichtbaar zijn.
 *
 * 2. BE_1_3_2_VerifyApothecaryLeastPrivilege:
 * - Rapporteert of de 'Apotheker'-rol correct is beperkt.
 * - Valideert dat de apotheker GEEN 'Nieuw Voorschrift'-knop kan zien.
 * - Valideert dat de apotheker GEEN toegang krijgt tot 'Arts'-specifieke
 * pagina's (zoals '/Prescriptions/new') via URL-manipulatie.
 * ===================================================================================
 */

namespace Tests.Beheerder
{
    [TestFixture]
    public class BE_1_3_least_privilege : BaseTest
    {
        // Gebruikersgegevens
        private const string PatientUsername = "patient";
        private const string PatientPassword = "Patient1";
        private const string ApothecaryUsername = "apothecary";
        private const string ApothecaryPassword = "apothecary1"; // Wachtwoord voor Apotheker

        // Kritieke Locators en URLs
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // UI-Elementen om te controleren op zichtbaarheid
        private static readonly By UserManagementLink = By.CssSelector("a[href='/Users']");
        private static readonly By NewPrescriptionLink = By.CssSelector("a[href='/Prescriptions/new']");

        // URLs voor toegangscontrole
        private const string PrescriptionsIndexUrl = "/Prescriptions"; // Zichtbaar voor Apotheker
        private const string PatientOwnDossierUrl = "/Prescriptions/MyPrescriptions"; // Zichtbaar voor Patiënt (eigen dossier)
        private const string UnauthorizedDoctorUrl = "/Prescriptions/new"; // Arts-alleen (Nieuwe Voorschrift)
        private const string ConceptualOtherPatientDossier = "/Dossiers/PAT-456"; // Conceptuele verboden URL voor Patiënt


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

        private void NavigateAndWaitForUrlChange(string targetUrl)
        {
            log.Info($"Navigating to: {targetUrl}");
            _driver.Navigate().GoToUrl(_baseUrl + targetUrl);
            Thread.Sleep(500); // Geef de server tijd om de redirect te verwerken
        }


        // ------------------------------------------------------------------
        // --- TC BE-1.3.1: HAPPY PATH (Patiënt Least Privilege) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("BE-1.3")]
        public void BE_1_3_1_VerifyPatientLeastPrivilege()
        {
            log.Info("=== Starting BE-1.3.1: Verifiëren rechten 'Patiënt' rol (Least Privilege) ===");

            try
            {
                // Stap 1: Log in als 'Jan Patiënt'
                PerformLogin(PatientUsername, PatientPassword, "Patiënt");

                // Stap 2 & 3: Navigeer naar eigen dossier
                NavigateAndWaitForUrlChange(PatientOwnDossierUrl);

                // Expected Result 1: Toont automatisch het eigen dossier
                Assert.That(_driver.Url, Does.Contain(PatientOwnDossierUrl).IgnoreCase,
                    "FAILURE: Patiënt werd niet naar zijn eigen dossier gestuurd of de URL klopt niet.");
                log.Info("✓ Toegang tot eigen dossier geslaagd.");


                // Stap 4 & 5: Probeer dossier van 'Mevr. X' te openen (via URL-manipulatie)
                NavigateAndWaitForUrlChange(ConceptualOtherPatientDossier);

                // Controleer of de gebruiker op de verboden URL is (moet niet)
                string currentUrl = _driver.Url;
                Assert.That(currentUrl, Does.Not.Contain(ConceptualOtherPatientDossier).IgnoreCase,
                    $"FAILURE: Patiënt kreeg onterecht toegang tot het verboden dossier. Huidige URL: {currentUrl}");
                log.Info("✓ Poging om ander dossier te openen resulteerde in blokkade/redirect.");


                // Stap 6: Controleer of 'Jan Patiënt' het 'Gebruikersbeheer' menu-item kan zien.
                log.Info("Stap 6: Controleer of het 'Gebruikersbeheer' menu NIET zichtbaar is.");
                ReadOnlyCollection<IWebElement> adminElements = _driver.FindElements(UserManagementLink);

                // Expected Result 3: 'Gebruikersbeheer' is niet zichtbaar
                Assert.That(adminElements.Count, Is.EqualTo(0),
                    "FAILURE: Het 'Gebruikersbeheer' menu-item is zichtbaar voor de Patiënt.");
                log.Info("✓ 'Gebruikersbeheer' menu is niet zichtbaar.");

                log.Info("=== BE-1.3.1 PASSED: Patiënt rol is correct beperkt ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-1.3.1 FAILED: {ex.Message}");
                // *** FIX: Screenshot-aanroep hier toegevoegd ***
                TakeScreenshot("BE_1_3_1_Failed");
                throw;
            }
        }


        // ------------------------------------------------------------------
        // --- TC BE-1.3.2: UNHAPPY PATH (Apotheker Least Privilege) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("BE-1.3")]
        public void BE_1_3_2_VerifyApothecaryLeastPrivilege()
        {
            log.Info("=== Starting BE-1.3.2: Verifiëren dat 'Apotheker' geen 'Arts' rechten heeft ===");

            try
            {
                // Stap 1: Log in als 'Apotheker Ali'
                PerformLogin(ApothecaryUsername, ApothecaryPassword, "Apotheker");

                // Stap 2 & 3: Open het dossier van 'Jan Patiënt' en navigeer naar 'Medicatieoverzicht'
                NavigateAndWaitForUrlChange(PrescriptionsIndexUrl);

                // Expected Result 1: 'Medicatieoverzicht' (Index) is zichtbaar
                Assert.That(_driver.Url, Does.Contain(PrescriptionsIndexUrl).IgnoreCase,
                    "FAILURE: Apotheker kon de medicatie-index niet zien.");
                log.Info("✓ Toegang tot Medicatieoverzicht (Index) is geslaagd.");


                // Stap 4: Controleer of de knop 'Nieuw voorschrift' (Arts recht) NIET zichtbaar is.
                log.Info("Stap 4: Controleer of de 'Nieuw voorschrift' link NIET zichtbaar is.");
                ReadOnlyCollection<IWebElement> doctorElements = _driver.FindElements(NewPrescriptionLink);

                // Expected Result 2: De knop/module 'Nieuw voorschrift' is niet zichtbaar
                Assert.That(doctorElements.Count, Is.EqualTo(0),
                    "FAILURE: De 'Nieuw voorschrift' knop is zichtbaar voor de Apotheker (heeft onterecht Arts rechten).");
                log.Info("✓ 'Nieuw voorschrift' knop is niet zichtbaar.");


                // Stap 5: Probeer de 'Nieuwe Diagnose/Voorschrift' pagina te openen (via URL-manipulatie)
                NavigateAndWaitForUrlChange(UnauthorizedDoctorUrl);

                // Assertie: De Apotheker mag niet op de /new pagina komen.
                string currentUrl = _driver.Url;
                Assert.That(currentUrl,
                    Does.Not.Contain(UnauthorizedDoctorUrl).IgnoreCase,
                    $"FAILURE: Apotheker kreeg onterecht toegang tot de Verboden URL. Huidige URL: {currentUrl}");

                // Valideer dat de redirect naar een veilige pagina is (bv. de Index)
                Assert.That(currentUrl,
                    Does.Contain(PrescriptionsIndexUrl).IgnoreCase,
                    $"FAILURE: Apotheker werd niet naar een veilige pagina (Index) geredirect. Huidige URL: {currentUrl}");

                log.Info("✓ Poging tot directe URL-toegang resulteerde in correcte blokkade/redirect.");

                log.Info("=== BE-1.3.2 PASSED: Apotheker rol is correct beperkt ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-1.3.2 FAILED: {ex.Message}");
                // *** FIX: Screenshot-aanroep hier toegevoegd ***
                TakeScreenshot("BE_1_3_2_Failed");
                throw;
            }
        }
    }
}
