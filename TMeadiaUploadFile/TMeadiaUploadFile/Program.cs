using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TMeadiaUploadFile
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver workerDriver = new ChromeDriver();
            workerDriver.Manage().Window.Maximize();

            // multiple steps to go to upload movie page
            Login(workerDriver);
            var allCookies = workerDriver.Manage().Cookies.AllCookies;
            var currentUrl = workerDriver.Url;

            string[] fileToUploads = File.ReadAllLines("C:\\temp\\movie_to_upload.txt").Skip(24).ToArray();
            // Luu y : file anh la \\msi\voice_storage\movie_images

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 1 // Giới hạn tối đa 3 luồng chạy cùng lúc
            };

            Parallel.ForEach(fileToUploads, options, fileToUpload =>
            {
                // BỌC TRONG USING: Đảm bảo xong việc hoặc bị lỗi thì Chrome vẫn tự động đóng lại
                //using (IWebDriver workerDriver = new ChromeDriver())
                {
                    try
                    {
                        workerDriver.Manage().Window.Maximize();

                        // 1. QUAN TRỌNG: Phải truy cập trang web trước!
                        //workerDriver.Navigate().GoToUrl(currentUrl);

                        //// 2. Mới nạp Cookie
                        //foreach (var cookie in allCookies)
                        //{
                        //    workerDriver.Manage().Cookies.AddCookie(cookie);
                        //}

                        //// 3. Nạp xong thì Load lại trang để nhận diện trạng thái đã đăng nhập
                        //workerDriver.Navigate().Refresh();

                        // --- XỬ LÝ DỮ LIỆU CỦA BẠN ---
                        var items = fileToUpload.Split('\t');
                        string orgName = items[0];
                        string enName = items[1];
                        string loName = items[2];
                        string enDesc = items[3];
                        string loDesc = items[4];
                        string imgName = items[5];

                        var mp4File = LocateMp4ByMovieName(orgName);
                        string[] imgPath = GetImagePathByMovieName(@"\\msi\voice_storage\movie_images", imgName);
                        string imgPathDoc = imgPath[0];
                        string imgPathNgang = imgPath[1];
                        string[] srtFiles = LocateSrtByMp4File(mp4File);

                        // Hàm upload
                        UploadMovie(workerDriver, mp4File, srtFiles[0], srtFiles[1], enName, loName, enDesc, loDesc, imgPathDoc, imgPathNgang);

                        Console.WriteLine($"[Thành công] Đã xử lý xong file: {orgName}");
                    }
                    catch (Exception ex)
                    {
                        // Ghi log rõ ràng lỗi thuộc về item nào
                        Console.WriteLine($"Đã xảy ra lỗi với {fileToUpload}: {ex.Message}");
                    }
                } // Khi chạy đến đây (dù thành công hay lỗi), workerDriver sẽ tự động gọi .Quit() để đóng Chrome.
            });

            Console.WriteLine("Đã xử lý xong toàn bộ mảng!");
        }

        static string[] GetImagePathByMovieName(string folderPath, string movieName) // khai bao bien trong ham (duong dan anh va ten phim). static = ham tinh, string = kieu du lieu phai tra ve.
        {
            string[] res = new string[2];   // 1 anh doc, 1 anh ngang
            if (!Directory.Exists(folderPath)) return res; // Kiem tra xem co ton tai ham GetImagePath hay chua. Neu KHONG TON TAI tra ve null

            string[] anhDoc = Directory.GetFiles(folderPath, movieName + "*dọc.png"); // Quet qua tat ca cac file (tim kiem) phim chua png o thu muc anh
            res[0] = anhDoc.Length > 0 ? anhDoc[0] : ""; //anh.Length > 0 nghia la co ton tai file nao hay khong. Neu co (?) thi tim thay anh, neu khong (: null) = khong tim thay anh ung voi phim do. 
            string[] anhNgang = Directory.GetFiles(folderPath, movieName + "*ngang.png"); // Quet qua tat ca cac file (tim kiem) phim chua png o thu muc anh
            res[1] = anhNgang.Length > 0 ? anhNgang[0] : ""; //files.Length > 0 nghia la co ton tai file nao hay khong. Neu co (?) thi tim thay anh, neu khong (: null) = khong tim thay anh ung voi phim do. 

            return res;
        }

        static string[] rootMovieFolders = @"\\hp245g8\NetFlixaAll64Tb,\\msi\NetFlixMsi1,\\msi\NetFlixMsi2,\\msi\NetFlixMsi3,\\msi\NetFlixMsi4,\\msi\NetFlixMsi5".Split(',');
        static ArrayList allMovieDirectories = new ArrayList();
        static string LocateMp4ByMovieName(string movieName)
        {
            string res = "";
            if (allMovieDirectories.Count == 0)
            {
                foreach (string rootMovieFolder in rootMovieFolders)
                {
                    var movies = Directory.GetDirectories(rootMovieFolder);
                    allMovieDirectories.AddRange(movies);
                }
            }

            foreach (string existedMovie in allMovieDirectories)
            {
                if (KeepSpaceAndAlphanumeric(Path.GetFileName(existedMovie)) ==
                    KeepSpaceAndAlphanumeric(movieName))
                {
                    // found the expected movie
                    var mp4Files = Directory.GetFiles(existedMovie, "*.mp4");
                    if (mp4Files.Count() > 0)
                    {
                        res = mp4Files[0];
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return res;
        }

        static string[] LocateSrtByMp4File(string mp4File)
        {
            string[] res = new string[2];
            string movieFolder = Path.GetDirectoryName(mp4File);
            var enSrtFiles = Directory.GetFiles(movieFolder, "*.English.srt");
            if (enSrtFiles.Count() > 0)
            {
                res[0] = enSrtFiles[0];
            }

            var loSrtFiles = Directory.GetFiles(movieFolder, "*.lo.srt");
            if (loSrtFiles.Count() > 0)
            {
                res[1] = loSrtFiles[0];
            }

            return res;
        }

        public static string KeepSpaceAndAlphanumeric(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // char.IsLetterOrDigit checks for A-Z, a-z, and 0-9
            // c == ' ' ensures we only keep regular spaces (not tabs or newlines)
            return new string(input.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).ToLower();
        }

        static WebDriverWait wait;
        static DefaultWait<IWebDriver> fluentWait;
        static void Login(IWebDriver driver)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            fluentWait = new DefaultWait<IWebDriver>(driver)
            {
                Timeout = TimeSpan.FromSeconds(60),
                PollingInterval = TimeSpan.FromMilliseconds(250) // Check every 250ms
            };

            // Ignore specific exceptions during the look-up period
            fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);

            //string url = "https://sso.laoid.net/login";
            driver.Navigate().GoToUrl("https://crm.laoid.net/v2/"); Wait(10);

            // Khởi tạo bộ đợi (Wait) tối đa 10 giây
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

            // Enable fluter to show html tree bang cach bam vao button enable an cua flutter
            var hiddenEnableHtmlTreeButton = driver.FindElement(By.XPath("//flt-semantics-placeholder[@aria-label='Enable accessibility']"));
            js.ExecuteScript("arguments[0].click();", hiddenEnableHtmlTreeButton); Wait(1);
            driver.FindElement(By.XPath("//flt-semantics[.='Login']")).Click(); Wait(5);
            string originalTab = driver.CurrentWindowHandle;
            var newTab = driver.WindowHandles.Last();
            driver.SwitchTo().Window(newTab);

            // 3. Tìm và nhập Username
            // Thay "username-id" bằng Id, Name hoặc XPath thực tế trên web abc.com
            IWebElement usernameField = wait.Until(d => d.FindElement(By.Name("username")));
            usernameField.Clear();
            usernameField.SendKeys("Tina@galaxy365.asia"); Wait(1);

            // 5. Tìm và Click nút Next
            IWebElement nextButton = driver.FindElement(By.XPath("//span[.='Next']"));
            nextButton.Click(); Wait(1);

            // 4. Tìm và nhập Password
            // Thay "password-id" bằng Id, Name hoặc XPath thực tế
            IWebElement passwordField = driver.FindElement(By.Name("password"));
            passwordField.Clear();
            passwordField.SendKeys("Cungau@2008"); Wait(1);

            nextButton = driver.FindElement(By.XPath("//span[.='Next']"));
            nextButton.Click(); Wait(1);

            driver.SwitchTo().Window(originalTab); Wait(2);
            IWebElement managerButton = driver.FindElement(By.XPath("//flt-semantics[.='Manager']"));
            managerButton.Click(); Wait(5);

            driver.Navigate().GoToUrl("https://crm.laoid.net/v2/main/company/services/list");

            //js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//flt-semantics-placeholder[@aria-label='Enable accessibility']"))); Wait(1);    // <== bam vao button an de enable fluter sinh the html cho cac element
            EnableFluterHtmlElement(driver, js);
            driver.FindElement(By.Id("flt-semantic-node-11")).Click(); Wait(3);
            driver.FindElement(By.XPath("//flt-semantics[@aria-label='Lao Social (business)']")).Click();
            driver.FindElement(By.XPath("//flt-semantics[.='Login to Lao Social']")).Click();
            Wait(5);

            newTab = driver.WindowHandles.Last();
            driver.SwitchTo().Window(newTab);
            driver.FindElement(By.XPath("//span[.='Film+']")).Click(); Wait(5);
        }

        static void Wait(int secondToWait)
        {
            System.Threading.Thread.Sleep(secondToWait * 1000);
        }

        static void EnableFluterHtmlElement(IWebDriver driver, IJavaScriptExecutor js)
        {
            try
            {
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//flt-semantics-placeholder[@aria-label='Enable accessibility']"))); Wait(1);    // <== bam vao button an de enable fluter sinh the html cho cac element
            }
            catch { }
        }

        static bool CheckFilmUploaded(IWebDriver driver, string movieName)
        {
            bool res = false;

            driver.FindElement(By.XPath("//input[@placeholder='Search movie by name']")).Clear(); Wait(1);
            driver.FindElement(By.XPath("//input[@placeholder='Search movie by name']")).SendKeys(movieName + Keys.Enter); Wait(1);
            var foundItemLocator = By.XPath($"//table/tbody/tr/td[2]//a[contains(.,'{movieName}')]");

            try
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                res = driver.FindElements(foundItemLocator).Count() > 0;
            }
            catch { }

            return res;
        }

        static void UploadMovie(IWebDriver driver, string movieFilePath, string enSrtFilePath, string loSrtFilePath
            , string enName, string loName, string enDesc, string loDesc, string imgPathDoc, string imgPathNgang)
        {
            try
            {
                // bam vao tab Movie
                driver.FindElement(By.XPath("//a[.=' Movie ']")).Click(); Wait(1);

                if (CheckFilmUploaded(driver, enName))
                {
                    return;
                }

                // bam vao nut Create
                driver.FindElement(By.XPath("//*[@id='custom-css']/my-app/div/header/my-header/div/div/div[2]/my-button/button/span")).Click(); Wait(1);
                // bam vao chon Movie
                driver.FindElement(By.XPath("//*[@id='custom-css']/my-app/div/header/my-header/div/div/div[2]/div/a[2]/span")).Click(); Wait(1);
                // nhap tieu de phim
                driver.FindElement(By.XPath("//*[@id='crawl-file-title']")).SendKeys(enName); Wait(1);
                // bam nut continue
                driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/footer/button[2]")).Click(); Wait(2);

                // nhap cac thong tin chi tiet (tab main information)
                // Tóm tắt phim
                IWebElement filmOverview = driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/my-movie-form/div/div[1]/div/div[2]/section/div[1]/div[3]/textarea"));
                filmOverview.SendKeys(enDesc);

                // The loai phim
                //IWebElement filmGenre = driver.FindElement(By.XPath("//*[@id='film-genres']"));
                //filmGenre.SendKeys("Family");

                // Quoc gia xuat xu
                //IWebElement cog = driver.FindElement(By.XPath("//*[@id='film-country']"));
                //cog.Clear();
                //cog.SendKeys("Thailand");

                // Chuyen sang muc tieng Lao : //*[@id="content"]/div/ng-component/div/my-movie-form/div/div[1]/div/div[1]/div[2]/button[2]
                driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/my-movie-form/div/div[1]/div/div[1]/div[2]/button[2]")).Click();
                Wait(1);

                // Nhap tua tieng Lao cho phim Gohan : ໂກຮັງ
                driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/my-movie-form/div/div[1]/div/div[2]/section/section/div/div/div/input")).SendKeys(loName); Wait(1);

                // Nhap phan tom tat phim bang tieng Lao
                IWebElement filmOverviewinLao = driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/my-movie-form/div/div[1]/div/div[2]/section/div[1]/div[3]/textarea"));
                filmOverviewinLao.Clear();
                filmOverviewinLao.SendKeys(loDesc);
                Wait(1);

                // Nhap anh dai dien tren tab Image Management
                driver.FindElement(By.XPath("//button[.='Image Management']")).Click();
                Wait(1);

                // upload image cho cac ngon ngu
                foreach (var lang in "EN-US,LAOS".Split(','))
                {
                    driver.FindElement(By.XPath($"//button[@class='language-toggle__btn' and .=' {lang} ']")).Click();
                    if (File.Exists(imgPathDoc))
                    {
                        UploadImgFile(driver, imgPathDoc, "poster");
                        UploadImgFile(driver, imgPathDoc, "logo");
                    }

                    if (File.Exists(imgPathNgang))
                    {
                        UploadImgFile(driver, imgPathNgang, "thumbnail");
                    }
                }

                // Bam nut tiep theo
                driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/footer/button[2]")).Click();
                Wait(10);

                UploadVideoFile(driver, movieFilePath);

                IWebElement dropdownElement = driver.FindElement(By.XPath("//select[contains(@class,'sub-select')]"));
                SelectElement select = new SelectElement(dropdownElement);
                if (enSrtFilePath != "")
                {
                    select.SelectByText("English");
                    //driver.FindElement(By.XPath($"//button[@class='language-toggle__btn' and .=' EN-US ']")).Click();
                    UploadSrtFile(driver, enSrtFilePath);
                }

                if (loSrtFilePath != "")
                {
                    select.SelectByText("Lao");
                    //driver.FindElement(By.XPath($"//button[@class='language-toggle__btn' and .=' LAOS ']")).Click();
                    UploadSrtFile(driver, loSrtFilePath);
                }

                driver.FindElement(By.XPath("//*[@id='content']/div/ng-component/div/footer/button[2]")).Click();
                Wait(1);

                driver.FindElement(By.XPath("//button[.= 'Submit for review']")).Click();
                File.WriteAllText("phim_da_up.txt", DateTime.Now + ":\t" + movieFilePath + "\n");
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }
        }

        static void UploadVideoFile(IWebDriver driver, string filePath)
        {
            Thread.Sleep(100000);

            try
            {
                // 2. Tìm thẻ input file ẩn trên giao diện
                // Thông thường các thư viện Web sẽ giấu thẻ này đi, nhưng nó luôn tồn tại để nhận file.
                IWebElement fileInput = driver.FindElement(By.XPath("//input[@type='file' and @accept='video/*']"));

                // 3. Truyền đường dẫn file vào thẻ input để hệ thống tự động upload
                fileInput.SendKeys(filePath);

                // 4. Chờ một khoảng thời gian dài hơn để video upload xong (tùy thuộc vào dung lượng video và mạng)
                // Bạn nên nâng thời gian chờ hoặc dùng WebDriverWait để theo dõi cho tới khi nút "Next" sáng lên.
                Thread.Sleep(10000);

                var uploadedFileLocator = By.XPath($"//span[@class='file-name-display' and .='{Path.GetFileName(filePath)}']");
                for (int i = 0; i < 5000; i++)
                {
                    try
                    {
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                        if (driver.FindElements(uploadedFileLocator).Count() > 0)
                            break;
                    }
                    catch { }

                    Wait(1);
                }

                // 5. Sau khi upload thành công, bấm nút Next ở góc dưới bên phải để qua bước "3 Subtitle"
                driver.FindElement(By.XPath("//button[.= 'Next']")).Click();
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi upload file: " + ex.Message);
            }
        }

        static void UploadSrtFile(IWebDriver driver, string srtFullFilePath)
        {
            try
            {
                IWebElement fileInput = driver.FindElement(By.XPath("//input[@type='file' and @accept='.srt,.vtt']"));
                fileInput.SendKeys(srtFullFilePath);
                Wait(1);

                var uploadedFileLocator = By.XPath($"//span[@class='file-name-display' and .='{Path.GetFileName(srtFullFilePath)}']");
                for (int i = 0; i < 200; i++)
                {
                    if (driver.FindElement(uploadedFileLocator) != null)
                    {
                        break;
                    }
                    Wait(1);
                }

                // 5. Sau khi upload thành công, bấm nút Next ở góc dưới bên phải để qua bước "3 Subtitle"
                driver.FindElement(By.XPath("//button[.= 'Next']")).Click();
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi upload file: " + ex.Message);
            }
        }

        // imageType: logo, thumbnail, poster
        static void UploadImgFile(IWebDriver driver, string imgFullFilePath, string imgType)
        {
            try
            {
                string imageClass = "image-field--logo";
                if (imgType == "thumbnail") imageClass = "image-field--thumbnail";
                if (imgType == "poster") imageClass = "image-field--poster";
                var imageFileInputs = driver.FindElements(By.XPath($"//div[contains(@class,'{imageClass}')]//input[@type='file' and @accept='image/*']")).Take(1);
                foreach (IWebElement fileInput in imageFileInputs)
                {
                    fileInput.SendKeys(imgFullFilePath);
                    Wait(1);

                    var uploadedFileLocator = By.XPath($"//div[contains(@class,'{imageClass}')]//img");
                    for (int i = 0; i < 200; i++)
                    {
                        try
                        {
                            if (driver.FindElement(uploadedFileLocator) != null)
                            {
                                break;
                            }
                        }
                        catch { }
                        Wait(1);
                    }
                    Wait(2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi upload file: " + ex.Message);
            }
        }
    }
}