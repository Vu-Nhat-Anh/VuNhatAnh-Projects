using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    // --- HÀM 1: HÀM MAIN (LUÔN CHẠY ĐẦU TIÊN) ---
    static IWebDriver driver = null;
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Khởi tạo trình duyệt thực tế
        // IWebDriver driver = new ChromeDriver();
        InitDriver();
        driver.Manage().Window.Maximize();

        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            //IWebElement mediaMetainfoButtonElement = wait.Until(d => d.FindElement(By.XPath("//span[.='Media Metainfo']")));
            //mediaMetainfoButtonElement.Click();

            //IWebElement filmSeriesElement = wait.Until(d => d.FindElement(By.XPath("//span[.='Series Movies']")));
            //filmSeriesElement.Click();

            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            Console.WriteLine("--> Chuyển sang giao diện Media Upload > Upload Film Series");
            IWebElement mediaUpload = wait.Until(d => d.FindElement(By.XPath("//span[.='Media Upload']")));
            mediaUpload.Click();

            Thread.Sleep(500);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
            IWebElement uploadFilmSeries = wait.Until(d => d.FindElement(By.XPath("//span[.='Upload Film Series']")));
            uploadFilmSeries.Click();

            string[] movieToUploads = System.IO.File.ReadAllLines("\\\\msi\\voice_storage\\movie_images\\metfone_series_upload.txt").Reverse().ToArray();
            string[] allMovieFolders = System.IO.File.ReadAllLines("C:\\scripts\\list_movies.txt").Reverse().ToArray();
            Dictionary<string, string> dictAllMovies = new Dictionary<string, string>();
            foreach (var movieFolder in allMovieFolders)
            {
                var movieName = Path.GetFileName(movieFolder);
                if (!dictAllMovies.ContainsKey(movieName)) dictAllMovies.Add(movieName, movieFolder);
            }

            // duyet tung phim trong danh sach lay tu file text
            for (int i = 0; i < movieToUploads.Length; i++)
            {
                // lay ve cac gia tri ten phim, ten phim moi tu moi dong trong file (Số TT	Tên phim gốc	Tên tiếng anh trên CMS	Tên tiếng Lào trên CMS	Mô tả anh	Mô tả Lào	Tên ảnh (ngang/dọc))
                var movieToUpload = movieToUploads[i];
                if (string.IsNullOrWhiteSpace(movieToUpload)) continue;
                var items = movieToUpload.Split('\t');

                string STT = items[0];
                string tengoc = items[1];
                string tendoilai = items[2];
                string tenKhmer = items[3];
                string motaKhmer = items[4];
                string sotap = items[5];
                string imageFile = items[7];
                if (!dictAllMovies.ContainsKey(tengoc)) continue;    // the movieName was not found

                try
                {
                    string movieFolder = dictAllMovies[tengoc];  // lay ve thu muc chua phim
                    var fileToUploads = Directory.GetFiles(movieFolder, "*.mp4", SearchOption.AllDirectories);  // lay dach sach file phim (mp4) trong thu muc chua phim

                    // duyet tung tap phim
                    for (int j = 0; j < fileToUploads.Length; j++)
                    {
                        try
                        {
                            string fileToUpload = fileToUploads[j];
                            string epName = Path.GetFileName(fileToUpload);
                            string tenPhimMoi = string.IsNullOrEmpty(tendoilai) ? tengoc : tendoilai;
                            string seasonEpisodeIndex = Regex.Match(epName, @"S\d+E\d+", RegexOptions.IgnoreCase).Value;
                            string episodeNewName = $"{tenPhimMoi}_{seasonEpisodeIndex}";

                            IWebElement searchFilm = driver.FindElement(By.XPath("//*[@id=\"csm_media_grid-filters\"]/td[4]/input"));
                            searchFilm.Click(); searchFilm.Clear(); System.Threading.Thread.Sleep(500);
                            searchFilm = driver.FindElement(By.XPath("//*[@id=\"csm_media_grid-filters\"]/td[4]/input"));
                            searchFilm.SendKeys(episodeNewName + Keys.Enter); System.Threading.Thread.Sleep(2000);
                            var results = driver.FindElements(By.XPath("//tbody/tr[@role='row']"));

                            if (!results.Any())
                            {
                                UploadAnEpisode(driver, wait, tengoc, tendoilai, tenKhmer, motaKhmer, fileToUpload, imageFile, episodeNewName);
                            }
                            else
                            {
                                Console.WriteLine($"Đã upload : {epName}");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Đã xảy ra lỗi: {ex.Message} {ex.StackTrace}");
                        }

                    }
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
        var files = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            string cleanFileName = KeepSpaceAndAlphanumeric(fileName).Trim();

            // 3. So sánh: Nếu tên file chứa tên phim
            if (cleanFileName.Contains(cleanMovieName))
            {
                // Kiểm tra chứa từ khóa ảnh ngang
                if (cleanFileName.Contains("hor") || cleanFileName.Contains("landscape") || cleanFileName.Contains("ngang")) //Chua chu hor HOAC landscape
                {
                    res[0] = filePath;
                }
                // Kiểm tra chứa từ khóa ảnh dọc
                else if (cleanFileName.Contains("ver") || cleanFileName.Contains("portrait") || cleanFileName.Contains("dọc"))
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
            string imageClass = (imgType == "portrait") ? "poster_" : "";

            // FIX LỖI XPATH: Đã xóa dấu ngoặc kép thừa ở cuối
            string xpathQuery = $"//*[@id='image_{imageClass}upload']//input[@type='file']";

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

    static void UploadVideoFile(IWebDriver driver, string filePath)
    {
        try
        {
            // 2. Tìm thẻ input file ẩn trên giao diện
            // Thông thường các thư viện Web sẽ giấu thẻ này đi, nhưng nó luôn tồn tại để nhận file.
            IWebElement fileInput = driver.FindElement(By.XPath("//div[@id='video_upload']//input[@type='file']"));

            // 3. Truyền đường dẫn file vào thẻ input để hệ thống tự động upload
            fileInput.SendKeys(filePath);

            // 4. Chờ một khoảng thời gian dài hơn để video upload xong (tùy thuộc vào dung lượng video và mạng)
            // Bạn nên nâng thời gian chờ hoặc dùng WebDriverWait để theo dõi cho tới khi nút "Next" sáng lên.
            Thread.Sleep(10000);

            var uploadedFileLocator = By.XPath($"//video[@id='videoPlayer_html5_api']");
            for (int i = 0; i < 5000; i++)
            {
                try
                {
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                    var playerElements = driver.FindElements(uploadedFileLocator);
                    if (playerElements.Count() > 0)
                    {
                        if (playerElements.First().Displayed)
                        {
                            break;
                        }
                    }
                }
                catch { }
                Thread.Sleep(10000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi upload file: " + ex.Message);
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

    static void TaoVoPhim(IWebDriver driver, WebDriverWait wait, string tenAnh, string sotap, string motaKhmer, string imageName = "")
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

    static string[] rootMovieFolders = @"D:\Downloads\Tải phim tổng hợp".Split(','); // Thư mục chứa phim
    static ArrayList allMovieDirectories = new ArrayList();

    static bool GetVideoUrl(IWebDriver driver, By videoLocator = null, int timeoutSeconds = 10)
    {
        // Kiểm tra và gán locator mặc định khi không truyền vào
        videoLocator ??= By.CssSelector("input[type='file']");

        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

            // 1. Định vị thẻ input dùng để upload file (Thay ID hoặc Selector tương ứng với CMS của bạn)
            By uploadInputLocator = By.CssSelector("");

            IWebElement uploadElement = driver.FindElement(uploadInputLocator);

            // 2. Đường dẫn tuyệt đối tới file video nằm trên máy tính của bạn (My PC)
            string filePath = @"D:\Downloads\Tải phim tổng hợp";

            // 3. Tải file lên bằng cách truyền đường dẫn trực tiếp vào thẻ input
            uploadElement.SendKeys(filePath);

            return true;

        }
        catch (NoSuchElementException)
        {
            return false;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void UploadAnEpisode(IWebDriver driver, WebDriverWait wait, string tengoc, string tenAnh, string tenKhmer, string motaKhmer, string mp4Path, string videoName, string episodeNewName)
    {
        try
        {
            IWebElement createFilmSeries = driver.FindElement(By.XPath("//a[contains(text(), 'Create Film Series')]")); //Nút Create Film Series
            createFilmSeries.Click();
            
            System.Threading.Thread.Sleep(20000); // Đợi trong thời gian 20 giây
            IWebElement uploadVideoFile = wait.Until(d => d.FindElement(By.XPath("//*[@id='video_upload']/div/div/div[2]/input"))); // Nút bấm tải video file
            //uploadVideoFile.FindElement(By.XPath("..")).Click();

            string targetImageName = string.IsNullOrWhiteSpace(videoName) ? tenAnh : videoName;
            string folderPath = @"D:\Downloads\Tải phim tổng hợp";
            string[] imgPath = GetImagePathByMovieName(folderPath, targetImageName);
            string imgPathNgang = imgPath[0];
            string imgPathDoc = imgPath[1];

            try
            {
                UploadVideoFile(driver, mp4Path);
                UploadImgFile(driver, imgPathNgang,"");

                IWebElement episodeNameInput = driver.FindElement(By.XPath("//*[@id='csmmedia-name']")); // Nut bam vao dien ten tieng Kho-me
                episodeNameInput.Clear();
                episodeNameInput.SendKeys(episodeNewName);

                int episodeIndex = int.Parse(System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(mp4Path), @"S\d+E(\d+)").Groups[1].Value); // Xác định số thứ tự tập phim dựa theo Biểu thức Chính quy
                IWebElement shortDescInput = driver.FindElement(By.XPath("//*[@id='csmmediafilmseries-short_desc']")); // Nut bam vao dien ten tieng Kho-me
                shortDescInput.SendKeys(tenKhmer);

                IWebElement descInput = driver.FindElement(By.XPath("//*[@id='csmmediafilmseries-description']")); // tom tat tieng Khmer
                descInput.SendKeys(motaKhmer);

                IWebElement detailInfoButton = driver.FindElement(By.XPath("//*[@id='video-tabs']/ul/li[2]/a"));
                detailInfoButton.Click(); // Nut bam thong tin phim

                IWebElement distServiceButton = driver.FindElement(By.XPath("//*[@id='tab_2']/div/div[1]/div/div/div[1]/div/div[1]/span[2]/span[1]/span/ul/li/input")); // Don vi phan phoi
                distServiceButton.Click();

                IWebElement distServiceSelection = driver.FindElement(By.XPath("//li[.='TV360']")); // Tick vao nut TV360
                distServiceSelection.Click();
               
                IWebElement filmInfoButton = driver.FindElement(By.XPath("//*[@id='video-tabs']/ul/li[3]/a")); // Nhap thong tin phim
                filmInfoButton.Click();

                IWebElement seriesMovieButton = driver.FindElement(By.XPath("//span[.='Select Series Film']")); // Nhap thong tin phim
                seriesMovieButton.Click();

                string newMovieName = videoName.Replace("Max's Puppy Dog", "Max&#039;s Puppy Dog");
                IWebElement filmSelect = driver.FindElement(By.XPath($"//li[.='{newMovieName}']")); // Nhap thong tin phim
                filmSelect.Click();

                IWebElement numEpisode = driver.FindElement(By.Id("csmmediafilmseries-episode_no")); // Nhap so thu tu tap
                numEpisode.SendKeys($"{episodeIndex}");

                IWebElement nameEpisode = driver.FindElement(By.Id("csmmediafilmseries-episode_name")); // Nhap ten tap
                nameEpisode.SendKeys($"Episode {episodeIndex}");
                nameEpisode.SendKeys(Keys.Tab);

                IWebElement saveButton = driver.FindElement(By.XPath("//button[.='Save']")); // Luu phim
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", saveButton);
                saveButton.Click();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
        }
    }

    static void InitDriver()
    {
        ChromeOptions options = new ChromeOptions();

        // 1. Chỉ định thư mục lưu trữ Profile người dùng (đường dẫn tùy chọn trên máy bạn)
        // Lưu ý: Dùng ký tự '@' trước chuỗi đường dẫn
        string profilePath = @"C:\SeleniumProfiles\MyUserSession";
        options.AddArgument($"user-data-dir={profilePath}");

        // 2. (Thùy chọn) Chọn tên profile cụ thể, mặc định là "Default"
        options.AddArgument("profile-directory=Default");

        driver = new ChromeDriver(options);
        driver.Manage().Window.Maximize();

        // Mở trang web cần làm việc
        driver.Navigate().GoToUrl("https://cms.tv360.metfone.com.kh/login");
        Login(driver); return;

        // Kiểm tra xem đã đăng nhập chưa (ví dụ: tìm 1 element chỉ xuất hiện khi đã log in)
        bool isLoggedIn = CheckIfLoggedIn(driver);

        if (!isLoggedIn)
        {
            Console.WriteLine("Chưa đăng nhập. Hãy thực hiện đăng nhập thủ công...");

            // Cho người dùng 60s để tự gõ ID/Password/OTP/Captcha đăng nhập
            // Sau khi đăng nhập xong, Profile sẽ tự động lưu lại tất cả vào ổ cứng
            System.Threading.Thread.Sleep(60000);
        }
        else
        {
            Console.WriteLine("Đã nhận diện phiên đăng nhập cũ! Tiếp tục cào dữ liệu/thao tác...");
        }
    }

    static bool CheckIfLoggedIn(IWebDriver driver)
    {
        try
        {
            // Thay đổi By.Id hoặc By.CssSelector phù hợp với trang web của bạn
            // Ví dụ: Tìm nút "Avatar" hoặc "Đăng xuất"
            return driver.FindElements(By.CssSelector(".user-avatar")).Count > 0;


        }
        catch
        {
            return false;
        }
    }
}