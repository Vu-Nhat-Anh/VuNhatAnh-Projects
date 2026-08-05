using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;

class Program
{
    // --- HÀM 1: HÀM MAIN (LUÔN CHẠY ĐẦU TIÊN) ---
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Khởi tạo trình duyệt thực tế
        IWebDriver driver = new ChromeDriver();
        driver.Manage().Window.Maximize();

        try
        {
            // Gọi hàm đăng nhập và truyền trình duyệt vào
            Login(driver);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

            IWebElement mediaMetainfoButtonElement = wait.Until(d => d.FindElement(By.XPath("//span[.='Media Metainfo']")));
            mediaMetainfoButtonElement.Click();

            IWebElement filmSeriesElement = wait.Until(d => d.FindElement(By.XPath("//span[.='Series Movies']")));
            filmSeriesElement.Click();

            string[] fileToUploads = System.IO.File.ReadAllLines("C:\\lam_phim\\danh_sach_phim.txt").Reverse().ToArray();
            for (int i = 0; i < fileToUploads.Length; i++)
            {
                try
                {
                    // lay ve cac gia tri ten phi, ten phim moi tu moi dong trong file (Số TT	Tên phim gốc	Tên tiếng anh trên CMS	Tên tiếng Lào trên CMS	Mô tả anh	Mô tả Lào	Tên ảnh (ngang/dọc))
                    var fileToUpload = fileToUploads[i];
                    if (string.IsNullOrWhiteSpace(fileToUpload)) continue;
                    var items = fileToUpload.Split('\t');
                    string STT = items[0];
                    string tenViet = items[1];
                    string tenAnh = items[2];
                    string sotap = items[3];
                    string tenKhmer = items[4];
                    string motaKhmer = items[5];

                    // Lưu ý file ảnh là C:\Users\hivu\Downloads\Ảnh poster Bplus.zip\Ảnh poster Bplus

                    //UploadMovie(driver, mp4File, srtFile, enName, loName, enDesc, loDesc, imgPathDoc, imgPathNgang);
                    IWebElement searchFilm = driver.FindElement(By.XPath("//*[@id=\"w0-filters\"]/td[3]/input")); // Ô input tìm theo tên phim
                    searchFilm.Clear();
                    System.Threading.Thread.Sleep(5000);
                    searchFilm = driver.FindElement(By.XPath("//*[@id=\"w0-filters\"]/td[3]/input"));
                    searchFilm.SendKeys(tenAnh);
                    searchFilm.SendKeys(Keys.Enter);
                    System.Threading.Thread.Sleep(2000);
                    var results = driver.FindElements(By.CssSelector("#w0 > div.table-responsive > table > tbody > tr:nth-child(1)"));

                    if (results.Count == 0)
                    {
                        try
                        {
                            taoVoPhim(driver, wait, tenAnh, sotap, motaKhmer);
                            IWebElement createMovies = wait.Until(d => d.FindElement(By.XPath("//button[text()='Create']"))); // Button tạo vỏ phim
                            createMovies.Click();
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine($"Đã xảy ra lỗi : {ex}");
                        }
                    }
                    else
                    {
                        continue;
                    }

                    wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                    Console.WriteLine("--> Chuyển sang giao diện Media Upload > Upload Film Series");
                    IWebElement mediaUpload = wait.Until(d => d.FindElement(By.XPath("//span[.='Media Upload']")));
                    mediaUpload.Click();

                    IWebElement uploadFilmSeries = wait.Until(d => d.FindElement(By.XPath("//span[.='Upload Film Series']")));
                    uploadFilmSeries.Click();

                    TaiVideoPhim(driver, wait);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message} {ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loi he thong: {ex.Message}");
        }
    }

    static string[] GetImagePathByMovieName(string folderPath, string movieName)
    {
        string[] res = new string[2] { "", "" };
        if (!Directory.Exists(folderPath)) return res;

        string cleanMovieName = KeepSpaceAndAlphanumeric(movieName).Trim();

        // Lấy tất cả file bắt đầu bằng movieName (không quan tâm đuôi file)
        var files = Directory.GetFiles(folderPath, "*.png");

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            string cleanFileName = KeepSpaceAndAlphanumeric(fileName).Trim();

            // 3. So sánh: Nếu tên file chứa tên phim
            if (cleanFileName.Contains(cleanMovieName))
            {
                // Kiểm tra chứa từ khóa ảnh ngang
                if (cleanFileName.Contains("hor") || cleanFileName.Contains("landscape")) //Chua chu hor HOAC landscape
                {
                    res[0] = filePath;
                }
                // Kiểm tra chứa từ khóa ảnh dọc
                else if (cleanFileName.Contains("ver") || cleanFileName.Contains("portrait"))
                {
                    res[1] = filePath;
                }
            }
        }

        return res;
    }



