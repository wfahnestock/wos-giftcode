// See https://aka.ms/new-console-template for more information
using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

class Program
{
    /*
     * Selenium WebDriver config
     */
    private static FirefoxDriver? _driver = null;
    private static FirefoxOptions _driverOptions = new FirefoxOptions();
    
    private static void Main()
    {
        _driverOptions.AddArguments("--headless"); // Don't show the browser
        _driver = new FirefoxDriver(_driverOptions);
        _driver.Url = "https://wos-giftcode.centurygame.com/";

        Console.Clear();
        
        Console.WriteLine("Enter the mode you wish to use:");
        Console.WriteLine("[1] Single Mode (Manually enter Player ID & Gift Code)");
        Console.WriteLine("[2] Batch Mode (Read from players.txt and codes.txt)");
        Console.WriteLine("");

        int choice;
        while (!int.TryParse(Console.ReadLine(), out choice))
        {
            Console.Write("Your choice: ");
        }

        switch (choice)
        {
            case 1:
                SingleMode();
                break;
            case 2:
                BatchMode();
                break;
        }
    }

    private static void SingleMode()
    {
        
        Console.Write("Enter your Player ID: ");

        string playerId = "";

        while (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = Console.ReadLine();
        }

        IWebElement? playerIdElement = null;

        try
        {
            // Wait for the page to load to find the first element. We assume the page is already loaded for subsequent attempts
            // at finding an element.
            WebDriverWait wait = new WebDriverWait(_driver, new TimeSpan(0, 0, 10));
            playerIdElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[1]/div[1]/div[1]/input")));
        }
        catch (NoSuchElementException ex)
        {
            Console.WriteLine("Error while trying to find Player ID element. Message:");
            Console.WriteLine("{0}", ex.Message);
            Console.ReadKey();
        }

        playerIdElement?.SendKeys(playerId);

        IWebElement? loginButtonElement = null;
        try
        {
            loginButtonElement = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[1]/div[1]/div[2]/span"));
        }
        catch (NoSuchElementException ex)
        {
            Console.WriteLine("Error while trying to find Login button element. Message:");
            Console.WriteLine("{0}", ex.Message);
            Console.ReadKey();
        }
        
        loginButtonElement.Click();

        int count = 0;
        bool playerAccountFound = false;
        
        while (count <= 4)
        {
            if (IsElementPresent(By.CssSelector(".exchange_container .main_content .roleInfo_con .exit_con[data-v-781897ff]")))
            {
                Console.WriteLine("Successfully logged in!");
                playerAccountFound = true;
                break;
            }
            
            Thread.Sleep(1000);

            count++;
        }

        /*
         * If we didn't find an account within 4 retries, assume we won't and return.
         * May occur on slow slow internet connections.
         */
        if (!playerAccountFound)
        {
            Console.WriteLine("Could not find player within 4 retries... Check Player ID and try again.");
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadLine();
            _driver.Dispose();
            Main();
        }
        
        Console.Write("Enter the Gift Code: ");

        String giftCode = "";

        while (string.IsNullOrWhiteSpace(giftCode))
        {
            giftCode = Console.ReadLine();
        }
        
        IWebElement? giftCodeInput = null;

        try
        {
            giftCodeInput = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[2]/div[1]/input"));
        }
        catch (NoSuchElementException ex)
        {
            Console.WriteLine("Error while trying to find gift code input element. Message:");
            Console.WriteLine("{0}", ex.Message);
            Console.ReadKey();
        }
        
        giftCodeInput?.SendKeys(giftCode);
        
        IWebElement? confirmButton = null;

        try
        {
            confirmButton = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[3]"));
        }
        catch (NoSuchElementException ex)
        {
            Console.WriteLine("Error while trying to find confirm button element. Message:");
            Console.WriteLine("{0}", ex.Message);
            Console.ReadKey();
        }
        
        confirmButton.Click();
        
        count = 0;
        
        while (count <= 4)
        {
            if (IsElementPresent(By.CssSelector(".message_modal .modal_content[data-v-5b685826]")))
            {
                IWebElement modal =
                    _driver.FindElement(By.CssSelector(".message_modal .modal_content[data-v-5b685826]"));

                if (modal.Text.Contains("Already claimed, unable to claim again."))
                {
                    // Message was that this gift code was already claimed.
                    Console.WriteLine("Gift code {0} was already claimed by this player!", giftCode);
                    Console.WriteLine("Press any key to return to the menu...");
                    Console.ReadLine();
                    _driver.Dispose();
                    Main();
                    break;
                }

                if (modal.Text.Contains("Gift Code not found"))
                {
                    // Message was that this gift code was invalid.
                    Console.WriteLine("Gift code '{0}' is invalid!", giftCode);
                    Console.WriteLine("Press any key to return to the menu...");
                    Console.ReadLine();
                    _driver.Dispose();
                    Main();
                    break;
                }

                Console.WriteLine("Successfully redeemed gift code: {0}!", giftCode);
                break;
            }
            
            // Wait 1s to try again.
            Thread.Sleep(1000);

            count++;
        }

        Console.WriteLine("Finished! Press any key to return to the menu...");
        Console.ReadLine();
        _driver.Dispose();
        Main();
    }

