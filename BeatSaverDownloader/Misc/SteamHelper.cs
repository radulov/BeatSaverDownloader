using Steamworks;

namespace BeatSaverDownloader.Misc
{
    public static class SteamHelper
    {
        public static HAuthTicket lastTicket;
        public static EResult lastTicketResult;

        public static Callback<GetAuthSessionTicketResponse_t> m_GetAuthSessionTicketResponse;
    }
}