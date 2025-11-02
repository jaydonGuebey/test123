using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI; 
using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using Tests;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (BE-2.2 Log Validatie)
 * ===================================================================================
 * Dit testbestand valideert de integriteit van de loggingfunctionaliteit
 * (Acceptance Criteria BE-2.2).
 *
 * Wat wordt gerapporteerd:
 *
 * 1. BE_2_2_1_LogRoleChangeTest:
 * - Rapporteert of een kritieke actie (het wijzigen van een gebruikersrol) 
 * daaadwerkelijk wordt vastgelegd in de logbestanden (/LogFiles).
 * - De test telt het aantal regels VÓÓR de actie.
 * - De test telt het aantal regels NÁ de actie.
 * - De test slaagt als het aantal regels is toegenomen (bv. +1), 
 * wat bewijst dat de audit trail werkt.
 * ===================================================================================
 */

namespace Tests.Beheerder
{
    [TestFixture]
    public class BE_2_2_logfiles_validatie : BaseTest // KLASNAAM BE_2_2
    {
        // Gebruikersgegevens
        private const string AdminUsername = "admin";
        private const string AdminPassword = "admin1";

        // Testdata
        private const string TargetUserName = "Patient";
        private const string TargetUserId = "7";
        private const string UserManagementUrl = "/Users";
        private const string LogFilesUrl = "/LogFiles";

        // Locators
        private static readonly By LoginUsernameField = By.Name("Username");
        private static readonly By LoginPasswordField = By.Name("Password");
        private static readonly By LoginButton = By.Name("btn-login");

        // Locators op de Users pagina
        private static readonly By UsersTable = By.ClassName("table");
        private static By GetRoleSelect(string userId) => By.XPath($"//input[@name='UserId' and @value='{userId}']/following-sibling::select[@name='NewRole']");
        private static By GetChangeButton(string userId) => By.XPath($"//input[@name='UserId' and @value='{userId}']/following-sibling::button[contains(text(), 'Change')]");

        // NIEUWE LOCATORS voor LogFiles pagina
        private static readonly By LogFileDropdownInput = By.Id("logFileDropdown"); // Input veld
        private static readonly By LogContentTextarea = By.Id("logContent");       // Textarea met loginhoud
        private static readonly By FirstLogFileButton = By.XPath("//div[@id='dropdownList']/button[1]"); // Eerste logbestand in de dropdown

        // --- HELPERFUNCTIES ---

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

        private void SubmitViaJavaScript(By by, string stepDescription)
        {
            LogStep(stepDescription);
            var element = FindWithWait(by);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            log.Info("Clicked element via JavaScript (forced submit/click).");
        }

        private int CountLogLines(string logContent)
        {
            // Telt het aantal niet-lege regels (cruciaal voor nauwkeurigheid)
            if (string.IsNullOrWhiteSpace(logContent)) return 0;
            return logContent.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
        }

        // NIEUWE FUNCTIE: Selecteert het eerste logbestand en retourneert de naam
        private string SelectFirstLogFile()
        {
            // Open dropdown
            FindWithWait(LogFileDropdownInput).Click();

            // Wacht tot de knopjes in de dropdown zichtbaar zijn
            _wait.Until(d => d.FindElements(By.XPath("//div[@id='dropdownList']/button")).Count > 0);
            ReadOnlyCollection<IWebElement> buttons = _driver.FindElements(By.XPath("//div[@id='dropdownList']/button"));

            // Kies de één-na-laatste (veiligheidsfallbacks als er te weinig items zijn)
            IWebElement chosen;
            if (buttons.Count >= 2)
            {
                chosen = buttons[buttons.Count - 2]; // tweede van achteren
            }
            else if (buttons.Count == 1)
            {
                chosen = buttons[0];
            }
            else
            {
                // fallback naar de eerste bekende locator als er geen knopjes gevonden zijn
                chosen = FindWithWait(FirstLogFileButton);
            }

            string logFileName = chosen.Text;

            // Klik op het gekozen logbestand (forceer click voor stabiliteit)
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", chosen);

            // Wacht tot de textarea gevuld is met de loginhoud
            _wait.Until(d => !string.IsNullOrWhiteSpace(d.FindElement(LogContentTextarea).GetAttribute("value")));

            return logFileName;
        }


