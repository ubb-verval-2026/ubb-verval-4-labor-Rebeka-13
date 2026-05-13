using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [TestCase(10, 5500)]
    [TestCase(5, 5250)]
    [TestCase(0, 5000)]
    [TestCase(-5, 4750)]
    [TestCase(-10, 4500)]
    public void Person_SalaryIncrease_ShouldIncrease(double percentage, double expectedSalary)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        
        wait.Until(d => {
            try
            {
                var input = d.FindElement(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"));
                input.Clear();
                input.SendKeys(percentage.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();


        // Assert
        wait.Until(d =>
        {
            var label = d.FindElement(By.XPath("//*[@data-test='DisplayedSalary']"));

            return double.TryParse(label.Text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val)
            && Math.Abs(val - expectedSalary) <= 0.001;
        });
    }

    [Test]
    public void Person_SaladyIncrease_WithInvalidPercentage_ShouldShowValidationErrors()
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("[data-test='SalaryIncreasePercentageInput']"));
            el.Clear();
            el.SendKeys("-11");
            return true;
        });

        // Act
        wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']"))).Click();
        
        // Assert - ValidationMessage az oldal tetejen
        var validationSummary = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("li.validation-message")));
        validationSummary.Text.Should().NotBeNullOrEmpty();

        // Assert - ValidationMessage a mezo alatt
        var validationMessage = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div.validation-message")));
        validationMessage.Text.Should().NotBeNullOrEmpty();

    }
    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}