using InsuranceOCR;
using SautinSoft.Document;
using System.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DataTable = System.Data.DataTable;
using Color = SautinSoft.Document.Color;
using Path = System.IO.Path;
using OpenCvSharp;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.EMMA;
using System.Globalization;

static class Program
{
    static string pdf_Path = ConfigurationManager.AppSettings["pdf_Path"].ToString();
    static string log_Path = ConfigurationManager.AppSettings["log_Path"].ToString();
    //static string project_Path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
    static string doc_Path = ConfigurationManager.AppSettings["doc_Path"].ToString();
    static string webApiUrl = ConfigurationManager.AppSettings["webapi_url"].ToString();
    static string tessdataPath = ConfigurationManager.AppSettings["tessdataPath"].ToString();
    static int timerDelayInMins = Convert.ToInt32(ConfigurationManager.AppSettings["timerDelayInMins"]);
    static string SFTPServerPath = ConfigurationManager.AppSettings["sFTPServerPath"].ToString();
    static Logger logger = new Logger(log_Path + @"\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

    static void Main(string[] args)
    {
        //MyMethodAsync();
        ////ProcessPendingDocsAsync();

        // Interval between task repetitions in milliseconds
        int interval = timerDelayInMins * 60000;

        // Create a cancellation token source to stop the tasks
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Start the tasks in parallel with timer interval
        //Task.Run(() => DirectoryCopying(interval, cancellationToken));          //Copying the files
        //Task.Run(() => ManagePolicyFoldersAsync(interval, cancellationToken));  //Check and add new Policy Folders
        //Task.Run(() => ManageDocsForPolicies(interval, cancellationToken));     //Check and add new Documents in the policy folders
        //Task.Run(() => ProcessPendingDocsAsync(interval, cancellationToken));   //OCR Processing

        //Start the tasks in parallel one time
        Task.Run(() => DirectoryCopying());          //Copying the files
        Task.Run(() => ManagePolicyFoldersAsync());  //Check and add new Policy Folders
        Task.Run(() => ManageDocsForPolicies());     //Check and add new Documents in the policy folders
        //Task.Run(() => ProcessPendingDocsAsync());   //OCR Processing
        //Task.Run(() => Task3(interval, cancellationToken));

        // Wait for user input to stop the tasks
        Console.WriteLine("Press Enter to stop the tasks.");
        Console.ReadLine();

        // Cancel the tasks
        cancellationTokenSource.Cancel();

        Console.WriteLine("Tasks stopped. Press any key to exit.");
        Console.ReadKey();

    }

    #region Http
    private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // Accept all certificates regardless of validation errors
        return true;
    }
    private static async Task<HttpResponseMessage> SendHttpRequest(string? requestDataJson, string uri, int method)
    {
        try
        {
            using (var httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ValidateCertificate
            }))
            {
                // Set the base URL of the Web API
                httpClient.BaseAddress = new Uri(webApiUrl);

                // Set the request content type
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Create the request content
                var content = new StringContent(requestDataJson, Encoding.UTF8, "application/json");

                var response = new HttpResponseMessage();
                if (method == Convert.ToInt32(Enums.HttpMethod.GET))
                {
                    // Send the POST request and get the response
                    response = await httpClient.GetAsync(uri);
                }
                else if (method == Convert.ToInt32(Enums.HttpMethod.POST))
                {
                    // Send the POST request and get the response
                    response = await httpClient.PostAsync(uri, content);

                }
                return response;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
            logger.Log("Error: " + ex.ToString());
            return null;
        }
    }
    #endregion

    #region Validation
    public static bool CheckWords(string words, int Mode)
    {
        //Check Range Expression
        if (Mode == 1)
        {
            string pattern = @"\b\d+\.\d+\s*-\s*\d+\.\d+\b";
            string pattern1 = @"\b\d+\s*-\s*\d+\b";
            Match match = Regex.Match(words, pattern);
            if (!match.Success)
            {
                match = Regex.Match(words, pattern1);
            }
            return match.Success;
        }

        //Check Numeric
        else if (Mode == 2)
        {
            double n;
            return double.TryParse(words, out n);
        }
        //Check Date dd/mm/yyyy or dd-mm-yyyy
        else if (Mode == 3)
        {
            Regex regex = new Regex(@"(((0|1)[0-9]|2[0-9]|3[0-1])\/(0[1-9]|1[0-2])\/((19|20)\d\d))$");
            bool isValid = regex.IsMatch(words.Trim());
            if (!isValid)
            {
                regex = new Regex(@"(((0|1)[0-9]|2[0-9]|3[0-1])\-(0[1-9]|1[0-2])\-((19|20)\d\d))$");
                isValid = regex.IsMatch(words.Trim());
            }

            return isValid;
        }

        return false;
    }
    public static void AutoCorrectResult(ref DataTable DtOCR_Value, List<CommonMedicalVal> common, int typeID)
    {
        int rowNum = DtOCR_Value.Rows.Count - 1;
        if (string.IsNullOrEmpty(Convert.ToString(DtOCR_Value.Rows[rowNum]["Result"])))
        {
            DtOCR_Value.Rows[rowNum].Delete();
            return;
        }
        double resOCR = Convert.ToDouble(DtOCR_Value.Rows[rowNum]["Result"]);
        double fromOCR = Convert.ToDouble(DtOCR_Value.Rows[rowNum]["RangeFrom"]);
        double toOCR = Convert.ToDouble(DtOCR_Value.Rows[rowNum]["RangeTill"]);

        var c = common.Find(x => x.Id == typeID);
        double from = c.HighestRangeFrom;
        double to = c.HighestRangeTill;

        double fromSubtract = fromOCR - from < 0 ? (fromOCR - from) * -1 : fromOCR - from;
        double toSubtract = to - toOCR < 0 ? (to - toOCR) * -1 : to - toOCR;

        double resFrom = from - resOCR < 0 ? (from - resOCR) * -1 : from - resOCR;
        double resTo = to - resOCR < 0 ? (to - resOCR) * -1 : to - resOCR;

        double diff = to - from;

        if (fromSubtract > 10)
        {
            DtOCR_Value.Rows[rowNum]["RangeFrom"] = from;
        }
        else if (fromSubtract > from)
        {
            DtOCR_Value.Rows[rowNum]["RangeFrom"] = fromOCR / 10;
        }

        if (toSubtract > 10)
        {
            DtOCR_Value.Rows[rowNum]["RangeTill"] = to;
        }
        else if (toSubtract > to)
        {
            DtOCR_Value.Rows[rowNum]["RangeTill"] = toOCR / 10;
        }

        if (resFrom > diff || resTo > diff)
        {
            DtOCR_Value.Rows[rowNum]["Result"] = resOCR / 10;
        }
    }
    public static string RemoveSpecialCharacters(this string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if (c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '.' || c == '_' | c == '-')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    static string ContainsMonth(string input)
    {
        string[] months = {
            "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE",
            "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER",
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEPT", "OCT", "NOV", "DEC"
        };

        string pattern = @"\b(" + string.Join("|", months) + @")\b";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        Match match = regex.Match(input);

        if (match.Success)
        {
            return match.Value;
        }

        return null; // No month found
    }
    static string ContainsYear(string input)
    {
        // Method 1: String Manipulation
        string year1 = input.Substring(input.IndexOf('-') + 1);
        if (year1 != "")
        {
            return year1;
        }

        // Method 2: Regular Expression
        string pattern = @"\b\d{4}\b";
        Match match = Regex.Match(input, pattern);
        if (match.Success)
        {
            string year2 = match.Value;
            return year2;  // Output: 2022
        }
        return null;
    }
    static int ConvertMonthToInt(string monthName)
    {
        string monthAbbreviationUpper = monthName.ToUpper();
        if (Mappings.monthMappings.ContainsKey(monthAbbreviationUpper))
        {
            return Mappings.monthMappings[monthAbbreviationUpper];
        }
        DateTime dateTime = DateTime.ParseExact(monthName, "MMMM", null);
        return dateTime.Month;
    }

    static void RemoveBlankData(ref DataTable dtOCR)
    {
        // Assuming you have a DataTable named "dataTable"

        // Iterate through each row of the DataTable in reverse order
        for (int i = dtOCR.Rows.Count - 1; i >= 0; i--)
        {
            DataRow row = dtOCR.Rows[i];

            // Iterate through each column of the DataTable
            foreach (DataColumn column in dtOCR.Columns)
            {
                // Check if the value is empty or blank
                if (string.IsNullOrWhiteSpace(row[column.ColumnName].ToString()))
                {
                    // Remove the row from the DataTable
                    dtOCR.Rows.RemoveAt(i);
                    break; // Break the inner loop and move to the next row
                }
            }
        }
    }
    #endregion

    #region Conversion
    public static int pdfToImage(string pdfPath, string tiffPath)
    {
        SautinSoft.PdfFocus f = new SautinSoft.PdfFocus();

        if (!Directory.Exists(tiffPath))
        {
            Directory.CreateDirectory(tiffPath);
        }
        f.OpenPdf(pdfPath);
        int pageCnt = f.PageCount;
        if (pageCnt > 0)
        {
            //Save all PDF pages to image folder as tiff images, 200 dpi
            f.ImageOptions.Dpi = 300;
            f.ImageOptions.ImageFormat = System.Drawing.Imaging.ImageFormat.Tiff;

            //Returns 3, if there is any exception while converting PDF to image
            pageCnt = f.ToImage(tiffPath, "Page") == 3 ? 0 : pageCnt;
        }
        f.ClosePdf();
        return pageCnt;
    }
    public static int ConvertPDFtoImages(string pdfPath, string tiffPath)
    {
        // Path to a document where to extract pictures.
        // By the way: You may specify DOCX, HTML, RTF files also. 
        DocumentCore dc = DocumentCore.Load(pdfPath);

        if (!Directory.Exists(tiffPath))
        {
            Directory.CreateDirectory(tiffPath);
        }

        // PaginationOptions allow to know, how many pages we have in the document. 
        DocumentPaginator dp = dc.GetPaginator(new PaginatorOptions());
        int pageCnt = dp.Pages.Count;
        if (pageCnt > 0)
        {
            for (int i = 0; i < pageCnt; i++)
            {
                //dp.Pages[i].Rasterize(800, SautinSoft.Document.Color.White).Save(@"E:\Workspace\Docs\BloodTests\PDFTextPath\Page" + i + ".tiff");
                dp.Pages[i].Rasterize(800, Color.White).Save(tiffPath + "Page" + (i + 1) + ".tiff");

            }
            // Each document page will be saved in its own image format: PNG, JPEG, TIFF with different DPI.
            //dp.Pages[0].Rasterize(800, SautinSoft.Document.Color.White).Save(@"E:\Workspace\Docs\BloodTests\PDFTextPath\example.png");
            //dp.Pages[1].Rasterize(400, SautinSoft.Document.Color.White).Save(@"E:\Workspace\Docs\BloodTests\PDFTextPath\example.jpeg");
            //dp.Pages[2].Rasterize(650, SautinSoft.Document.Color.White).Save(@"E:\Workspace\Docs\BloodTests\PDFTextPath\example.tiff");
        }
        return pageCnt;
    }
    #endregion

    static async Task KYCOCRProcessingAsync(DataTable dt, Proposal_Master proposalData, Document_Master document)
    {
        try
        {
            bool IsNameMatch = false;
            bool IsDOBMatch = false;
            bool IsCityMatch = false;
            bool IsPinCodeMatch = false;

            string strAddr = string.Empty;
            string strName = string.Empty;
            string strAadharNo = string.Empty;
            string strPAN = string.Empty;
            DataTable DtOCR_Value = new DataTable();
            DtOCR_Value.Columns.AddRange(new DataColumn[3]
            {
                  new DataColumn("FieldName",typeof(string)),
                  new DataColumn("FieldValue",typeof(string)),
                  new DataColumn("MatchStatus",typeof(string))
            });

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string value = Convert.ToString(dt.Rows[i]["text"]);
                if (RemoveSpecialCharacters(value) == "")
                {
                    continue;
                }

                bool isDateExpression = CheckWords(value.Trim(), 3);

                if (isDateExpression)
                {
                    if (!string.IsNullOrEmpty(proposalData.DOB))
                    {
                        bool matchDOB = value.ToUpper().Contains(proposalData.DOB.ToUpper());
                        if (!matchDOB)
                        {
                            continue;
                        }
                        else
                        {
                            IsDOBMatch = true;
                        }
                    }

                }
                else
                {
                    if (!string.IsNullOrEmpty(proposalData.FirstName))
                    {
                        bool matchName = value.ToUpper().Contains(proposalData.FirstName.ToUpper());
                        if (!matchName)
                        {
                            continue;
                        }
                        else
                        {
                            //if (item.valueCaptured == true)
                            //{
                            //    continue;
                            //}


                            int left = Convert.ToInt32(dt.Rows[i]["left"]);
                            int top = Convert.ToInt32(dt.Rows[i]["top"]);
                            int width = Convert.ToInt32(dt.Rows[i]["width"]);
                            int height = Convert.ToInt32(dt.Rows[i]["height"]);
                            int line_num = Convert.ToInt32(dt.Rows[i]["lin_num"]);
                            int block_num = Convert.ToInt32(dt.Rows[i]["block_num"]);
                            int para_num = Convert.ToInt32(dt.Rows[i]["para_num"]);

                            DataTable dt_MedReportValues = new DataTable();
                            DataView dv = new DataView(dt);
                            dv.RowFilter = "block_num = " + block_num + " AND para_num = " + para_num + " AND lin_num = " + line_num;
                            dt_MedReportValues = dv.ToTable();

                            List<string> words_MedValues = new List<string>();

                            foreach (DataRow dr_test in dt_MedReportValues.Rows)
                            {
                                if (dr_test["text"].ToString() != "" && dr_test["text"].ToString() != " ")
                                {
                                    //words_MedValues.Add(dr_test["text"].ToString());
                                    if (!strName.Contains(dr_test["text"].ToString()))
                                    {
                                        strName = strName + dr_test["text"].ToString() + " ";
                                    }
                                }
                            }

                            List<string> nameParameters = new List<string>();
                            nameParameters.Add(proposalData.FirstName);
                            if (proposalData.MiddleName != "--")
                            {
                                nameParameters.Add(proposalData.MiddleName);
                            }
                            nameParameters.Add(proposalData.LastName);
                            int cnt = nameParameters.Select(x => strName.ToUpper().Contains(x.ToUpper())).Count();
                            IsNameMatch = (cnt == nameParameters.Count);
                        }
                    }



                    if (!string.IsNullOrEmpty(proposalData.CurrentCity))
                    {
                        bool matchCity = value.ToUpper().Contains(proposalData.CurrentCity.ToUpper());
                        if (!matchCity)
                        {
                            continue;
                        }
                        else
                        {
                            IsCityMatch = true;
                        }
                    }


                    bool matchPinCode = value.Contains(proposalData.CurrentPinCode.ToString());
                    if (!matchPinCode)
                    {
                        continue;
                    }
                    else
                    {
                        IsPinCodeMatch = true;
                    }

                }
            }



            KYC_Verify_Master kyc_Verify_Master = new KYC_Verify_Master()
            {
                Name = Convert.ToInt32(IsNameMatch),
                DOB = Convert.ToInt32(IsDOBMatch),
                City = Convert.ToInt32(IsCityMatch),
                Pincode = Convert.ToInt32(IsPinCodeMatch),
                PolicyNo = document.policyNo,
                DocMasterID = document.docMasterID,
                CreatedDate = DateTime.Now,
                CreatedBy = "SYSTEM"
            };

            var requestDataJson = System.Text.Json.JsonSerializer.Serialize(kyc_Verify_Master);
            var med_Report_Master_Response = await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddkycOCRResult", Convert.ToInt32(Enums.HttpMethod.POST));

        }
        catch (Exception ex)
        {

        }
    }
    static void KYC_OCRPreProcessing(string imageFilePath)
    {
        try
        {
            //imageFilePath = @"G:\Workspace\Docs\Proof Of Address.jpg";
            Mat image = new Mat();
            image = Cv2.ImRead(imageFilePath);

            Mat GrayImage = new Mat();
            Cv2.CvtColor(image, GrayImage, ColorConversionCodes.BGR2GRAY);

            Mat BlurImage = new Mat();
            Cv2.GaussianBlur(GrayImage, BlurImage, new OpenCvSharp.Size(1, 1), 0);

            Mat Output = new Mat();
            Cv2.Threshold(BlurImage, Output, 160, 255, ThresholdTypes.Binary);  //54//80

            Mat img1 = new Mat();
            img1 = 255 - BlurImage;
            Output.SaveImage(pdf_Path + @"OCRImages\ProcessedKYCImage.jpeg");
        }
        catch (Exception ex)
        {

        }
    }

    #region Directory Copying
    //public static async Task DirectoryCopying(int interval, CancellationToken cancellationToken)
    public static async Task DirectoryCopying()
    {
        try
        {
            //while (!cancellationToken.IsCancellationRequested)
            //{
                // Perform Task 1 logic here
                Console.WriteLine($"Running DirectoryCopying at {DateTime.Now}");
                string sFTPServerPath = SFTPServerPath;
                string localFileServerPath = doc_Path + @"InsuranceDocs\";
                string currDate = DateTime.Now.ToString("yyyyMMdd");
                string[] dirs = Directory.GetDirectories(sFTPServerPath, "*", SearchOption.TopDirectoryOnly);

                foreach (string dir in dirs)
                {
                    string fileName = Path.GetFileName(dir);

                    if (currDate == fileName)
                    {
                        string[] proposalFolder = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                        foreach (var eachPropposalNo in proposalFolder)
                        {
                            string proposalNumber = Path.GetFileName(eachPropposalNo);
                            if (!Directory.Exists(localFileServerPath + proposalNumber))
                            {
                                Directory.CreateDirectory(localFileServerPath + proposalNumber);
                            }
                            string[] getAllFiles = Directory.GetFiles(eachPropposalNo);
                            foreach (var file in getAllFiles)
                            {
                                var destFileName = localFileServerPath + proposalNumber + @"\" + Path.GetFileNameWithoutExtension(file) + Path.GetExtension(file);
                                File.Copy(file, destFileName, true);
                            }
                        }
                    }
                }
                // Delay for the specified interval
            //    await Task.Delay(interval);

            //    // Check if cancellation has been requested
            //    if (cancellationToken.IsCancellationRequested)
            //        break;
            //}
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now}: Error in Directory Copying");
            logger.Log($"{DateTime.Now}: Error in Directory Copying");
            Console.WriteLine($"{DateTime.Now}: {ex.Message}");
            logger.Log($"{DateTime.Now}: {ex.Message}");
        }
    }
    //public static async Task ManagePolicyFoldersAsync(int interval, CancellationToken cancellationToken)
    public static async Task ManagePolicyFoldersAsync()
    {
        //while (!cancellationToken.IsCancellationRequested)
        //{
            // Perform Task 3 logic here
            Console.WriteLine($"Running ManagePolicyFolders at {DateTime.Now}");
            string[] dirs = Directory.GetDirectories(doc_Path + @"InsuranceDocs\", "*", SearchOption.TopDirectoryOnly);
            var requestDataJson = System.Text.Json.JsonSerializer.Serialize(dirs);
            await SendHttpRequest(requestDataJson, "PolicyMasterAPI/CheckPolicyMaster", Convert.ToInt32(Enums.HttpMethod.POST));

            // Delay for the specified interval
        //    await Task.Delay(interval);

        //    // Check if cancellation has been requested
        //    if (cancellationToken.IsCancellationRequested)
        //        break;
        //}
    }
    //public static async Task ManageDocsForPolicies(int interval, CancellationToken cancellationToken)
    public static async Task ManageDocsForPolicies()
    {
        //while (!cancellationToken.IsCancellationRequested)
        //{
            // Perform Task 3 logic here
            Console.WriteLine($"Running ManageDocsForPolicies task at {DateTime.Now}");
            List<Policy_Docs_Model> policy_Docs_Model = new List<Policy_Docs_Model>();
            string[] dirs = Directory.GetDirectories(doc_Path +@"InsuranceDocs\", "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
            {
                string directoryName = dir.Split(@"\")[dir.Split(@"\").Length - 1];
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                string[] modifiedFiles = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    modifiedFiles[i] = files[i].Replace(doc_Path, @"\");
                }
                policy_Docs_Model.Add(new Policy_Docs_Model()
                {
                    PolicyNo = Convert.ToInt32(directoryName),
                    Files = modifiedFiles
                });
            }
            var requestDataJson = System.Text.Json.JsonSerializer.Serialize(policy_Docs_Model);
            await SendHttpRequest(requestDataJson, "PolicyMasterAPI/ManageDocsForPolicies", Convert.ToInt32(Enums.HttpMethod.POST));

            // Delay for the specified interval
        //    await Task.Delay(interval);

        //    // Check if cancellation has been requested
        //    if (cancellationToken.IsCancellationRequested)
        //        break;
        //}

    }
    #endregion

    #region OCRProcessing
    //public static async Task ProcessPendingDocsAsync(int interval, CancellationToken cancellationToken)
    public static async Task ProcessPendingDocsAsync()
    {
        //while (!cancellationToken.IsCancellationRequested)
        //{
        // Perform Task 1 logic here
        Console.WriteLine($"Running ProcessPendingDocsAsync at {DateTime.Now}");

        try
        {
            List<Document_Master> ocrData = new List<Document_Master>();
            var ocrDataResponse = await SendHttpRequest("", "PolicyMasterAPI/GetPendingOCRData", Convert.ToInt32(Enums.HttpMethod.GET));

            // Check if the response was successful
            if (ocrDataResponse.IsSuccessStatusCode)
            {
                // Read the response content
                var ocrDataResponseContent = await ocrDataResponse.Content.ReadAsStringAsync();
                ocrData = System.Text.Json.JsonSerializer.Deserialize<List<Document_Master>>(ocrDataResponseContent);
                if (ocrData == null)
                {
                    return;
                }
                // Process the response as needed
                Console.WriteLine("Response: " + ocrDataResponse);
                logger.Log("Response: " + ocrDataResponse);
            }
            else
            {
                // Handle the error response
                Console.WriteLine("Error: " + ocrDataResponse.StatusCode);
                logger.Log("Error: " + ocrDataResponse.StatusCode);
            }

            DataTable dt = new DataTable();

            dt.Columns.AddRange(new DataColumn[12]
            {
                  new DataColumn("Level",typeof(int)),
                  new DataColumn("page_num",typeof(string)),
                  new DataColumn("block_num",typeof(string)),
                  new DataColumn("para_num",typeof(string)),
                  new DataColumn("lin_num",typeof(string)),
                  new DataColumn("word_num",typeof(string)),
                  new DataColumn("left",typeof(string)),
                  new DataColumn("top",typeof(string)),
                  new DataColumn("width",typeof(string)),
                  new DataColumn("height",typeof(string)),
                  new DataColumn("conf",typeof(string)),
                  new DataColumn("text",typeof(string))
            });


            //foreach (var document in ocrData.FindAll(x => x.docType.ToUpper() != "OTHERS"))
            foreach (var document in ocrData)
            {
                Proposal_Master proposalData = new Proposal_Master();
                string path = "";
                if (document.docType == "ECG")
                {
                    continue;
                }
                if (document.docType == "OTHERS")
                {
                    continue;
                }
                if (document.docType == "KYC")
                {
                    var requestDataJson = System.Text.Json.JsonSerializer.Serialize(document.policyNo);
                    var proposalDataResponse = await SendHttpRequest("", "PolicyMasterAPI/GetProposalDetailsByPolicyNo?id=" + document.policyNo, Convert.ToInt32(Enums.HttpMethod.GET));

                    // Check if the response was successful
                    if (proposalDataResponse.IsSuccessStatusCode)
                    {
                        var proposalDataResponseContent = await proposalDataResponse.Content.ReadAsStringAsync();
                        var jsonSerializerSettings = new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        };
                        proposalData = JsonConvert.DeserializeObject<Proposal_Master>(proposalDataResponseContent, jsonSerializerSettings);
                        if (proposalData == null)
                        {
                            return;
                        }
                        KYC_OCRPreProcessing(doc_Path + document.document_Path);
                    }
                    else
                    {
                        // Handle the error response
                        Console.WriteLine("Error: " + ocrDataResponse.StatusCode);
                        logger.Log("Error: " + ocrDataResponse.StatusCode);
                    }
                }
                if (document.docExtension == FileExtensions.Jpg || document.docExtension == FileExtensions.Png || document.docExtension == FileExtensions.Tiff)
                {
                    if (document.docType == "KYC")
                        path = pdf_Path + @"OCRImages\ProcessedKYCImage.jpeg";
                    else
                        path = doc_Path + document.document_Path;
                    OCRProcessingForImages(doc_Path + document.document_Path, ref dt);
                }
                else if (document.docExtension == FileExtensions.Pdf)
                {
                    OCRProcessingForPDF(document, ref dt);
                }
                if (document.docType == "KYC")
                {
                    KYCOCRProcessingAsync(dt, proposalData, document);
                }
                if (document.docType == "Medical")
                {
                    await PassOCR(dt, document);
                }
                else
                {
                    if (document.docType != "OTHERS")
                        await PassData(dt, document);
                }
            }
        }
        //    // Delay for the specified interval
        //    await Task.Delay(interval);

        //    // Check if cancellation has been requested
        //    if (cancellationToken.IsCancellationRequested)
        //        break;
        //}
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now}: Error in OCR Processing");
            logger.Log($"{DateTime.Now}: Error in OCR Processing");
            Console.WriteLine($"{DateTime.Now}: {ex.Message}");
            logger.Log($"{DateTime.Now}: {ex.Message}");
        }
    }
    public static async Task PassData(DataTable dt, Document_Master doc_Master)
    {
        try
        {
            string? Type = doc_Master.docType;
            int policyNo = doc_Master.policyNo;
            DataTable DtOCR_Value = new DataTable();
            DtOCR_Value.Columns.AddRange(new DataColumn[2]
            {
                  new DataColumn("Field Name",typeof(string)),
                  new DataColumn("Value",typeof(string))
            });
            if (Type == "MonthlyFinancial")
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i]["text"].ToString().ToUpper().Contains("GROSS")
                         && dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("EARNING"))
                    {
                        DataRow dr = DtOCR_Value.NewRow();
                        dr["Field Name"] = "Gross Earnings";
                        dr["Value"] = dt.Rows[i + 2]["text"].ToString();
                        DtOCR_Value.Rows.Add(dr);
                    }
                    if (Convert.ToString(dt.Rows[i]["text"]).ToUpper().Contains("MONTH"))
                    {
                        string combinedWord = Convert.ToString(dt.Rows[i - 3]["text"]) + Convert.ToString(dt.Rows[i - 2]["text"]) + Convert.ToString(dt.Rows[i - 1]["text"]) + Convert.ToString(dt.Rows[i]["text"]);
                        if (combinedWord.ToUpper().Contains("FORTHEMONTH"))
                        {
                            int cnt = i + 1;
                            while (ContainsMonth(Convert.ToString(dt.Rows[cnt]["text"]).ToUpper()) == null)
                            {
                                cnt++;
                            }
                            string valString = Convert.ToString(dt.Rows[cnt]["text"]).ToUpper();
                            DataRow drMonth = DtOCR_Value.NewRow();
                            drMonth["Field Name"] = "Month";
                            drMonth["Value"] = ContainsMonth(valString);
                            DtOCR_Value.Rows.Add(drMonth);

                            if (ContainsYear(valString) != null)
                            {
                                DataRow drYear = DtOCR_Value.NewRow();
                                drYear["Field Name"] = "Year";
                                drYear["Value"] = ContainsYear(valString);
                                DtOCR_Value.Rows.Add(drYear);
                            }
                            i = cnt;
                        }
                    }


                }
                if (DtOCR_Value.Rows.Count > 0)
                {
                    Monthly_Finance_Master monthly_Finance_Master = new Monthly_Finance_Master();
                    monthly_Finance_Master.PolicyNo = policyNo;
                    decimal salaryAmt = decimal.Parse(RemoveSpecialCharacters(Convert.ToString(DtOCR_Value.Rows[2]["Value"])));
                    monthly_Finance_Master.SalaryAmount = (int)salaryAmt;
                    monthly_Finance_Master.Month = ConvertMonthToInt(Convert.ToString(DtOCR_Value.Rows[0]["Value"]));
                    monthly_Finance_Master.Year = Convert.ToInt32(DtOCR_Value.Rows[1]["Value"]);
                    monthly_Finance_Master.CreatedBy = "SYSTEM";
                    monthly_Finance_Master.CreatedDate = DateTime.Now;
                    monthly_Finance_Master.DocMasterID = doc_Master.docMasterID;
                    var requestDataJson = System.Text.Json.JsonSerializer.Serialize(monthly_Finance_Master);
                    await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMonthlyFinanceValues", Convert.ToInt32(Enums.HttpMethod.POST));
                }
            }
            if (Type == "YearlyFinancial")
            {
                //for (int i = 0; i < dt.Rows.Count; i++)
                //{
                //    if (dt.Rows[i]["text"].ToString().ToUpper().Contains("GROSS")
                //         && dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("EARNING"))
                //    {
                //        DataRow dr = DtOCR_Value.NewRow();
                //        dr["Field Name"] = "Gross Earnings";
                //        dr["Value"] = dt.Rows[i + 2]["text"].ToString();
                //        DtOCR_Value.Rows.Add(dr);
                //    }
                //    if (Convert.ToString(dt.Rows[i]["text"]).ToUpper().Contains("MONTH"))
                //    {
                //        string combinedWord = Convert.ToString(dt.Rows[i - 3]["text"]) + Convert.ToString(dt.Rows[i - 2]["text"]) + Convert.ToString(dt.Rows[i - 1]["text"]) + Convert.ToString(dt.Rows[i]["text"]);
                //        if (combinedWord.ToUpper().Contains("FORTHEMONTH"))
                //        {
                //            int cnt = i + 1;
                //            while (ContainsMonth(Convert.ToString(dt.Rows[cnt]["text"]).ToUpper()) == null)
                //            {
                //                cnt++;
                //            }
                //            string valString = Convert.ToString(dt.Rows[cnt]["text"]).ToUpper();
                //            DataRow drMonth = DtOCR_Value.NewRow();
                //            drMonth["Field Name"] = "Month";
                //            drMonth["Value"] = ContainsMonth(valString);
                //            DtOCR_Value.Rows.Add(drMonth);

                //            if (ContainsYear(valString) != null)
                //            {
                //                DataRow drYear = DtOCR_Value.NewRow();
                //                drYear["Field Name"] = "Year";
                //                drYear["Value"] = ContainsYear(valString);
                //                DtOCR_Value.Rows.Add(drYear);
                //            }
                //            i = cnt;
                //        }
                //    }
                //}

                Yearly_Finance_Master yearly_Finance_Master = new Yearly_Finance_Master();
                yearly_Finance_Master.PolicyNo = policyNo;
                decimal salaryAmt = decimal.Parse(RemoveSpecialCharacters(Convert.ToString(DtOCR_Value.Rows[2]["Value"])));
                yearly_Finance_Master.Form16_AY = (int)salaryAmt;
                yearly_Finance_Master.Gross_AY = (int)salaryAmt;
                yearly_Finance_Master.YearFrom = ConvertMonthToInt(Convert.ToString(DtOCR_Value.Rows[0]["Value"]));
                yearly_Finance_Master.YearTo = Convert.ToInt32(DtOCR_Value.Rows[1]["Value"]);
                yearly_Finance_Master.CreatedBy = "SYSTEM";
                yearly_Finance_Master.CreatedDate = DateTime.Now;
                yearly_Finance_Master.DocMasterID = doc_Master.docMasterID;
                var requestDataJson = System.Text.Json.JsonSerializer.Serialize(yearly_Finance_Master);
                await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMonthlyFinanceValues", Convert.ToInt32(Enums.HttpMethod.POST));
            }
            else if (Type == "Proposal")
            {
                //Read Medical Parameters from JSON file
                string text = File.ReadAllText(@"./ProposalValues.json");
                var common = System.Text.Json.JsonSerializer.Deserialize<List<ProposalValues>>(text);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    try
                    {
                        string value = Convert.ToString(dt.Rows[i]["text"]);
                        if (RemoveSpecialCharacters(value) == "")
                        {
                            continue;
                        }
                        foreach (var item in common)
                        {
                            string[] alternatives = item.Alternatives.Split('|');
                            bool match = Array.Exists(alternatives, x => (value.ToUpper()).Contains(x));
                            if (!match)
                            {
                                continue;
                            }
                            else
                            {
                                string Match = value.ToUpper();
                            }


                            if (item.valueCaptured == true)
                            {
                                continue;
                            }

                            if (dt.Rows[i]["text"].ToString().ToUpper().Contains("PROPOSAL")
                                && !dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("NUMBER"))
                            {
                                continue;
                            }

                            if (dt.Rows[i]["text"].ToString().ToUpper().Contains("NAME")
                                && !dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("OF")
                                && !dt.Rows[i + 2]["text"].ToString().ToUpper().Contains("ENTITY"))
                            {
                                continue;
                            }

                            if (
                                (dt.Rows[i]["text"].ToString() + dt.Rows[i + 1]["text"].ToString() + dt.Rows[i + 2]["text"].ToString()).ToUpper().Contains("CITY")
                                && !(dt.Rows[i]["text"].ToString() + dt.Rows[i + 1]["text"].ToString() + dt.Rows[i + 2]["text"].ToString()).ToUpper().Contains("DISTRICT")
                                )
                            {
                                continue;
                            }

                            if (
                                (dt.Rows[i]["text"].ToString() + dt.Rows[i + 1]["text"].ToString() + dt.Rows[i + 2]["text"].ToString()).ToUpper().Contains("PLAN")
                                && !(dt.Rows[i]["text"].ToString() + dt.Rows[i + 1]["text"].ToString() + dt.Rows[i + 2]["text"].ToString()).ToUpper().Contains("COVER")
                                )
                            {
                                continue;
                            }




                            if (dt.Rows[i]["text"].ToString().ToUpper().Contains("PIN")
                                && !dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("CODE"))
                            {
                                continue;
                            }

                            //if (dt.Rows[i]["text"].ToString().ToUpper().Contains("CURRENT")
                            //    && dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("RESIDENTIAL")
                            //    && dt.Rows[i + 2]["text"].ToString().ToUpper().Contains("ADDRESS"))

                            //{
                            //    List<string> words_AddrValues = new List<string>();

                            //    for (int j = 2; j <= 70; j++)
                            //    {
                            //        if (dt.Rows[i + j]["text"].ToString().ToUpper() != "")
                            //        {
                            //            words_AddrValues.Add(dt.Rows[i + j]["text"].ToString().ToUpper());
                            //        }
                            //    }
                            //}



                            //if (dt.Rows[i]["text"].ToString().ToUpper().Contains("PROPOSAL") && dt.Rows[i + 1]["text"].ToString().ToUpper() != "COUNT")
                            //{
                            //    continue;
                            //}


                            int left = Convert.ToInt32(dt.Rows[i]["left"]);
                            int top = Convert.ToInt32(dt.Rows[i]["top"]);
                            int width = Convert.ToInt32(dt.Rows[i]["width"]);
                            int height = Convert.ToInt32(dt.Rows[i]["height"]);
                            int line_num = Convert.ToInt32(dt.Rows[i]["lin_num"]);
                            int block_num = Convert.ToInt32(dt.Rows[i]["block_num"]);
                            int para_num = Convert.ToInt32(dt.Rows[i]["para_num"]);

                            DataTable dt_MedReportValues = new DataTable();
                            DataView dv = new DataView(dt);
                            dv.RowFilter = "block_num = " + block_num + " AND para_num = " + para_num + " AND lin_num = " + line_num;
                            dt_MedReportValues = dv.ToTable();

                            List<string> words_ProposalValues = new List<string>();

                            foreach (DataRow dr_test in dt_MedReportValues.Rows)
                            {
                                if (dr_test["text"].ToString() != "" && dr_test["text"].ToString() != " ")
                                {
                                    words_ProposalValues.Add(dr_test["text"].ToString());
                                }
                            }

                            DataRow row = DtOCR_Value.NewRow();
                            DtOCR_Value.Rows.Add(row);
                            int rowNum = DtOCR_Value.Rows.Count - 1;

                            DtOCR_Value.Rows[rowNum]["Field Name"] = item.FieldName;

                            switch (value.ToUpper())
                            {
                                case "PROPOSAL":

                                    for (int j = 0; j < words_ProposalValues.Count; j++)
                                    {
                                        string val = RemoveSpecialCharacters(words_ProposalValues[j]);
                                        bool isNumeric = CheckWords(val, 2);

                                        if (isNumeric)
                                        {

                                            DtOCR_Value.Rows[rowNum]["Value"] = val;
                                            break;
                                        }
                                    }
                                    break;

                                case "TITLE":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "FIRST":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "MIDDLE":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString() == "" ? "--" : dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "LAST":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "BIRTH":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "EDUCATION":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;

                                case "OCCUPATION":
                                    int k = words_ProposalValues.IndexOf("Title");

                                    DtOCR_Value.Rows[rowNum]["Value"] = words_ProposalValues[k + 1] + words_ProposalValues[k + 2] + words_ProposalValues[k + 3];

                                    break;

                                case "NAME":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 7]["text"].ToString() + " " + dt.Rows[i + 8]["text"].ToString();

                                    break;

                                case "ANNUAL":
                                    for (int j = 0; j < words_ProposalValues.Count; j++)
                                    {
                                        string val = RemoveSpecialCharacters(words_ProposalValues[j]);
                                        bool isNumeric = CheckWords(val, 2);

                                        if (isNumeric)
                                        {
                                            DtOCR_Value.Rows[rowNum]["Value"] = val;   //to get last numeric value
                                        }
                                    }

                                    break;

                                case "PERCENTAGE":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 1]["text"].ToString();

                                    break;



                                case "CITY":
                                case "CITY/":

                                    if (item.FieldName == "Current City")
                                    {
                                        DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 3]["text"].ToString();
                                    }
                                    else if (item.FieldName == "Permanent City")
                                    {
                                        DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 2]["text"].ToString();
                                    }

                                    break;

                                case "PIN":

                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 2]["text"].ToString();

                                    break;

                                case "MOBILE":
                                    for (int j = 0; j < words_ProposalValues.Count; j++)
                                    {
                                        string val = RemoveSpecialCharacters(words_ProposalValues[j]);

                                        bool isNumeric = CheckWords(val, 2);

                                        if (isNumeric)
                                        {
                                            if (val.Length == 10)
                                            {
                                                DtOCR_Value.Rows[rowNum]["Value"] = val;
                                                break;
                                            }
                                        }
                                    }

                                    break;

                                case "E-MAIL":
                                    DtOCR_Value.Rows[rowNum]["Value"] = words_ProposalValues.Last();

                                    break;




                                case "PLAN":
                                    DtOCR_Value.Rows[rowNum]["Value"] = dt.Rows[i + 3]["text"].ToString();

                                    break;


                            }


                            if (!string.IsNullOrEmpty(DtOCR_Value.Rows[rowNum]["Value"].ToString()))
                                item.valueCaptured = true;
                            else
                            {
                                item.valueCaptured = false;
                                DtOCR_Value.Rows.Remove(row);
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
                Proposal_Master proposal_Master = new Proposal_Master();
                proposal_Master.PolicyNo = Convert.ToInt32(DtOCR_Value.Rows[0]["Value"]);
                proposal_Master.Title = (string?)DtOCR_Value.Rows[1]["Value"];
                proposal_Master.FirstName = (string?)DtOCR_Value.Rows[2]["Value"];
                proposal_Master.MiddleName = (string?)DtOCR_Value.Rows[3]["Value"];
                proposal_Master.LastName = (string?)DtOCR_Value.Rows[4]["Value"];
                DateTime dob = DateTime.ParseExact((string)DtOCR_Value.Rows[5]["Value"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string formattedDate = dob.ToString("yyyy-MM-dd");
                proposal_Master.DOB = formattedDate;
                proposal_Master.Education = (string?)DtOCR_Value.Rows[6]["Value"];
                proposal_Master.Occupation = (string?)DtOCR_Value.Rows[7]["Value"];
                proposal_Master.Employer = (string?)DtOCR_Value.Rows[8]["Value"];
                proposal_Master.AnnualIncome = Convert.ToDecimal(DtOCR_Value.Rows[9]["Value"]);
                proposal_Master.PercentageShare = Convert.ToDecimal(DtOCR_Value.Rows[10]["Value"]);
                proposal_Master.CurrentCity = (string?)DtOCR_Value.Rows[11]["Value"];
                proposal_Master.CurrentPinCode = Convert.ToDecimal(DtOCR_Value.Rows[12]["Value"]);
                proposal_Master.MobileNo = Convert.ToDecimal(DtOCR_Value.Rows[13]["Value"]);
                proposal_Master.EmailID = (string?)DtOCR_Value.Rows[14]["Value"];
                proposal_Master.PermanentCity = (string?)DtOCR_Value.Rows[15]["Value"];
                proposal_Master.PermanentPinCode = (string?)DtOCR_Value.Rows[16]["Value"];
                proposal_Master.SumAssured = Convert.ToDecimal(DtOCR_Value.Rows[17]["Value"]);
                //proposal_Master.Gender = "";
                //proposal_Master.RelationWithProposer = "";
                proposal_Master.CreatedDate = DateTime.Now;
                proposal_Master.CreatedBy = "SYSTEM";
                proposal_Master.DocMasterID = doc_Master.docMasterID;

                var requestDataJson = System.Text.Json.JsonSerializer.Serialize(proposal_Master);
                await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddProposalValues", Convert.ToInt32(Enums.HttpMethod.POST));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error");
        }
    }
    public static void OCRProcessingForImages(string path, ref DataTable dt)
    {
        var str_text3 = "";
        Console.WriteLine($"{DateTime.Now}: Running OCR for image " + path);
        logger.Log("Running OCR for image " + path);

        //ECG
        //string imageFilePath = doc_Path + @"ECGTests\ECGTestImages\Page" + i + ".tiff";
        //Mat image = new Mat();
        //image = Cv2.ImRead(imageFilePath);
        //Mat Output = new Mat();
        //Cv2.Threshold(image, Output, 153, 255, ThresholdTypes.Binary);
        //Output.SaveImage(@"G:\Workspace\Docs\ECGTests\ECGTestImages\ProcessedImage.jpeg");


        using (var tEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default)) //creating the tesseract OCR engine with English as the language
        {
            tEngine.SetVariable("debug_file", "null");

            //Load of the image file from the Pix object which is a wrapper for Leptonica PIX structure
            using (var img = Pix.LoadFromFile(path)) // Blood Test
            //using (var img = Pix.LoadFromFile(pdf_Path + @"ECGTestImages\ProcessedImage.jpeg")) // ECG
            {
                var pixFile = img.Rotate((float)(90 * Math.PI / 180.0f)); //ECG
                //pixFile = pixFile.Deskew();
                using (var page = tEngine.Process(img)) //process the specified image
                {
                    var str_xml = page.GetHOCRText(0);
                    str_text3 = page.GetTsvText(1);

                }
            }
        }

        string[] str_table = str_text3.Split('\n');

        foreach (var item in str_table)
        {
            if (item != "")
            {
                string[] str_item = item.Split('\t');

                dt.Rows.Add(str_item[0], str_item[1], str_item[2], str_item[3], str_item[4], str_item[5], str_item[6],
                    str_item[7], str_item[8], str_item[9], str_item[10], str_item[11]);
            }
        }
        Console.WriteLine($"{DateTime.Now}: OCR completed!");
        logger.Log("OCR completed!");
    }
    public static void OCRProcessingForPDF(Document_Master document, ref DataTable dt)
    {
        Console.WriteLine($"{DateTime.Now}: Running OCR for " + document.documentName);
        Console.WriteLine($"{DateTime.Now}: Converting PDF to images");
        logger.Log("Converting PDF to images");

        //SautinSoft
        string imagePath = pdf_Path + @"OCRImages\";
        int pageCnt = ConvertPDFtoImages(doc_Path + document.document_Path, imagePath);

        Console.WriteLine($"{DateTime.Now}: Images Generated");
        logger.Log("Images Generated");

        for (int i = 1; i <= pageCnt; i++)
        {
            OCRProcessingForImages(pdf_Path + @"OCRImages\Page" + i + ".tiff", ref dt);
        }
    }
    public static async Task PassOCR(DataTable dt, Document_Master document_Master)
    {
        try
        {
            DataTable dtOCR = ManipulatingOCRData(dt);
            RemoveBlankData(ref dtOCR);

            Med_Report_Master med_Report_Master = new Med_Report_Master()
            {
                PolicyNo = document_Master.policyNo,
                DocMasterID = document_Master.docMasterID,
                ReportType = document_Master.docType,
                ReportName = document_Master.documentName,
                ReportDate = DateTime.Now,
                Age = 20
            };

            var requestDataJson = System.Text.Json.JsonSerializer.Serialize(med_Report_Master);
            var med_Report_Master_Response = await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMedicalReport", Convert.ToInt32(Enums.HttpMethod.POST));

            // Check if the response was successful
            if (med_Report_Master_Response.IsSuccessStatusCode)
            {
                // Read the response content
                var med_Report_Master_Content = await med_Report_Master_Response.Content.ReadAsStringAsync();

                // Process the response as needed
                Console.WriteLine("Response: " + med_Report_Master_Response);
                logger.Log("Response: " + med_Report_Master_Response);

                List<Med_Report_Details> med_Report_Details = new List<Med_Report_Details>();

                foreach (DataRow dr in dtOCR.Rows)
                {
                    bool isNumeric = CheckWords(Convert.ToString(dr["Result"]), 2);
                    med_Report_Details.Add(new Med_Report_Details()
                    {
                        ReportID = Convert.ToInt32(med_Report_Master_Content),
                        TestName = Convert.ToString(dr["Test"]),
                        NumericTestValue = isNumeric ? Convert.ToDouble(dr["Result"]) : 0,
                        StringTestValue = !isNumeric ? Convert.ToString(dr["Result"]) : "",
                        RangeFrom = dr["RangeFrom"].Equals(System.DBNull.Value) ? 0 : Convert.ToDouble(dr["RangeFrom"]),
                        RangeTill = dr["RangeTill"].Equals(System.DBNull.Value) ? 0 : Convert.ToDouble(dr["RangeTill"]),
                        HealthStatus = Convert.ToString(dr["HealthStatus"]),
                        CreatedDate = DateTime.Now,
                        ModifiedDate = null,
                        CreatedBy = "SYSTEM"
                    });
                }

                requestDataJson = System.Text.Json.JsonSerializer.Serialize(med_Report_Details);
                await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMedicalValues", Convert.ToInt32(Enums.HttpMethod.POST));

            }
            else
            {
                // Handle the error response
                Console.WriteLine("Error: " + med_Report_Master_Response.StatusCode);
                logger.Log("Error: " + med_Report_Master_Response.StatusCode);
            }
        }
        catch (Exception ex)
        {

        }
    }
    public static DataTable ManipulatingOCRData(DataTable dt)
    {
        //Read Medical Parameters from JSON file
        string text = File.ReadAllText(@"./CommonMedValues.json");
        var common = System.Text.Json.JsonSerializer.Deserialize<List<CommonMedicalVal>>(text);
        try
        {
            DataTable DtOCR_Value = new DataTable();
            DtOCR_Value.Columns.AddRange(new DataColumn[5]
            {
                  new DataColumn("Test",typeof(string)),
                  new DataColumn("Result",typeof(string)),
                  new DataColumn("RangeFrom",typeof(string)),
                  new DataColumn("RangeTill",typeof(string)),
                  new DataColumn("HealthStatus",typeof(string))
            });

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    string value = Convert.ToString(dt.Rows[i]["text"]);
                    if (RemoveSpecialCharacters(value) == "")
                    {
                        continue;
                    }

                    if ((value.ToUpper().Contains("URINE") && dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("EXAMINATION"))
                        || value.ToUpper().Contains("URINE") && dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("ROUTINE") && dt.Rows[i + 2]["text"].ToString().ToUpper().Contains("EXAMINATION"))
                    {
                        var urineMenu = common.FindAll(x => x.IsSubMenu == true);
                        var menu = common.FindAll(x => x.IsSubMenu == false);

                        DataRow dr = DtOCR_Value.NewRow();
                        DtOCR_Value.Rows.Add(dr);
                        int rowNumber = DtOCR_Value.Rows.Count - 1;

                        bool altMatch = false;
                        for (int counter = i + 2; counter < dt.Rows.Count; counter++)
                        {
                            i = counter;
                            string v = Convert.ToString(dt.Rows[counter]["text"]);

                            bool nonurine = menu.Exists(x => x.TestName == v.ToUpper());
                            if (nonurine)
                                break;

                            Console.WriteLine($"{DateTime.Now}: Urine Test Keyword: " + v);
                            logger.Log("Keyword: " + v);
                            if (string.IsNullOrEmpty(v))
                            {
                                continue;
                            }

                            if (!DtOCR_Value.Rows[rowNumber].ItemArray.All(x => x is DBNull))
                            {
                                if (DtOCR_Value.Rows[rowNumber]["Result"].ToString() != "")
                                {
                                    dr = DtOCR_Value.NewRow();
                                    DtOCR_Value.Rows.Add(dr);
                                    rowNumber = DtOCR_Value.Rows.Count - 1;
                                }
                            }

                            foreach (var item in urineMenu)
                            {
                                if (DtOCR_Value.Rows[rowNumber]["Test"].ToString() == "")
                                    altMatch = v.ToUpper().Contains(item.Alternatives);
                                if (altMatch)
                                {
                                    if (DtOCR_Value.Rows[rowNumber]["Test"].ToString() == "")
                                    {
                                        DtOCR_Value.Rows[rowNumber]["Test"] = item.TestName;
                                        break;
                                    }
                                    if (item.StringResult == null || item.StringResult == "")
                                    {
                                        if (CheckWords(v, 2))
                                        {
                                            DtOCR_Value.Rows[rowNumber]["Result"] = v;
                                            altMatch = false;
                                            dr = DtOCR_Value.NewRow();
                                            DtOCR_Value.Rows.Add(dr);
                                            rowNumber = DtOCR_Value.Rows.Count - 1;
                                            break;
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        if (item.StringResult.Contains(v.ToUpper()))
                                        {
                                            if (item.StringResult == v.ToUpper())
                                            {
                                                DtOCR_Value.Rows[rowNumber]["Result"] = v;
                                                altMatch = false;
                                                break;
                                            }
                                            string res = v;
                                            string[] valReferences = item.StringResult.Split('|');
                                            int exactIndex = Array.FindIndex(valReferences, x => v.ToUpper().Trim().Equals(x));
                                            if (exactIndex == -1)
                                            {
                                                counter++;
                                                res = res + " " + Convert.ToString(dt.Rows[counter]["text"]);
                                                int containsIndex = Array.FindIndex(valReferences, x => res.ToUpper().Contains(x));
                                                while (res.ToUpper() != valReferences[containsIndex])
                                                {
                                                    counter++;
                                                    res = res + " " + Convert.ToString(dt.Rows[counter]["text"]);
                                                }
                                            }
                                            DtOCR_Value.Rows[rowNumber]["Result"] = res;
                                            altMatch = false;
                                            break;
                                        }
                                        continue;
                                    }
                                }
                                if (!altMatch)
                                    continue;

                                i = counter;
                                break;
                            }
                        }
                    }

                    foreach (var item in common)
                    {
                        string[] alternatives = item.Alternatives.Split('|');
                        bool match = Array.Exists(alternatives, x => value.ToUpper().Contains(x));
                        if (!match)
                        {
                            continue;
                        }
                        if (item.valueCaptured == true)
                        {
                            continue;
                        }
                        if (dt.Rows[i]["text"].ToString().ToUpper().Contains("GLYCOSYLATED")
                            || dt.Rows[i - 1]["text"].ToString().ToUpper().Contains("GLYCOSYLATED")
                            || dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("GLYCOSYLATED"))
                        {
                            continue;
                        }
                        if (dt.Rows[i]["text"].ToString().ToUpper().Contains("URINE")
                            && !dt.Rows[i + 1]["text"].ToString().ToUpper().Contains("EXAMINATION"))
                        {
                            continue;
                        }

                        int left = Convert.ToInt32(dt.Rows[i]["left"]);
                        int top = Convert.ToInt32(dt.Rows[i]["top"]);
                        int width = Convert.ToInt32(dt.Rows[i]["width"]);
                        int height = Convert.ToInt32(dt.Rows[i]["height"]);
                        int line_num = Convert.ToInt32(dt.Rows[i]["lin_num"]);
                        int block_num = Convert.ToInt32(dt.Rows[i]["block_num"]);
                        int para_num = Convert.ToInt32(dt.Rows[i]["para_num"]);

                        DataTable dt_MedReportValues = new DataTable();
                        DataView dv = new DataView(dt);
                        dv.RowFilter = "block_num = " + block_num + " AND para_num = " + para_num + " AND lin_num = " + line_num;
                        dt_MedReportValues = dv.ToTable();

                        List<string> words_MedValues = new List<string>();

                        foreach (DataRow dr_test in dt_MedReportValues.Rows)
                        {
                            if (dr_test["text"].ToString() != "" && dr_test["text"].ToString() != " ")
                            {
                                words_MedValues.Add(dr_test["text"].ToString());
                            }
                        }

                        DataRow row = DtOCR_Value.NewRow();
                        DtOCR_Value.Rows.Add(row);
                        int rowNum = DtOCR_Value.Rows.Count - 1;

                        if (item.IsMultiWord)
                        {
                            string resultantString = string.Join("", words_MedValues);
                            Console.WriteLine($"{DateTime.Now}: Multi-Keyword: " + resultantString);
                            logger.Log($"{DateTime.Now}: Multi-Keyword: " + resultantString);
                            string[] alt = item.MultiWordAlternatives.Split('|');
                            if (!Array.Exists(alt, x => resultantString.ToUpper().Contains(x.ToUpper())))
                            {
                                if (string.IsNullOrEmpty(Convert.ToString(DtOCR_Value.Rows[rowNum]["Result"])))
                                    DtOCR_Value.Rows[rowNum].Delete();
                                continue;
                            }
                        }

                        for (int j = 0; j < words_MedValues.Count; j++)
                        {
                            Console.WriteLine($"{DateTime.Now}: Keyword: " + words_MedValues[j]);
                            logger.Log("Keyword: " + words_MedValues[j]);
                            //Check whether word captured is numeric
                            string val = RemoveSpecialCharacters(words_MedValues[j]);
                            bool isNumeric = CheckWords(val, 2);

                            //Check whether word captured is of type Range i.e. 12.0 - 16.0
                            bool isRangeExpression = CheckWords(words_MedValues[j], 1);

                            DtOCR_Value.Rows[rowNum]["Test"] = item.TestName;
                            if (item.StringResult == "" || item.StringResult == null)
                            {
                                if (isNumeric)
                                {
                                    if (DtOCR_Value.Rows[rowNum]["Result"].ToString() != "")
                                    {
                                        if (words_MedValues[j + 1] == "-" || words_MedValues[j + 1] == "to" || words_MedValues[j + 1] == "--")
                                        {
                                            string rangeTill = words_MedValues[j + 2];
                                            DtOCR_Value.Rows[rowNum]["RangeFrom"] = val;
                                            DtOCR_Value.Rows[rowNum]["RangeTill"] = CheckWords(rangeTill, 2) ? rangeTill : item.HighestRangeTill;
                                            j = j + 2;
                                            break;
                                        }
                                    }
                                    DtOCR_Value.Rows[rowNum]["Result"] = val;
                                    continue;
                                }
                                else if (isRangeExpression)
                                {
                                    string[] rangeParams = words_MedValues[j].Split('-');
                                    DtOCR_Value.Rows[rowNum]["RangeFrom"] = rangeParams[0];
                                    DtOCR_Value.Rows[rowNum]["RangeTill"] = rangeParams[1];
                                    continue;
                                }
                            }
                            else
                            {
                                if (!CheckWords(words_MedValues[j], 2))
                                {
                                    string[] results = item.StringResult.Split('|');
                                    int index = Array.FindIndex(results, x => words_MedValues[j].ToUpper().Contains(x));
                                    if (index != -1)
                                    {
                                        DtOCR_Value.Rows[rowNum]["Result"] = results[index];
                                        DtOCR_Value.Rows[rowNum]["HealthStatus"] = words_MedValues[j].Trim().ToUpper() == results[0].Trim().ToUpper() ? "Normal" : "Abnormal";
                                    }
                                    else
                                    {
                                        //DtOCR_Value.Rows.Remove(row);
                                        continue;
                                    }

                                }
                                else
                                    continue;
                            }
                        }
                        if (item.StringResult == "" || item.StringResult == null)
                        {
                            Console.WriteLine($"{DateTime.Now}: {DtOCR_Value.Rows[rowNum][0]} Before AutoCorrect: {DtOCR_Value.Rows[rowNum][1]} {DtOCR_Value.Rows[rowNum][2]} {DtOCR_Value.Rows[rowNum][3]}");
                            logger.Log($"{DateTime.Now}: {DtOCR_Value.Rows[rowNum][0]} Before AutoCorrect: {DtOCR_Value.Rows[rowNum][1]} {DtOCR_Value.Rows[rowNum][2]} {DtOCR_Value.Rows[rowNum][3]}");
                            AutoCorrectResult(ref DtOCR_Value, common, item.Id);
                            logger.Log($"{DateTime.Now}: {DtOCR_Value.Rows[rowNum][0]} Before AutoCorrect: {DtOCR_Value.Rows[rowNum][1]} {DtOCR_Value.Rows[rowNum][2]} {DtOCR_Value.Rows[rowNum][3]}");
                            Console.WriteLine($"{DateTime.Now}: {DtOCR_Value.Rows[rowNum][0]} After AutoCorrect: {DtOCR_Value.Rows[rowNum][1]} {DtOCR_Value.Rows[rowNum][2]} {DtOCR_Value.Rows[rowNum][3]}");
                            if (Convert.ToDouble(DtOCR_Value.Rows[rowNum]["Result"]) > Convert.ToDouble(DtOCR_Value.Rows[rowNum]["RangeFrom"])
                                        && Convert.ToDouble(DtOCR_Value.Rows[rowNum]["Result"]) < Convert.ToDouble(DtOCR_Value.Rows[rowNum]["RangeTill"]))
                            {
                                DtOCR_Value.Rows[rowNum]["HealthStatus"] = "Normal";
                            }
                            else
                                DtOCR_Value.Rows[rowNum]["HealthStatus"] = "Abnormal";
                        }
                        if (!string.IsNullOrEmpty(DtOCR_Value.Rows[rowNum]["Result"].ToString()))
                            item.valueCaptured = true;
                        else
                        {
                            item.valueCaptured = false;
                            DtOCR_Value.Rows.Remove(row);
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"Error occurred: {ex.Message}");
                }

            }

            //var dir = new DirectoryInfo(imagePath);
            //if (dir.Exists)
            //{
            //    dir.Delete(true);
            //}

            return DtOCR_Value;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{DateTime.Now}: Unexpected Error: " + e.Message);
            logger.Log($"Error occurred: {e.Message}");
            throw e;
        }
    }

    #endregion




}