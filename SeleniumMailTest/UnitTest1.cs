using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace SeleniumMailTest
{
    [TestClass]
    public class UnitTest1
    {        
        FirefoxDriver firefox;
        Uri mailBoxURI = new Uri("http://mail.yandex.ru");
        Uri logoutURI = new Uri("https://passport.yandex.ru/passport?mode=embeddedauth&action=logout&uid=442984194&yu=2214860301481491904&retpath=http%3A%2F%2Fwww.yandex.ru");
        string login = "al.al.pletnev.testmailbox@yandex.ru";
        string pass = "Pg%e1xMe";

        string folder = "Входящие";
        string from = "hello@yandex.ru";
        string subject = "Соберите всю почту в этот ящик";
        string text = "";

        [TestMethod]
        public void FirefoxYandexMailTestMethod()
        {
            try
            {
                FirefoxOptions options = new FirefoxOptions();
                options.SetPreference("geo.enabled", false);                    //выключал геолокацию, чтобы не маячила панелька с запросом
                Console.WriteLine("Set parameter: " + "geo.enabled = false");
                firefox = new FirefoxDriver(options);

                firefox.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));   //на случай плохой сети. 30 - не оптимальное значение, но оставим для отладки

                firefox.Navigate().GoToUrl(mailBoxURI);                         //идем по ссылке и логинимся
                Console.WriteLine("Opened URI: " + mailBoxURI);
                firefox.FindElementByName("login").SendKeys(login);
                Console.WriteLine("Set login: " + login);
                firefox.FindElementByName("passwd").SendKeys(pass);
                Console.WriteLine("Set password: " + pass);
                firefox.FindElementByXPath                                      //ищем и кликаем кнопку "Войти"
                    ("//button[@class=' nb-button _nb-action-button nb-group-start'][@type='submit']").Click();
                Console.WriteLine("Clicked element found by XPath: '//button[@class=' nb-button _nb-action-button nb-group-start'][@type='submit']'");                              
               
                Console.WriteLine("Searching element " + "nb-4" + ", timeout 30.0 sec...");
                var fwe = new WebDriverWait(firefox, TimeSpan.FromSeconds(30.0));
                fwe.Until(ExpectedConditions.ElementExists(By.Id("nb-4")));     //ждем появления кнопки "Найти"
 
                Console.WriteLine("Element " + "nb-4" + " found");
                fwe.Until(ExpectedConditions.ElementExists(By.XPath("//input[@class='js-search-input js-allow-shortcuts _nb-input-controller']"))).SendKeys("folder: " + folder + " from: '" + from + "' subject: '" + subject + "'");                                                         //ждем появления текстбокса для поиска и
                                                                                //ищем письмо. интересно, что яндекс.почта не поддерживает название папки, взятое в кавычки
                firefox.FindElementById("nb-4").Click();                        //ищем и кликаем кнопку "Найти"
                firefox.FindElementById("nb-4").Click();                        //еще раз кликаем, потому что на первый клик почему-то только установился курсор на заданный элемент. С кнопкой логина такого не было
                Console.WriteLine("Clicked element found by ID: " + "nb-4");

                //для упрощения логики поиска нужного письма сделаем допущение, что оно может быть только одно.
                //в противном случае нужно реализовывать поиск конкретного письма в списке, поскольку язык запросов
                //(https://yandex.ru/support/mail-new/web/letter/query-language.xml) не позволяет ограничить выдачу
                //так, чтобы гарантировать уникальность найденного письма.

                fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@data-key='box=messages-item-box']"))).Click();
                     //открываем письмо

                fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@class='b-message-body__content']")));//ждем открытия письма
                
                // проверка содержания письма
                //

                fwe.Until(ExpectedConditions.ElementExists(By.XPath("//div[@class='mail-User-Name']"))).Click();                
                //firefox.FindElementByXPath("//div[@class='mail-User-Name']").Click();               //открываем меню

                fwe.Until(ExpectedConditions.ElementExists(By.XPath("//a[@class='b-mail-dropdown__item__content '][@data-metric='Меню сервисов:Выход']")));
                firefox.FindElementByXPath("//a[@class='b-mail-dropdown__item__content '][@data-metric='Меню сервисов:Выход']").Click();                                   //выполняем логаут
                Console.WriteLine("Logged out");
                //firefox.Navigate().GoToUrl(logoutURI);                        //такой способ в яндекс.почте не срабатывает
                
                options.SetPreference("geo.enabled", true);                     //включаем геолокацию обратно
                Console.WriteLine("Restored parameter: " + "geo.enabled = true");
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
