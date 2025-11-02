using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Collections.ObjectModel;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (BE-2.4 Log Autorisatie)
 * ===================================================================================
 * Dit testbestand valideert de toegangscontrole (autorisatie) voor de 
 * logbestanden-pagina (Acceptance Criteria BE-2.4).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. BE_2_4_1_AuthorizedAdminHasAccessToLogs (Happy Path):
 * - Rapporteert of een geautoriseerde gebruiker ('Admin') de log-pagina 
 * kan zien en benaderen.
 *
 * 2. BE_2_4_2_UnauthorizedUserCannotAccessLogs (Unhappy Path):
 * - Rapporteert of een niet-geautoriseerde gebruiker ('healthinsurer') 
 * GEEN toegang heeft tot de logs.
 * - Valideert dat de menu-link NIET zichtbaar is.
 * - Valideert dat directe URL-toegang (URL-manipulatie) wordt geblokkeerd.
 * - Als deze test faalt, is er een kritiek beveiligingslek.
 * ===================================================================================
 */

namespace Tests.Beheerder
{
    [TestFixture]
    public class BE_2_4_log_autorisatie : BaseTest
    {
        // Gebruikersgegevens
        private const string AdminUsername = "admin"; // Geautoriseerde gebruiker
        private const string AdminPassword = "admin1";

        // Ongeautoriseerde gebruiker (Health Insurer/Zorgverzekeraar)
        private const string UnauthorizedUsername = "healthinsurer";
        private const string UnauthorizedPassword = "healthinsurer1";

        // Locators & URLs
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        private const string LogFilesUrl = "/LogFiles";
        private static readonly By AuditTrailMenuLink = By.CssSelector("a[href='/LogFiles']");

        // Elementen om succesvolle toegang te verifiëren (Gebruikt voor de Happy Path)
        private static readonly By LogContentTextarea = By.Id("logContent");


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
            Thread.Sleep(500);
        }

        // ------------------------------------------------------------------
        // --- TC BE-2.4.1: HAPPY PATH (Geautoriseerde toegang) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("BE-2.4")]
        public void BE_2_4_1_AuthorizedAdminHasAccessToLogs()
        {
            log.Info("=== Starting BE-2.4.1: Geautoriseerde beheerder (Admin) heeft toegang tot logs ===");

            try
            {
                // Stap 1: Log in als 'Admin Jansen'
                PerformLogin(AdminUsername, AdminPassword, "Admin");

                // Stap 2: Zoek in het navigatiemenu naar 'Audittrail'
                IWebElement menuLink = FindWithWait(AuditTrailMenuLink);

                Assert.That(menuLink.Displayed, Is.True, "FAILURE: Het 'Audittrail' menu-item is niet zichtbaar.");

                // Stap 3: Klik op het menu-item
                menuLink.Click();

                // Stap 4 & Expected Result 2: De Audittrail-module laadt succesvol
                _wait.Until(d => d.Url.Contains(LogFilesUrl));
                FindWithWait(LogContentTextarea);

                log.Info("✓ Assertion passed: Audittrail pagina is succesvol geladen.");

                log.Info("=== BE-2.4.1 PASSED: Positieve toegangscontrole is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-2.4.1 FAILED: {ex.Message}");
                throw;
            }
        }

        // ------------------------------------------------------------------
        // --- TC BE-2.4.2: UNHAPPY PATH (Ongeautoriseerde toegang) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Unhappy Path")]
        [Category("BE-2.4")]
        public void BE_2_4_2_UnauthorizedUserCannotAccessLogs()
        {
            log.Info("=== Starting BE-2.4.2: Ongeautoriseerde gebruiker (Zorgverzekeraar) heeft geen toegang tot logs ===");

            try
            {
                // Stap 1: Log in als 'Zorgverzekeraar'
                PerformLogin(UnauthorizedUsername, UnauthorizedPassword, "Zorgverzekeraar");

                // Stap 2: Controleer of het menu-item NIET zichtbaar is.
                ReadOnlyCollection<IWebElement> menuElements = _driver.FindElements(AuditTrailMenuLink);
                Assert.That(menuElements.Count, Is.EqualTo(0), "FAILURE: Het 'Audittrail' menu-item is onterecht zichtbaar.");
                log.Info("✓ 'Audittrail' menu is niet zichtbaar.");


                // Stap 3 & 4: Probeer de directe URL in te voeren
                log.Info($"Stap 3/4: Probeer de directe verboden URL {LogFilesUrl} te benaderen.");
                NavigateAndWaitForUrlChange(LogFilesUrl);

                string currentUrl = _driver.Url;

                // --- CRUCIALE ASSERTIE: FAAL HIER ALS DE BEVEILIGING SCHENDT ---
                if (currentUrl.Contains(LogFilesUrl))
                {
                    // Als de gebruiker na de poging op de verboden pagina is gebleven, is dit een fout.
                    Assert.Fail($"BEVEILIGINGSFOUT: Ongeautoriseerde gebruiker heeft onterecht toegang tot de logbestanden op URL: {currentUrl}");
                }

                // Valideer dat de gebruiker is geredirect naar een veilige/generieke landingspagina (Index/Home/Login)
                Assert.That(currentUrl,
                    Does.Not.Contain(LogFilesUrl).IgnoreCase,
                    $"FAILURE: Gebruiker is geblokkeerd, maar de uiteindelijke URL klopt niet.");

                log.Info($"✓ Assertion passed: Directe URL-toegang is correct geblokkeerd en geredirect naar {currentUrl}.");

                log.Info("=== BE-2.4.2 PASSED: Negatieve toegangscontrole is OK ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE_2_4_2 FAILED: {ex.Message}");
                TakeScreenshot("BE_2_4_2_Failed");
                throw;
            }
        }
    }
}