    public static string KeepSpaceAndAlphanumeric(string input) // Chỉ giữ lại các ký tự chữ và số (từ A - Z từ 0 - 9 và bỏ qua các ký tự đặc biệt)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // char.IsLetterOrDigit checks for A-Z, a-z, and 0-9
        // c == ' ' ensures we only keep regular spaces (not tabs or newlines)
        return new string(input.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).ToLower();
    }

    static void UploadImgFile(IWebDriver driver, string imgFullFilePath, string imgType)
    {
        try
        {
            // Xác định ID wrapper dựa trên type
            string imageClass = (imgType == "portrait") ? "image_upload" : "second_image_upload";

            // FIX LỖI XPATH: Đã xóa dấu ngoặc kép thừa ở cuối
            string xpathQuery = $"//*[@id='{imageClass}']//input[@type='file']";

            var fileInputs = driver.FindElements(By.XPath(xpathQuery));

            if (fileInputs.Count > 0)
            {
                IWebElement fileInput = fileInputs[0];

                // SendKeys đường dẫn tuyệt đối của file ảnh vào input[type='file']
                fileInput.SendKeys(imgFullFilePath);
                Thread.Sleep(5000); // Chờ UI upload/render thumbnail
                Console.WriteLine($"[Thành công] Đã gửi file {imgType}: {imgFullFilePath}");
            }
            else
            {
                Console.WriteLine($"[Lỗi] Không tìm thấy input file cho {imageClass}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi upload file: " + ex.Message + ex.StackTrace);
        }
    }

    // --- HÀM 2: HÀM LOGIN (NẰM TÁCH BIỆT HOÀN TOÀN VỚI MAIN) ---
    static void Login(IWebDriver driver)
    {
        try
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            var fluentWait = new DefaultWait<IWebDriver>(driver)
            {
                Timeout = TimeSpan.FromSeconds(60),
                PollingInterval = TimeSpan.FromMilliseconds(250)
            };
            fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

            driver.Navigate().GoToUrl("https://cms.tv360.metfone.com.kh/login");

            // Tìm và điền Username vào Tab mới
            IWebElement usernameField = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"loginform-username\"]")));
            usernameField.Clear();
            usernameField.SendKeys("Metavision_Upload");

            // Tìm và điền Password
            IWebElement passwordField = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"loginform-password\"]")));
            passwordField.Clear();
            passwordField.SendKeys("Metavision@123");


            Console.WriteLine("Nhap captcha vao day");
            string captcha = Console.ReadLine();
            IWebElement captchaField = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"loginform-captcha\"]")));
            captchaField.SendKeys(captcha);

            IWebElement next = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"login-form\"]/div[4]/button")));
            next.Click();

            Console.WriteLine("Nhap ma xac thuc vao day");
            string auth = Console.ReadLine();
            IWebElement authField = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"googleauthenticatorform-code\"]")));
            authField.SendKeys(auth);

            IWebElement verify = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"google-authenticator-form\"]/div[3]/button")));
            verify.Click();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi xảy ra trong hàm Login: {ex.Message}");
        }
    }

    static void taoVoPhim(IWebDriver driver, WebDriverWait wait, string tenAnh, string sotap, string motaKhmer, string imageName = "")
    {
        try
        {

            IWebElement CreateFilm = wait.Until(d => d.FindElement(By.XPath("/html/body/div/div[3]/div[2]/div/div[2]/div/div/div[1]/div[2]/a")));
            CreateFilm.Click();

            IWebElement FilmName = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"csmattribute-name\"]")));
            FilmName.SendKeys(tenAnh);

            IWebElement isActive = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"w0\"]/div/div[2]/div/div/div[1]/div[2]/div/div/label/span[3]")));
            isActive.Click();

            IWebElement soluongtap = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"csmattribute-duration\"]")));
            soluongtap.SendKeys(sotap);

            IWebElement territory = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"w0\"]/div/div[2]/div/div/div[1]/div[7]/div/div/label/span[3]")));
            territory.Click();

            IWebElement motaphim = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"csmattribute-description\"]")));
            motaphim.SendKeys(motaKhmer);

            IWebElement uploadFile = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"image_upload\"]/div/div/div[2]/input")));

            string targetImageName = string.IsNullOrWhiteSpace(imageName) ? tenAnh : imageName;
            string folderPath = @"C:\Users\hivu\Downloads\Ảnh poster Bplus\Ảnh poster Bplus";

            // Gọi hàm lấy ảnh
            string[] imgPath = GetImagePathByMovieName(folderPath, targetImageName);
            string imgPathNgang = imgPath[0];
            string imgPathDoc = imgPath[1];

            // Log đường dẫn để debug trực tiếp
            Console.WriteLine($"[DEBUG] Đường dẫn ảnh ngang (16:9): '{imgPathNgang}'");
            Console.WriteLine($"[DEBUG] Đường dẫn ảnh dọc (2:3): '{imgPathDoc}'");

            if (string.IsNullOrEmpty(imgPathDoc) || !File.Exists(imgPathDoc))
            {
                Console.WriteLine($"[CẢNH BÁO] KHÔNG tìm thấy ảnh DỌC (*ver.png) cho phim: {targetImageName}");
            }
            else
            {
                UploadImgFile(driver, imgPathDoc, "portrait");
            }

            if (string.IsNullOrEmpty(imgPathNgang) || !File.Exists(imgPathNgang))
            {
                Console.WriteLine($"[CẢNH BÁO] KHÔNG tìm thấy ảnh NGANG (*hor.png) cho phim: {targetImageName}");
            }
            else
            {
                UploadImgFile(driver, imgPathNgang, "landscape");
            }
            // Đường dẫn ảnh 2:3 - //*[@id="image_upload"]/div/div/div[2]/div
            // Đường dẫn ảnh 16:9 - //*[@id="second_image_upload"]/div/div/div[2]/div

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    static void TaiVideoPhim(IWebDriver driver, WebDriverWait wait)
    {
        try
        {
            IWebElement createFilmSeries = wait.Until(d => d.FindElement(By.XPath("//*[@id=\"mainGridPjax\"]/div[1]/div[1]/div[2]/div/a")));

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
        }

    }
}