using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestTools.UITesting.HtmlControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;


namespace ScenarioTest
{
    /// <summary>
    /// Summary description for CodedUITest1
    /// </summary>
    [CodedUITest]
    public class VerifiyWebApp_RoleClaims
    {
        public VerifiyWebApp_RoleClaims()
        {
        }

        [TestMethod]
        public void CodedUITestMethod1()
        {
            RunEndToEnd("https://todolistservicewithcertificate.azurewebsites.net/",
                "tasktrackerwebapp-approver@msidentitysamplestesting.onmicrosoft.com",
                "xam/vtIMPD+uFpEd5RMKu/KJLwpi0c+tr6WoA9KfHcg=", "Approver");
        }

        private void RunEndToEnd(string url, string userLogin, string userPassword, string role)
        {
            // Launch a brower
            BrowserWindow browser = BrowserWindow.Launch(url, "-private");
            HtmlDocument page = new HtmlDocument(browser);
            page.FilterProperties[HtmlDocument.PropertyNames.PageUrl] = url;


            // Sign in
            page = ClickSignInHyperLink(browser, page);
            browser.WaitForControlReady();
            page = AcceptConsent(browser, page);
            page = EnterCredentialsAndSignIn(browser, page, userLogin, userPassword);

#if VerifyRole
            // Navigate to the About page and verify that the user has the right role
            page = NavigateToAboutPage(browser, page);
            VerifyThatTheUserIsInRole(page, role);
#endif

            // Sign out
            SignOut(page);
        }

        private static void SignOut(HtmlDocument page)
        {
            // Click the "Sign-out" Hyperlink
            HtmlHyperlink signOutHyperLink = new HtmlHyperlink(page);
            signOutHyperLink.SearchProperties[HtmlHyperlink.PropertyNames.Id] = "logoutLink";
            if (signOutHyperLink.Exists)
            {
                signOutHyperLink.EnsureClickable();
                Mouse.Click(signOutHyperLink);
            }
        }

        private static void VerifyThatTheUserIsInRole(HtmlDocument page, string roleName)
        {
            // Verify that the  users has a role of Approver
            HtmlCustom ul = new HtmlCustom(page);
            ul.SearchProperties["TagName"] = "UL";
            HtmlCustom li = new HtmlCustom(ul);
            li.SearchProperties["TagName"] = "LI";
            Assert.IsTrue(li.InnerText.Contains(roleName),
                          string.Format("the About box should show that the user has the '{0}' role", roleName));
        }

        private static HtmlDocument NavigateToAboutPage(BrowserWindow browser, HtmlDocument page)
        {
            // Click the "About" Hyperlink
            HtmlHyperlink aboutHyperlink = new HtmlHyperlink(page);
            aboutHyperlink.SearchProperties[HtmlHyperlink.PropertyNames.Id] = "about";
            aboutHyperlink.WaitForControlExist();
            Mouse.Click(aboutHyperlink);
            page = new HtmlDocument(browser);
            return page;
        }

        private static HtmlDocument AcceptConsent(BrowserWindow browser, HtmlDocument page)
        {
            HtmlControl acceptConsentButton = new HtmlControl(page);
            acceptConsentButton.SearchProperties[HtmlEdit.PropertyNames.Id] = "cred_accept_button";
            acceptConsentButton.TryFind();
            if (acceptConsentButton.Exists)
            {
                Mouse.Click(acceptConsentButton);
            }

            // Get the new page
            page = new HtmlDocument(browser);
            return page;
        }

        private static HtmlDocument EnterCredentialsAndSignIn(BrowserWindow browser, HtmlDocument page, string login, string password)
        {
            // Check if there is a login user chooser (some identity already logged-in)
            HtmlControl loginUserChooser = new HtmlControl(page);
            loginUserChooser.SearchProperties[HtmlEdit.PropertyNames.Id] = "login_user_chooser";
            loginUserChooser.TryFind();
            if (loginUserChooser.Exists)
            {
                HtmlTable chooseAnotherAccount = new HtmlTable(loginUserChooser);
                chooseAnotherAccount.SearchProperties[HtmlTable.PropertyNames.Id] = "use_another_account";
                chooseAnotherAccount.TryFind();
                if (chooseAnotherAccount.Exists)
                {
                    Mouse.Click(chooseAnotherAccount);
                }
            }

            // Enter the login and password
            HtmlEdit loginTextBox = new HtmlEdit(page);
            loginTextBox.SearchProperties[HtmlEdit.PropertyNames.Id] = "cred_userid_inputtext";
            loginTextBox.Text = login;
            loginTextBox.WaitForControlReady();

            // The page can be refreshed.
            page = new HtmlDocument(browser);

            HtmlEdit passwordTextBox = new HtmlEdit(page);
            passwordTextBox.SearchProperties[HtmlEdit.PropertyNames.Id] = "cred_password_inputtext";
            passwordTextBox.EnsureClickable();
            passwordTextBox.Password = password;

            HtmlControl signInButton = new HtmlControl(page);
            signInButton.SearchProperties[HtmlControl.PropertyNames.Id] = "cred_sign_in_button";
            Mouse.Click(signInButton);

            // Get the new page
            page = new HtmlDocument(browser);
            return page;
        }

        private static HtmlDocument ClickSignInHyperLink(BrowserWindow browser, HtmlDocument page)
        {
            // Click the "Sign-In" Hyperlink
            HtmlHyperlink signInHyperLink = new HtmlHyperlink(page);
            signInHyperLink.SearchProperties[HtmlHyperlink.PropertyNames.Id] = "loginLink";

            signInHyperLink.EnsureClickable();
            Mouse.Click(signInHyperLink);

            // Get the new page
            page = new HtmlDocument(browser);
            return page;
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        private TestContext testContextInstance;
    }
}
