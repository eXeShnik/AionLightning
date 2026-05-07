namespace AionLightning.Login.Network.Aion
{
    public enum AionAuthResponse
    {
        AUTHED = 0,
        SYSTEM_ERROR = 1,
        INVALID_PASSWORD = 2,
        INVALID_PASSWORD2 = 3,
        FAILED_ACCOUNT_INFO = 4,
        FAILED_SOCIAL_NUMBER = 5,
        NO_GS_REGISTERED = 6,
        ALREADY_LOGGED_IN = 7,
        SERVER_DOWN = 8,
        INVALID_PASSWORD3 = 9,
        NO_SUCH_ACCOUNT = 10,
        DISCONNECTED = 11,
        AGE_LIMIT = 12,
        ALREADY_LOGGED_IN2 = 13,
        ALREADY_LOGGED_IN3 = 14,
        SERVER_FULL = 15,
        GM_ONLY = 16,
        ERROR_17 = 17,
        TIME_EXPIRED = 18,
        TIME_EXPIRED2 = 19,
        SYSTEM_ERROR2 = 20,
        ALREADY_USED_IP = 21,
        IP_BANNED = 22,
    }

    public static class AionAuthResponseExtensions
    {
        public static int GetMessageId(this AionAuthResponse response)
        {
            return (int)response;
        }
    }
}
