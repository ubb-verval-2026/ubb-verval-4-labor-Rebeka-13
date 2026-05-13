using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatesAndStuff.Web.Tests
{
    [TestFixture]
    public class BlazeDemoTests
    {
        private IWebDriver driver;
        private StringBuilder verificationErrors;
        private const string BaseURL = "https://blazedemo.com";

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

        [Test]
        public void Flights_BetweenMexicoCityAndDublin_AtLeastThreeAvailable()
        {
            // Arrange
            driver.Navigate().GoToUrl(BaseURL);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            driver.FindElement(By.Name("fromPort")).SendKeys("Mexico City");
            Thread.Sleep(300);
            driver.FindElement(By.Name("toPort")).SendKeys("Dublin");
            Thread.Sleep(300);

            // Act
            wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("input[type='submit']"))).Click();

            // Assert
            var rows = wait.Until(d =>
            {
                var table = d.FindElement(By.CssSelector("table.table"));
                var bodyRows = table.FindElements(By.CssSelector("tbody tr"));
                return bodyRows.Count > 0 ? bodyRows : null;
            });

            (rows.Count >= 3).Should().BeTrue();

        }
    }
}
