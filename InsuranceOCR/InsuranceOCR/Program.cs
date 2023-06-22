using ClosedXML.Excel;
using InsuranceOCR;
using SautinSoft.Document;
using System.Configuration;
using System.Data;
using OpenCvSharp;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DocumentFormat.OpenXml.Drawing.Charts;
using DataTable = System.Data.DataTable;
using System.Diagnostics;

static class Program
{
    static string output_Path = ConfigurationManager.AppSettings["output_Path"].ToString();
    static string pdf_Path = ConfigurationManager.AppSettings["pdf_Path"].ToString();
    static string log_Path = ConfigurationManager.AppSettings["log_Path"].ToString();
    static string doc_Path = ConfigurationManager.AppSettings["doc_Path"].ToString();
    static string webApiUrl = ConfigurationManager.AppSettings["webapi_url"].ToString();
    static Logger logger = new Logger(log_Path + @"\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

    static void Main(string[] args)
    {
        MyMethodAsync();
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
                else if(method == Convert.ToInt32(Enums.HttpMethod.POST))
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
        //DocumentCore dc = DocumentCore.Load(@"E:\Workspace\Docs\BloodTests\PDFImagePath\ApolloPlusBloodUrine.pdf");
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
    
    public static async Task ManagePolicyFoldersAsync()
    {
        string[] dirs = Directory.GetDirectories(doc_Path, "*", SearchOption.TopDirectoryOnly);
        var requestDataJson = JsonSerializer.Serialize(dirs);
        await SendHttpRequest(requestDataJson, "PolicyMasterAPI/CheckPolicyMaster", Convert.ToInt32(Enums.HttpMethod.POST));
    }
    public static async Task ManageDocsForPolicies()
    {
        List<Policy_Docs_Model> policy_Docs_Model = new List<Policy_Docs_Model>();
        string[] dirs = Directory.GetDirectories(doc_Path, "*", SearchOption.TopDirectoryOnly);
        foreach (string dir in dirs)
        {
            string directoryName = dir.Split(@"\")[dir.Split(@"\").Length - 1];
            string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            policy_Docs_Model.Add(new Policy_Docs_Model()
            {
                PolicyNo = Convert.ToInt32(directoryName),
                Files=files
            });
        }
        var requestDataJson = JsonSerializer.Serialize(policy_Docs_Model);
        await SendHttpRequest(requestDataJson, "PolicyMasterAPI/ManageDocsForPolicies", Convert.ToInt32(Enums.HttpMethod.POST));
    }
    public static async Task ProcessPendingDocsAsync()
    {

        List<Document_Master> ocrData = new List<Document_Master>();
        var ocrDataResponse = await SendHttpRequest("", "PolicyMasterAPI/GetPendingOCRData", Convert.ToInt32(Enums.HttpMethod.GET));
        // Check if the response was successful
        if (ocrDataResponse.IsSuccessStatusCode)
        {
            // Read the response content
            var ocrDataResponseContent = await ocrDataResponse.Content.ReadAsStringAsync();
            ocrData = JsonSerializer.Deserialize<List<Document_Master>>(ocrDataResponseContent);
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
        foreach (var document in ocrData)
        {
            if (document.DocExtension==FileExtensions.Jpg|| document.DocExtension == FileExtensions.Png|| document.DocExtension == FileExtensions.Tiff)
            {
                ImageProcessing(document);
            }
            else if (document.DocExtension == FileExtensions.Pdf)
            {
                PDFProcessing(document);
            }
        }        
    }

    public static void ImageProcessing(Document_Master document)
    {

    }

    public static void PDFProcessing(Document_Master document)
    {

    }

    public static void OCRProcessing(string imagePath)
    {
        var str_text3 = "";
        //Read Medical Parameters from JSON file
        string text = File.ReadAllText(@"./CommonMedValues.json");
        var common = JsonSerializer.Deserialize<List<CommonMedicalVal>>(text);

        #region PDF To Tiff
        //SautinSoft
        //string imagePath = pdf_Path + @"BloodReportImages\"; //BloodTest
        string imagePath = doc_Path + @"ECGTests\ECGTestImages\"; //ECG
        //int pageCnt = pdfToImage(pdf_Path + "ApolloPlusBloodUrine.pdf", imagePath);
        Console.WriteLine($"{DateTime.Now}: Converting PDF to images");
        logger.Log("Converting PDF to images");

        //var medReports = JsonSerializer.Deserialize<string>(File.ReadAllText(@"./MedicalReports.json"));


        //BloodTest
        //int pageCnt = ConvertPDFtoImages(pdf_Path + "VighnaharBloodUrineTest.pdf", imagePath);
        //int pageCnt = ConvertPDFtoImages(pdf_Path + "ApolloPlusBloodUrine.pdf", imagePath);
        //int pageCnt = ConvertPDFtoImages(pdf_Path + "ArthDiagnostics.pdf", imagePath);
        //int pageCnt = ConvertPDFtoImages(pdf_Path + "CrystalBloodUrineTest.pdf", imagePath);
        //int pageCnt = ConvertPDFtoImages(pdf_Path + "KarthikaBloodUrinetest.pdf", imagePath);

        //ECG
        //int pageCnt = ConvertPDFtoImages(@"G:\Workspace\Docs\ECGTests\ArthDiagnostics.pdf", imagePath);
        //int pageCnt = ConvertPDFtoImages(@"G:\Workspace\Docs\ECGTests\GoldRushPathology.pdf", imagePath);
        int pageCnt = ConvertPDFtoImages(@"G:\Workspace\Docs\ECGTests\KarthikaHealthCare.pdf", imagePath);

        Console.WriteLine($"{DateTime.Now}: Images Generated");
        logger.Log("Images Generated");
        #endregion

        string tessdataPath = ConfigurationManager.AppSettings["tessdataPath"].ToString();

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
        try
        {
            Console.WriteLine($"{DateTime.Now}: Running OCR on PDF");
            logger.Log("Running OCR on PDF");
            for (int i = 1; i <= pageCnt; i++)
            {
                //ECG
                string imageFilePath = doc_Path + @"ECGTests\ECGTestImages\Page" + i + ".tiff";
                Mat image = new Mat();
                image = Cv2.ImRead(imageFilePath);
                Mat Output = new Mat();
                Cv2.Threshold(image, Output, 153, 255, ThresholdTypes.Binary);

                Output.SaveImage(@"G:\Workspace\Docs\ECGTests\ECGTestImages\ProcessedImage.jpeg");


                using (var tEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default)) //creating the tesseract OCR engine with English as the language
                {
                    tEngine.SetVariable("debug_file", "null");

                    //Load of the image file from the Pix object which is a wrapper for Leptonica PIX structure
                    //using (var img = Pix.LoadFromFile(pdf_Path + @"BloodReportImages\Page" + i + ".tiff")) // Blood Test
                    using (var img = Pix.LoadFromFile(pdf_Path + @"ECGTestImages\ProcessedImage.jpeg")) // ECG
                    {
                        var pixFile = img.Rotate((float)(90 * Math.PI / 180.0f)); //ECG
                        //pixFile = pixFile.Deskew();
                        //pixFile.Save(pdf_Path + @"BloodReportImages\Page" + i + ".tiff");
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
            }
            Console.WriteLine($"{DateTime.Now}: OCR completed!");
            logger.Log("OCR completed!");


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
                        DataRow drHeader = DtOCR_Value.NewRow();
                        drHeader["Test"] = "URINE EXAMINATION";
                        DtOCR_Value.Rows.Add(drHeader);
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

            var dir = new DirectoryInfo(imagePath);
            if (dir.Exists)
            {
                dir.Delete(true);
            }

            DataSet ds = new DataSet();

            DtOCR_Value.TableName = "OCR_Value";
            ds.Tables.Add(DtOCR_Value);

            List<Policy_Master> policies = new List<Policy_Master>();
            int ProposalNumber = 0;

            foreach (DataRow dr in DtOCR_Value.Rows)
            {
                policies.Add(new Policy_Master()
                {
                    PolicyNo = ProposalNumber,
                    PolicyStatusCode = 0,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "SYSTEM"
                });
            }

            var requestDataJson = JsonSerializer.Serialize(policies);
            //await SendHttpRequest(requestDataJson, "PolicyMasterAPI/AddMedicalValues");

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(ds);

                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;

                string OutPutFile = "";
                string OutputPath = output_Path;

                string filePath = output_Path;

                string CurrDate = DateTime.Now.ToString("yyyyMMddHHmmss");

                string OutputFileName = "file_Name" + CurrDate + ".xlsx";

                wb.SaveAs(filePath + "\\" + OutputFileName);

                OutPutFile = filePath + "\\" + OutputFileName;

                filePath = "";
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"{DateTime.Now}: Unexpected Error: " + e.Message);
            logger.Log($"Error occurred: {e.Message}");

        }

    }
    public static async Task MyMethodAsync()
    {
        Task<int> longRunningTask = LongRunningOperationAsync();

        // run the below code in separate thread
        //some code here 
        //some code here
        for (int i = 0; i < 10000000000; i++)
        {
            Console.WriteLine(i); //SET BREAK POINT HERE
        }
        //some code here

        //and now we call await on the task 
        int result = await longRunningTask;
    }
    public static async Task<int> LongRunningOperationAsync() // assume we return an int from this long running operation 
    {
        int timerDelayInSecs = Convert.ToInt32(ConfigurationManager.AppSettings["timerDelayInSecs"]);
        bool retry = true;

        using (AutoResetEvent wait = new AutoResetEvent(false))
        {
            while (retry)
            {

                //Do Work here
                //_ = ManagePolicyFoldersAsync();
                //_ = ManageDocsForPolicies();
                OCRProcessing();
                //await Task.Delay(43200000); //12 hour delay
                await Task.Delay(timerDelayInSecs * 1000); //SET BREAK POINT HERE
            }
        }

        return 1;
    }
}