    private static void BatchMode()
    {
        string playersFile = Path.Combine(Directory.GetCurrentDirectory(), "players.txt");
        string giftCodesFile = Path.Combine(Directory.GetCurrentDirectory(), "codes.txt");
        List<string> playersList = new();
        List<string> codesList = new();

        using (StreamReader sr = new(playersFile))
        {
            Console.WriteLine("Reading players.txt...");

            if (playersList.Count <= 0)
            {
                while (sr.ReadLine() is { } line)
                {
                    playersList.Add(line.Trim());
                }
            }
            
            Console.WriteLine("Done!");
        }
        
        using (StreamReader sr = new(giftCodesFile))
        {
            Console.WriteLine("Reading codes.txt...");

            if (codesList.Count <= 0)
            {
                while (sr.ReadLine() is { } line)
                {
                    codesList.Add(line.Trim());
                }
            }
            
            Console.WriteLine("Done!");
        }

        for (int i = playersList.Count - 1; i >= 0; i--)
        {
            IWebElement? playerIdElement = null;

            try
            {
                // Wait for the page to load to find the first element. We assume the page is already loaded for subsequent attempts
                // at finding an element.
                WebDriverWait wait = new(_driver, new TimeSpan(0, 0, 10));
                playerIdElement = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[1]/div[1]/div[1]/input")));
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine("Error while trying to find Player ID element. Message:");
                Console.WriteLine("{0}", ex.Message);
                Console.ReadKey();
            }

            if (playerIdElement.GetAttribute("value") != String.Empty)
            {
                playerIdElement.Clear();
            }
            
            // Enter the player ID
            playerIdElement.SendKeys(playersList[i]);
            
            IWebElement? loginButtonElement = null;
            try
            {
                loginButtonElement = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[1]/div[1]/div[2]/span"));
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine("Error while trying to find Login button element. Message:");
                Console.WriteLine("{0}", ex.Message);
                Console.ReadKey();
            }
        
            loginButtonElement.Click();

            int count = 0;
            bool playerAccountFound = false;
        
            while (count <= 4)
            {
                if (IsElementPresent(By.CssSelector(".exchange_container .main_content .roleInfo_con .exit_con[data-v-781897ff]")))
                {
                    Console.WriteLine("Successfully logged in!");
                    playerAccountFound = true;
                    break;
                }
            
                Thread.Sleep(2000);

                count++;
            }
            
            /*
             * If we didn't find an account within 4 retries, assume we won't and return.
             * May occur on slow slow internet connections.
             */
            if (!playerAccountFound)
            {
                Console.WriteLine("Could not find player '{0}' within 4 retries... Check Player ID and try again.", playerIdElement.Text);
                // remove the player from the list
                playersList.RemoveAt(i);
                BatchMode();
            }

            foreach (string giftCode in codesList)
            {
                IWebElement? giftCodeInput = null;

                try
                {
                    giftCodeInput = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[2]/div[1]/input"));
                }
                catch (NoSuchElementException ex)
                {
                    Console.WriteLine("Error while trying to find gift code input element. Message:");
                    Console.WriteLine("{0}", ex.Message);
                    Console.ReadKey();
                }

                if (giftCodeInput.GetAttribute("value") != String.Empty)
                {
                    giftCodeInput.Clear();
                }

                giftCodeInput?.SendKeys(giftCode);
        
                IWebElement? confirmButton = null;

                try
                {
                    confirmButton = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[3]"));
                }
                catch (NoSuchElementException ex)
                {
                    Console.WriteLine("Error while trying to find confirm button element. Message:");
                    Console.WriteLine("{0}", ex.Message);
                    Console.ReadKey();
                }
        
                confirmButton.Click();
                
                count = 0;
        
                while (count <= 4)
                {
                    if (IsElementPresent(By.CssSelector(".message_modal .modal_content[data-v-5b685826]")))
                    {
                        IWebElement modal =
                            _driver.FindElement(By.CssSelector(".message_modal .modal_content[data-v-5b685826]"));
                        IWebElement? modalConfirmButton = null;

                        if (modal.Text.Contains("Already claimed, unable to claim again."))
                        {
                            // Message was that this gift code was already claimed.
                            Console.WriteLine("Gift code {0} was already claimed by this player!", giftCode);

                            try
                            {
                                modalConfirmButton = _driver.FindElement(By.XPath("/html/body/div/div/div[2]/div[2]/div[2]"));
                                modalConfirmButton.Click();
                                continue;
                            }
                            catch (NoSuchElementException ex)
                            {
                                Console.WriteLine("Error while trying to find confirm button element. Message:");
                                Console.WriteLine("{0}", ex.Message);
                                Console.ReadKey();
                            }
                        }

                        if (modal.Text.Contains("Gift Code not found"))
                        {
                            // Message was that this gift code was invalid.
                            Console.WriteLine("Gift code '{0}' is invalid!", giftCode);
                            try
                            {
                                modalConfirmButton = _driver.FindElement(By.XPath("/html/body/div/div/div[2]/div[2]/div[2]"));
                                modalConfirmButton.Click();
                                continue;
                            }
                            catch (NoSuchElementException ex)
                            {
                                Console.WriteLine("Error while trying to find confirm button element. Message:");
                                Console.WriteLine("{0}", ex.Message);
                                Console.ReadKey();
                            }
                        }

                        Console.WriteLine("Successfully redeemed gift code: {0}!", giftCode);
                        try
                        {
                            modalConfirmButton = _driver.FindElement(By.XPath("/html/body/div/div/div[2]/div[2]/div[2]"));
                            modalConfirmButton.Click();
                            continue;
                        }
                        catch (NoSuchElementException ex)
                        {
                            Console.WriteLine("Error while trying to find confirm button element. Message:");
                            Console.WriteLine("{0}", ex.Message);
                            Console.ReadKey();
                        }
                    }
                    
                    Thread.Sleep(500);

                    count++;
                }
            }
            
            Console.WriteLine("Finished processing {0} gift codes for player {1}", codesList.Count, playersList[i]);
            IWebElement? retreatButton = null;
            try
            {
                retreatButton = _driver.FindElement(By.XPath("/html/body/div/div/div/div[3]/div[2]/div[1]/div[2]"));
                retreatButton.Click();
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine("Error while trying to find retreat button element. Message:");
                Console.WriteLine("{0}", ex.Message);
                Console.ReadKey();
            }
        }

        Console.WriteLine("Finished! Press any key to return to the menu...");
        Console.ReadLine();
        _driver.Dispose();
        Main();
    }

    private static bool IsElementPresent(By by)
    {
        try
        {
            _driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
}