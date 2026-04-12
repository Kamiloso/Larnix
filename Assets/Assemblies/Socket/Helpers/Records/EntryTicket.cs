#nullable enable
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Helpers.Records;

internal record EntryTicket(
    long ServerSecret,
    long ChallengeId,
    long Timestamp,
    long RunId,
    FixedRsaPublic RsaPublicKey
    );
