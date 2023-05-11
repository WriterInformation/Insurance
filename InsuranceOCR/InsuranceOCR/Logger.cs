namespace InsuranceOCR
{
    public class Logger
    {
        private readonly string _logFilePath;

        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}{Environment.NewLine}";

            try
            {
                File.AppendAllText(_logFilePath, logMessage);
            }
            catch (Exception ex)
            {
                // handle exception
            }
        }
    }
}
