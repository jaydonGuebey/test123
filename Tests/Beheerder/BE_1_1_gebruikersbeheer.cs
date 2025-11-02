using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT VOOR DEZE TESTKLASSE (BE-1.1 Gebruikersbeheer)
 * ===================================================================================
 * * Dit testbestand valideert de kernfunctionaliteit van het gebruikersbeheer 
 * (Acceptance Criteria BE-1.1).
 * * Wat wordt gerapporteerd:
 * * 1. BE-1.1.1: Succesvolle Rolwijziging (Happy Path)
 * - Deze test rapporteert of een 'Admin' succesvol de rol van een 
 * bestaande gebruiker ('Patient' -> 'specialist') kan wijzigen.
 * - De test valideert dat de wijziging **persistent** is (bewaard blijft) 
 * door de pagina te refreshen en te controleren of de nieuwe rol 
 * nog steeds is geselecteerd.
 * - Als deze test faalt, betekent dit dat de beheerder geen rollen kan 
 * toekennen, wat een kritieke fout is.
 * * ===================================================================================
 */

namespace Tests.Beheerder
{
    [TestFixture]
    public class BE_1_1_gebruikersbeheer : BaseTest
    {
        // Gebruikersgegevens
        private const string AdminUsername = "admin"; // Admin Jansen
        private const string AdminPassword = "admin1";

        // Testdata
        private const string TargetUserName = "Patient"; // De gebruiker die we aanpassen (UserId=7)
        private const string TargetUserId = "7";
        private const string TargetRoleValue = "specialist"; // Rol die we toewijzen
        private const string TargetRoleDisplay = "specialist";
        private const string UserManagementUrl = "/Users";
        // private const string ExpectedSuccessMessage = "Role successfully changed"; // VERWIJDERD


        // Locators
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // Locators op de Gebruikersbeheer (Users) pagina
        private static readonly By UsersTable = By.ClassName("table");
        private static readonly By SuccessAlert = By.CssSelector("div.alert-success");

        // Locators voor de Target Gebruiker (gebaseerd op UserID)
        private static By GetRoleSelect(string userId) => By.XPath($"//input[@name='UserId' and @value='{userId}']/following-sibling::select[@name='NewRole']");
        private static By GetChangeButton(string userId) => By.XPath($"//input[@name='UserId' and @value='{userId}']/following-sibling::button[contains(text(), 'Change')]");

        // Locator om de geselecteerde rol te verifiëren
        private static By GetSelectedRoleOptionLocator(string userId, string roleValue) => By.XPath($"//input[@name='UserId' and @value='{userId}']/following-sibling::select/option[@value='{roleValue}' and @selected]");


        // ==================================================================
        // --- HELPERFUNCTIES ---
        // ==================================================================

        private void PerformLogin(string username, string password, string role)
        {
            log.Info($"Logging in as {role} user: {username}");
            NavigateToLogin();
            Type(LoginUsernameField, username, $"Entering username ({username})");
            Type(LoginPasswordField, password, "Entering password");
            Click(LoginButton, "Clicking login button");
            _wait.Until(d => d.Url != _baseUrl + "/Account/Login");
        }

        private void NavigateToUserManagement()
        {
            log.Info("Navigating to User Management...");
            _driver.Navigate().GoToUrl(_baseUrl + UserManagementUrl);
            FindWithWait(UsersTable);
        }

        // De benodigde SubmitViaJavaScript methode
        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }


        // ------------------------------------------------------------------
        // --- TC BE-1.1.1: HAPPY PATH (Toekennen van een enkele rol) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("BE-1.1")]
        public void BE_1_1_1_AssignSingleRoleToNewUser()
        {
            log.Info($"=== Starting BE-1.1.1: Toekennen van rol '{TargetRoleDisplay}' aan gebruiker '{TargetUserName}' ===");

            try
            {
                // Stap 1: Log in als 'Admin Jansen'
                PerformLogin(AdminUsername, AdminPassword, "Admin");

                // Stap 2 & 3: Navigeer naar 'Gebruikersbeheer'
                NavigateToUserManagement();

                log.Info($"Targeting user row for: {TargetUserName}");

                // Stap 4 & 5: Selecteer de rol 'specialist'
                By roleSelectLocator = GetRoleSelect(TargetUserId);
                var selectElement = new SelectElement(FindWithWait(roleSelectLocator));

                log.Info($"Selecting role '{TargetRoleDisplay}' from the dropdown.");
                selectElement.SelectByValue(TargetRoleValue);

                // Stap 6: Klik op 'Change' knop om op te slaan
                By changeButtonLocator = GetChangeButton(TargetUserId);
                SubmitViaJavaScript(changeButtonLocator, "Clicking 'Change' button to save the new role.");

                // *** NIEUWE STAP: Pagina refreshen om persistentie te checken ***
                log.Info("Refreshing page to check if role change is persistent...");
                _driver.Navigate().Refresh();
                FindWithWait(UsersTable); // Wacht tot de tabel weer geladen is


                // Stap 7 & Expected Result: Controleer of de rol na de refresh actief is
                log.Info("Verifying new role is selected in the dropdown after explicit refresh...");

                // Zoek de 'specialist' optie die de 'selected' attribuut heeft
                By selectedRoleLocator = GetSelectedRoleOptionLocator(TargetUserId, TargetRoleValue);
                IWebElement selectedRoleOption = FindWithWait(selectedRoleLocator);

                Assert.That(selectedRoleOption.GetAttribute("selected"), Is.EqualTo("true"),
                    $"FAILURE: De rol '{TargetRoleDisplay}' is NIET permanent toegekend na opslaan en refreshen.");

                log.Info($"✓ Assertion passed: Rol '{TargetRoleDisplay}' is succesvol en persistent toegekend.");

                log.Info("=== BE-1.1.1 PASSED: Rol succesvol en persistent toegekend ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-1.1.1 FAILED: {ex.Message}");
                TakeScreenshot("BE_1_1_1_Failed");
                throw;
            }
        }
    }
}