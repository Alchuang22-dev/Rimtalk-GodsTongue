using System.Collections.Generic;
using Verse;

namespace RimTalk.Broadcast;

public static class BroadcastPending
{
    // initiator -> (origin,message)
    public static readonly Dictionary<Pawn, PendingBroadcast> Pending = new();

    public sealed class PendingBroadcast
    {
        public readonly Pawn Origin;
        public readonly string Message;

        public PendingBroadcast(Pawn origin, string message)
        {
            Origin = origin;
            Message = message;
        }
    }
}
