namespace AionLightning.Login.Network.Aion
{
    public enum AionAuthResponse
    {
        AUTHED = 0,
        SYSTEM_ERROR = 1,
        INVALID_PASSWORD = 2,
        INVALID_PASSWORD2 = 3,
        FAILED_ACCOUNT_INFO = 4,
        FAILED_SOCIAL_SECURITY_NUMBER = 5,
        NO_GS_REGISTERED = 6,
        ALREADY_LOGGED_IN = 7,
        ALREADY_LOGGED_IN2 = 8,
        ACCESS_FAILED = 9,
        AGE_LIMIT = 10,
        SERVER_DOWN = 11,
        SERVER_IN_CHECK = 12,
        GM_ONLY = 13,
        SERVER_FULL = 14,
        IP_BANNED = 15,
        TIME_EXPIRED = 16,
        TIME_EXPIRED2 = 17,
        ACCOUNT_IN_USE = 21,
        BANNED = 23,
    }

    public static class AionAuthResponseExtensions
    {
        public static int GetMessageId(this AionAuthResponse response)
        {
            return (int)response;
        }
    }
}
