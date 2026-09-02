namespace CustomNPCExample.Quests
{
    public static class DamonSnitchMessage
    {
        public const string MessageId =
            "wvc_snitch_initial_damon_message";

        public static bool Send()
        {
            return SnitchPhoneDelivery.Queue(
                MessageId,
                "Damon Trey",
                "Yo. One of ours got pinched. He's talking. " +
                "Cops are crawling everywhere. " +
                "Meet me at Bud's Bar."
            );
        }
    }
}