        // ------------------------------------------------------------------
        // --- TC BE-2.2.1: LOGGING (Rolwijziging genereert log) ---
        // ------------------------------------------------------------------

        [Test]
        [Category("Happy Path")]
        [Category("BE-2.2")]
        public void BE_2_2_1_LogRoleChangeTest()
        {
            log.Info("=== Starting BE-2.2.1: Verifiëren dat rolwijziging een logregel genereert via de logviewer ===");
            int initialLineCount = 0;
            string targetLogFile;

            try
            {
                // Stap 1: Log in als 'Admin Jansen'
                PerformLogin(AdminUsername, AdminPassword, "Admin");

                // Stap 2: Navigeer naar /LogFiles en sla de huidige inhoud op
                log.Info($"Navigeren naar {LogFilesUrl} om initiële loginhoud te tellen.");
                _driver.Navigate().GoToUrl(_baseUrl + LogFilesUrl);

                // Selecteer logbestand en tel de regels
                targetLogFile = SelectFirstLogFile();
                string logContent = FindWithWait(LogContentTextarea).GetAttribute("value");
                initialLineCount = CountLogLines(logContent);

                log.Info($"Logbestand '{targetLogFile}' geselecteerd. Initiële regels: {initialLineCount}.");


                // Stap 3: Rolwijziging uitvoeren (Actie die een logregel moet genereren)
                NavigateToUserManagement();

                // Selecteer de rol voor de target gebruiker (Patient)
                By roleSelectLocator = GetRoleSelect(TargetUserId);
                var selectElement = new SelectElement(FindWithWait(roleSelectLocator));

                // Wissel de rol om een log entry te garanderen
                string currentRole = selectElement.SelectedOption.GetAttribute("value");
                string newRole = (currentRole == "patient") ? "specialist" : "patient";

                log.Info($"Wisselen van rol van '{currentRole}' naar '{newRole}'.");
                selectElement.SelectByValue(newRole);

                // Klik op 'Change' knop om op te slaan
                By changeButtonLocator = GetChangeButton(TargetUserId);
                SubmitViaJavaScript(changeButtonLocator, "Clicking 'Change' button to save the new role.");

                // Wacht tot de pagina is teruggekeerd naar /Users
                _wait.Until(d => d.Url.Contains(UserManagementUrl));


                // Stap 4: Ga terug naar /LogFiles en controleer
                log.Info($"Navigeren terug naar {LogFilesUrl} en selecteer '{targetLogFile}' opnieuw.");
                _driver.Navigate().GoToUrl(_baseUrl + LogFilesUrl);

                // Filter en selecteer het OORSPRONKELIJKE logbestand opnieuw om de meest recente inhoud te zien
                FindWithWait(LogFileDropdownInput).SendKeys(targetLogFile);
                FindWithWait(FirstLogFileButton).Click();

                // Wacht tot de textarea gevuld is met nieuwe content
                _wait.Until(d => !string.IsNullOrWhiteSpace(d.FindElement(LogContentTextarea).GetAttribute("value")));

                string newLogContent = FindWithWait(LogContentTextarea).GetAttribute("value");
                int finalLineCount = CountLogLines(newLogContent);

                log.Info($"Nieuwe regels geteld: {finalLineCount}.");


                // Stap 5: Valideer of er precies één regel is toegevoegd
                Assert.That(finalLineCount, Is.EqualTo(initialLineCount + 1),
          $"FAILURE: Logregel is NIET correct toegevoegd. Verwacht: {initialLineCount + 1} regels, Gevonden: {finalLineCount} regels.");

                log.Info("✓ Assertie geslaagd: Exact één nieuwe logregel is toegevoegd. Logging werkt.");

                log.Info("=== BE-2.2.1 PASSED: Rolwijziging genereert een logregel ===");
            }
            catch (Exception ex)
            {
                log.Error($"BE-2.2.1 FAILED: {ex.Message}");
                TakeScreenshot("BE_2_2_1_Failed");
                throw;
            }
        }
    }
}