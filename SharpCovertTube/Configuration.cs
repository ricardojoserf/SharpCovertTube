using System;

namespace SharpCovertTube
{
    internal class Configuration
    {
        /* Configuration values */
        // Channel ID (mandatory!!!). Get it from: https://www.youtube.com/account_advanced
        public const string channel_id = "";
        // API Key (mandatory!!!). Get it from: https://console.cloud.google.com/apis/credentials
        public const string api_key = "";
        // AES Key used for payload encryption
        public const string payload_aes_key = "0000000000000000";
        // IV Key used for payload encryption
        public const string payload_aes_iv = "0000000000000000";
        // Period between every check of the Youtube channel. Default is 10 minutes to avoid exceeding api quota
        public const int seconds_delay = 600;
        // Show debug messages in console or not
        public const bool debug_console = true;
        // Write debug messages in log file or not
        public const bool log_to_file = true;
        // Log file 
        public const string log_file = "c:\\temp\\.sharpcoverttube.log";
        // Exfiltrate command responses through DNS or not
        public const bool dns_exfiltration = true;
        // DNS hostname used for DNS exfiltration
        public const string dns_hostname = ".test.org";
    }
}
