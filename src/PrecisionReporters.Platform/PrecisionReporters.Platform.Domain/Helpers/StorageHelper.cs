using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Helpers
{
    public static class StorageHelper
    {
        private static string ToUrlSafeBase64String(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('=', '_')
                .Replace('/', '~');
        }

        public static string CreateCannedPrivateURL(string urlString, string durationUnits,
            string durationNumber, string privateKeyId, string xmlKey, string policyStatement)
        {
            TimeSpan timeSpanInterval = GetDuration(durationUnits, durationNumber);

            string strPolicy = CreatePolicyStatement(policyStatement, urlString, DateTime.Now, DateTime.Now.Add(timeSpanInterval), "0.0.0.0/0");
            if ("Error!" == strPolicy) return "Invalid time frame.  Start time cannot be greater than end time.";
            
            string strExpiration = CopyExpirationTimeFromPolicy(strPolicy);
            byte[] bufferPolicy = Encoding.ASCII.GetBytes(strPolicy);

            // Initialize the SHA1CryptoServiceProvider object and hash the policy data.
            using (SHA1CryptoServiceProvider cryptoSHA1 = new SHA1CryptoServiceProvider())
            {
                bufferPolicy = cryptoSHA1.ComputeHash(bufferPolicy);

                RSACryptoServiceProvider providerRSA = new RSACryptoServiceProvider(2048);
                providerRSA.FromXmlString(xmlKey);
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(providerRSA);
                rsaFormatter.SetHashAlgorithm("SHA1");
                byte[] signedPolicyHash = rsaFormatter.CreateSignature(bufferPolicy);

                string strSignedPolicy = ToUrlSafeBase64String(signedPolicyHash);

                return urlString + "?Expires=" + strExpiration + "&Signature=" + strSignedPolicy + "&Key-Pair-Id=" + privateKeyId;
            }
        }

        private static TimeSpan GetDuration(string units, string numUnits)
        {
            TimeSpan timeSpanInterval = new TimeSpan();
            switch (units)
            {
                case "seconds":
                    timeSpanInterval = new TimeSpan(0, 0, 0, int.Parse(numUnits));
                    break;
                case "minutes":
                    timeSpanInterval = new TimeSpan(0, 0, int.Parse(numUnits), 0);
                    break;
                case "hours":
                    timeSpanInterval = new TimeSpan(0, int.Parse(numUnits), 0, 0);
                    break;
                case "days":
                    timeSpanInterval = new TimeSpan(int.Parse(numUnits), 0, 0, 0);
                    break;
                default:
                    Console.WriteLine("Invalid time units; use seconds, minutes, hours, or days");
                    break;
            }
            return timeSpanInterval;
        }

        private static string CreatePolicyStatement(string policyStatement, string resourceUrl,
                               DateTime startTime, DateTime endTime, string ipAddress)
        {
            string strPolicy = policyStatement;

            TimeSpan startTimeSpanFromNow = (startTime - DateTime.Now);
            TimeSpan endTimeSpanFromNow = (endTime - DateTime.Now);
            TimeSpan intervalStart =
                (DateTime.UtcNow.Add(startTimeSpanFromNow)) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan intervalEnd =
                (DateTime.UtcNow.Add(endTimeSpanFromNow)) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            int startTimestamp = (int)intervalStart.TotalSeconds; // START_TIME
            int endTimestamp = (int)intervalEnd.TotalSeconds;  // END_TIME

            if (startTimestamp > endTimestamp)
                return "Error!";

            // Replace variables in the policy statement.
            strPolicy = strPolicy.Replace("RESOURCE", resourceUrl);
            strPolicy = strPolicy.Replace("START_TIME", startTimestamp.ToString());
            strPolicy = strPolicy.Replace("END_TIME", endTimestamp.ToString());
            strPolicy = strPolicy.Replace("IP_ADDRESS", ipAddress);
            strPolicy = strPolicy.Replace("EXPIRES", endTimestamp.ToString());
            return strPolicy;
        }

        private static string CopyExpirationTimeFromPolicy(string policyStatement)
        {
            int startExpiration = policyStatement.IndexOf("EpochTime");
            string strExpirationRough = policyStatement.Substring(startExpiration + "EpochTime".Length);
            char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            List<char> listDigits = new List<char>(digits);
            StringBuilder buildExpiration = new StringBuilder(20);
            foreach (char c in strExpirationRough)
            {
                if (listDigits.Contains(c))
                    buildExpiration.Append(c);
            }
            return buildExpiration.ToString();
        }
    }
}
