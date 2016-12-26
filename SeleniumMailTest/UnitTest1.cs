using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.IO;
using System.Reflection;

namespace SeleniumMailTest
{
    [TestClass]
    public class UnitTest1
    {        
        
        FirefoxDriver firefox;        
        
        static Uri mailBoxURI = new Uri("http://mail.yandex.ru");
        static Uri logoutURI = new Uri("https://passport.yandex.ru/passport?mode=embeddedauth&action=logout&uid=442984194&yu=2214860301481491904&retpath=http%3A%2F%2Fwww.yandex.ru");
        static string login = "al.al.pletnev.testmailbox@yandex.ru";
        static string pass = "Pg%e1xMe";

        static string folder = "Входящие";
        static string from = "hello@yandex.ru";
        static string subject = "Соберите всю почту в этот ящик";
        static string text = "";

        //FirefoxDriver firefox;
        //FirefoxOptions options;

        static void loadTestSettings()
        {
            Assembly _assembly;
            Stream _xmlStream;
            StreamReader _textStreamReader;
            
            _assembly = Assembly.GetExecutingAssembly();
            _xmlStream = _assembly.GetManifestResourceStream("SeleniumMailTest.config.xml");
            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("SeleniumMailTest.Text.txt"));

            if (_textStreamReader.Peek() != -1)
            {
                text = _textStreamReader.ReadLine();
            }
            else Log("Cannot read expected Text value from config");
            
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(_xmlStream);

            string mailBoxURI = xmlConfig.DocumentElement.SelectSingleNode("MailboxURI").InnerText;
            string login = xmlConfig.DocumentElement.SelectSingleNode("Login").InnerText;
            string pass = xmlConfig.DocumentElement.SelectSingleNode("Password").InnerText;
            string folder = xmlConfig.DocumentElement.SelectSingleNode("Folder").InnerText;
            string from = xmlConfig.DocumentElement.SelectSingleNode("From").InnerText;
            string subject = xmlConfig.DocumentElement.SelectSingleNode("Subject").InnerText;
        }

        static void Log(string text)
        {
            Console.WriteLine(text);
        }

        static FirefoxDriver initiateFirefoxDriver()
        {
            FirefoxOptions options = new FirefoxOptions();
            options.SetPreference("geo.enabled", false);    //выключал геолокацию, чтобы не маячила панелька с запросом
            Log("Set parameter: " + "geo.enabled = false");
            FirefoxDriver driver = new FirefoxDriver(options);
            driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));   //на случай плохой сети. 30 - не оптимальное значение, но оставим для отладки  
           
            return driver;
        }

        static void restoreBrowserPrefs()
        {
            FirefoxOptions options = new FirefoxOptions();
            options.SetPreference("geo.enabled", true);                     //включаем геолокацию обратно
            
            Log("Restored parameter: " + "geo.enabled = true");
        }

        static void logInToMailbox(FirefoxDriver driver, string link, string login, string password)
        {
            driver.Navigate().GoToUrl(link);                //идем по ссылке и логинимся
            Log("Opened URI: " + link);
            driver.FindElementByName("login").SendKeys(login);
            Log("Set login: " + login);
            driver.FindElementByName("passwd").SendKeys(password);
            Log("Set password: " + password);
            driver.FindElementByXPath                       //ищем и кликаем кнопку "Войти"
                ("//button[@class=' nb-button _nb-action-button nb-group-start'][@type='submit']").Click();
            
            Log("Logging in to mailbox...");
        }

        static void searchMail(FirefoxDriver driver, string folder, string from, string subject)
        {
            Log("Waiting for appearing of Search button");
            var fwe = new WebDriverWait(driver, TimeSpan.FromSeconds(30.0));
            fwe.Until(ExpectedConditions.ElementExists(By.Id("nb-4")));     //ждем появления кнопки "Найти"

            Log("Search button found");

            Log("Waiting for appearing of Search field");
            fwe.Until(ExpectedConditions.ElementExists(By.XPath("//input[@class='js-search-input js-allow-shortcuts _nb-input-controller']"))).SendKeys("folder: " + folder + " from: '" + from + "' subject: '" + subject + "'");                                                         //ждем появления текстбокса для поиска и
            //ищем письмо. интересно, что яндекс.почта не поддерживает название папки, взятое в кавычки
            Log("Search field found, entering search query...");
            driver.FindElementById("nb-4").Click();                        //ищем и кликаем кнопку "Найти"
            driver.FindElementById("nb-4").Click();                        //еще раз кликаем, потому что на первый клик почему-то только установился курсор на заданный элемент. С кнопкой логина такого не было
            Log("Started searching the following mail: \n" + "folder:" + folder + "\n" + "from: '" + from + "'\n" + "subject: '" + subject + "'");            
        }

        static void openMailAndCheckText(FirefoxDriver driver, string text)
        {
            var fwe = new WebDriverWait(driver, TimeSpan.FromSeconds(30.0));
            //для упрощения логики поиска нужного письма сделаем допущение, что оно может быть только одно.
            //в противном случае нужно реализовывать поиск конкретного письма в списке, поскольку язык запросов
            //(https://yandex.ru/support/mail-new/web/letter/query-language.xml) не позволяет ограничить выдачу
            //так, чтобы гарантировать уникальность найденного письма.

            Log("Waiting for search result box to be displayed...");
            fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@data-key='box=messages-item-box']"))).Click();
            //открываем письмо
            Log("Search result box is present, opening the email...");

            fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@class='b-message-body__content']")));//ждем открытия письма
            Log("Email is opened");

            Log("Checking email text...");
            // проверка содержания письма
            //
            string toCompare = fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@class='b-message-body__content']"))).GetAttribute("innerHTML");

            //toCompare = toCompare + "1";      //для проверки теста

            Assert.IsTrue(toCompare.Equals(text), "Assertion failed: Email text is incorrect!");

            if (toCompare.Equals(text))
            {
                Log("Email text is correct");
            }
            else 
            {
                Log("Email text is incorrect!");
                Log("Actual email text:\n" + toCompare);
                Log("Expected email text:\n" + text);
            }
        }

        static void logOutFromMailbox(FirefoxDriver driver)
        {
            var fwe = new WebDriverWait(driver, TimeSpan.FromSeconds(30.0));
            Log("Logging out...");
            fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@class='mail-User-Name']"))).Click();
            //firefox.FindElementByXPath("//div[@class='mail-User-Name']").Click();               //открываем меню

            fwe.Until(ExpectedConditions.ElementExists(By.XPath("//a[@class='b-mail-dropdown__item__content '][@data-metric='Меню сервисов:Выход']")));
            driver.FindElementByXPath("//a[@class='b-mail-dropdown__item__content '][@data-metric='Меню сервисов:Выход']").Click();                                   //выполняем логаут
            Log("Logged out");
            //firefox.Navigate().GoToUrl(logoutURI);                        //такой способ в яндекс.почте не срабатывает

            
        }

        [TestMethod]
        public void FirefoxYandexMailTestMethod()
        {
            try
            {
                FirefoxDriver driver = initiateFirefoxDriver();
                loadTestSettings();
                logInToMailbox(driver, mailBoxURI.ToString(), login, pass);
                searchMail(driver, folder, from, subject);
                openMailAndCheckText(driver, text);
                //logOutFromMailbox(driver);
                //restoreBrowserPrefs();                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: \n" + e.Message);
            }            
            finally
            {
                Console.ReadLine();
                TearDown();
            }            
        }
        [TestCleanup]
        public void TearDown()
        {
            firefox.Quit();
        }
    }
}
